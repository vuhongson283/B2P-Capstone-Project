using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;
using B2P_API.Services;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly BookingService _bookingService;
        public PaymentsController(IConfiguration config, BookingService bookingService)
        {
            // Set Stripe API Key (test key)
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
            _bookingService = bookingService;
        }

        [HttpPost("connected-account")]
        public IActionResult CreateConnectedAccount()
        {
            var options = new AccountCreateOptions
            {
                Type = "custom", // dùng custom để có thể active luôn
                Country = "US",
                Email = $"test{Guid.NewGuid()}@example.com", // random => tạo được nhiều account
                BusinessType = "individual",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                },
                Individual = new AccountIndividualOptions
                {
                    FirstName = "Jenny",
                    LastName = "Rosen",
                    Dob = new DobOptions { Day = 1, Month = 1, Year = 1990 },
                    Address = new AddressOptions
                    {
                        Line1 = "123 Main Street",
                        City = "San Francisco",
                        State = "CA",
                        PostalCode = "94111",
                        Country = "US"
                    },
                    Email = "jenny.rosen@example.com"
                },
                ExternalAccount = new AccountBankAccountOptions
                {
                    Country = "US",
                    Currency = "usd",
                    RoutingNumber = "110000000", // test value
                    AccountNumber = "000123456789" // test value
                }
            };

            var service = new Stripe.AccountService();
            var account = service.Create(options);

            return Ok(account);
        }


        // 1. Tạo PaymentIntent (hold tiền 7 ngày, tiền đi thẳng vào đối tác)
        [HttpPost("create")]
        public IActionResult CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount, // số tiền (cent)
                Currency = request.Currency, // ví dụ: "usd"
                PaymentMethodTypes = new List<string> { "card" },
                CaptureMethod = "manual", // manual capture để hold
                ApplicationFeeAmount = request.PlatformFee, // phí nền tảng lấy (cent)
                TransferData = new PaymentIntentTransferDataOptions
                {
                    Destination = request.DestinationAccountId // Connected Account của đối tác
                },
                Metadata = new Dictionary<string, string>
        {
            { "BookingId", request.BookingId.ToString() }
        }
            };

            var service = new PaymentIntentService();
            var paymentIntent = service.Create(options);

            return Ok(paymentIntent);
        }


        // 2. Capture khi xác nhận
        [HttpPost("capture/{paymentIntentId}")]
        public IActionResult CapturePayment(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            var paymentIntent = service.Capture(paymentIntentId);
            return Ok(paymentIntent);
        }

        // 3. Cancel nếu không duyệt
        [HttpPost("cancel/{paymentIntentId}")]
        public IActionResult CancelPayment(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            var paymentIntent = service.Cancel(paymentIntentId);
            return Ok(paymentIntent);
        }

        [HttpPost("account-link")]
        public IActionResult CreateAccountLink([FromBody] CreateAccountLinkRequest request)
        {
            var options = new AccountLinkCreateOptions
            {
                Account = request.AccountId, // ID của connected account vừa tạo
                RefreshUrl = "https://example.com/reauth",
                ReturnUrl = "https://example.com/return",
                Type = "account_onboarding"
            };

            var service = new AccountLinkService();
            var accountLink = service.Create(options);

            return Ok(new { url = accountLink.Url });
        }

        [HttpPost("confirm/{paymentIntentId}")]
        public IActionResult ConfirmPayment(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentConfirmOptions
            {
                PaymentMethod = "pm_card_visa" // thẻ test Stripe
            };
            var paymentIntent = service.Confirm(paymentIntentId, options);
            return Ok(paymentIntent);
        }

        [HttpPost("create-checkout-session")]
        public IActionResult CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = request.Currency,
                    UnitAmount = request.Amount, // cent
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = request.Description
                    }
                },
                Quantity = 1
            }
        },
                Mode = "payment",
                SuccessUrl = "https://example.com/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "https://example.com/cancel",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Ok(new { url = session.Url });
        }

        [HttpPost("test-webhook")]
        public async Task<IActionResult> TestWebhook([FromBody] JsonElement payload)
        {
            Console.WriteLine(payload); // Log toàn bộ JSON để debug

            // Lấy eventType
            if (!payload.TryGetProperty("type", out var typeProp))
            {
                Console.WriteLine("Webhook không có type");
                return Ok();
            }
            var eventType = typeProp.GetString();

            int bookingId = 0;
            string stripePaymentIntentId = string.Empty;

            // Lấy data.object
            if (payload.TryGetProperty("data", out var dataProp) &&
                dataProp.TryGetProperty("object", out var objectProp))
            {
                // Stripe PaymentIntent Id
                if (objectProp.TryGetProperty("id", out var stripeIdProp))
                {
                    stripePaymentIntentId = stripeIdProp.GetString();
                    Console.WriteLine($"Stripe PaymentIntentId: {stripePaymentIntentId}");
                }

                // BookingId trong metadata
                if (objectProp.TryGetProperty("metadata", out var metadataProp) &&
                    metadataProp.TryGetProperty("BookingId", out var bookingIdProp))
                {
                    var bookingIdStr = bookingIdProp.GetString();
                    if (!int.TryParse(bookingIdStr, out bookingId))
                        Console.WriteLine($"BookingId không hợp lệ: {bookingIdStr}");
                    else
                        Console.WriteLine($"BookingId: {bookingId}");
                }
            }

            if (bookingId == 0)
            {
                Console.WriteLine("Webhook không có BookingId, bỏ qua xử lý.");
                return Ok();
            }

            // Xử lý theo loại sự kiện Stripe
            switch (eventType)
            {
                case "payment_intent.succeeded":
                    await _bookingService.MarkBookingCompleteAsync(bookingId);
                    Console.WriteLine("Merchant đã nhận tiền");
                    break;

                case "payment_intent.canceled":
                    await _bookingService.MarkBookingCancelledAsync(bookingId);
                    Console.WriteLine("Hủy đơn");
                    break;

                case "payment_intent.amount_capturable_updated":
                    await _bookingService.MarkBookingPaidAsync(bookingId, stripePaymentIntentId);
                    Console.WriteLine("Player đã trả tiền (chờ capture)");
                    break;

                case "payment_intent.created":
                    Console.WriteLine("Tạo PaymentIntent thành công (không xử lý DB)");
                    break;

                default:
                    Console.WriteLine($"Sự kiện chưa xử lý: {eventType}");
                    break;
            }

            return Ok();
        }






    }

    // Request model cho create
    public class CreatePaymentRequest
    {
        public long Amount { get; set; } // số tiền (cent)
        public string Currency { get; set; } = "usd";
        public long PlatformFee { get; set; } // phí platform (cent)
        public string DestinationAccountId { get; set; } = string.Empty;
        public string BookingId { get; set; }
    }
    public class CreateAccountLinkRequest
    {
        public string AccountId { get; set; } = string.Empty;
    }

    public class CreateCheckoutRequest
    {
        public long Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public string Description { get; set; } = "";
    }
}
