using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;
using B2P_API.Services;
using B2P_API.DTOs.PaymentDTOs;
using Newtonsoft.Json;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly BookingService _bookingService;
        private readonly PaymentService _paymentService;
		private readonly IBookingNotificationService _notificationService;
		public PaymentsController(IConfiguration config, BookingService bookingService, PaymentService paymentService, IBookingNotificationService notificationService)
        {
            // Set Stripe API Key (test key)
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
            _bookingService = bookingService;
            _paymentService = paymentService;
			_notificationService = notificationService;
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
		public async Task<IActionResult> CancelPayment(string paymentIntentId)
		{
			try
			{
				Console.WriteLine($"🔄 [CancelPayment] START - Cancelling payment: {paymentIntentId}");

				// ✅ CHECK REQUIRED SERVICES
				if (_notificationService == null)
				{
					Console.WriteLine($"❌ [CancelPayment] ERROR: _notificationService is null!");
					return StatusCode(500, "NotificationService not initialized");
				}

				if (_bookingService == null)
				{
					Console.WriteLine($"❌ [CancelPayment] ERROR: _bookingService is null!");
					return StatusCode(500, "BookingService not initialized");
				}

				var service = new PaymentIntentService();

				// ✅ GET PAYMENT INTENT DETAILS FIRST
				var existingPaymentIntent = await service.GetAsync(paymentIntentId);
				Console.WriteLine($"📄 [CancelPayment] Current status: {existingPaymentIntent.Status}");
				Console.WriteLine($"📄 [CancelPayment] Metadata: {JsonConvert.SerializeObject(existingPaymentIntent.Metadata)}");

				// ✅ GET BOOKINGID FROM METADATA
				int bookingId = 0;
				if (existingPaymentIntent.Metadata != null &&
					existingPaymentIntent.Metadata.ContainsKey("BookingId") &&
					int.TryParse(existingPaymentIntent.Metadata["BookingId"], out bookingId))
				{
					Console.WriteLine($"✅ [CancelPayment] BookingId found: {bookingId}");
				}
				else
				{
					Console.WriteLine($"❌ [CancelPayment] No valid BookingId in metadata");
					return BadRequest("No valid BookingId found in payment metadata");
				}

				// ✅ CANCEL THE PAYMENT ON STRIPE
				Console.WriteLine($"🔄 [CancelPayment] Sending cancel request to Stripe...");
				var cancelledPaymentIntent = await service.CancelAsync(paymentIntentId);
				Console.WriteLine($"✅ [CancelPayment] Payment cancelled successfully! New status: {cancelledPaymentIntent.Status}");

				// ✅ CANCEL BOOKING IN DATABASE
				Console.WriteLine($"🔄 [CancelPayment] Marking booking {bookingId} as cancelled in database...");
				var cancelResult = await _bookingService.MarkBookingCancelledAsync(bookingId);

				if (!cancelResult.Success)
				{
					Console.WriteLine($"❌ [CancelPayment] Failed to cancel booking: {cancelResult.Message}");
					return StatusCode(500, $"Failed to cancel booking: {cancelResult.Message}");
				}

				Console.WriteLine($"✅ [CancelPayment] Booking {bookingId} marked as cancelled in database");

				// ✅ GET BOOKING DETAILS AND SEND SIGNALR NOTIFICATION DIRECTLY
				Console.WriteLine($"🔄 [CancelPayment] Getting booking details for notification...");
				var bookingDetailsResponse = await _bookingService.GetByIdAsync(bookingId);

				if (!bookingDetailsResponse.Success || bookingDetailsResponse.Data == null)
				{
					Console.WriteLine($"❌ [CancelPayment] Could not get booking details for notification");
					return Ok(new
					{
						success = true,
						paymentIntent = cancelledPaymentIntent,
						bookingId = bookingId,
						bookingCancelled = true,
						notificationSent = false,
						message = "Payment and booking cancelled, but notification failed due to missing booking details."
					});
				}

				var bookingData = bookingDetailsResponse.Data;
				int facilityId = bookingData.FacilityId;

				Console.WriteLine($"📄 [CancelPayment] Booking details:");
				Console.WriteLine($"   FacilityId: {facilityId}");
				Console.WriteLine($"   Email: {bookingData.Email}");
				Console.WriteLine($"   CheckInDate: {bookingData.CheckInDate}");

				if (facilityId == 0)
				{
					Console.WriteLine($"❌ [CancelPayment] FacilityId is 0, cannot send notification");
					return Ok(new
					{
						success = true,
						paymentIntent = cancelledPaymentIntent,
						bookingId = bookingId,
						bookingCancelled = true,
						notificationSent = false,
						message = "Payment and booking cancelled, but notification failed due to missing facilityId."
					});
				}

				// ✅ BUILD NOTIFICATION DATA (SAME AS MarkCancel)
				string courtName = "Sân thể thao";
				string timeSlot = "N/A";
				int courtId = 0;

				// ✅ NULL CHECK FOR SLOTS
				if (bookingData.Slots != null && bookingData.Slots.Count > 0)
				{
					var firstSlot = bookingData.Slots[0];
					if (firstSlot != null)
					{
						courtName = firstSlot.CourtName ?? "Sân thể thao";
						courtId = firstSlot.CourtId;
						var startTime = firstSlot.StartTime.ToString(@"hh\:mm");
						var endTime = firstSlot.EndTime.ToString(@"hh\:mm");
						timeSlot = $"{startTime} - {endTime}";
					}
				}

				string dateStr = bookingData.CheckInDate.ToString("dd/MM/yyyy");

				var cancelNotificationData = new
				{
					bookingId = bookingId,
					facilityId = facilityId,
					courtId = courtId,
					courtName = courtName,
					customerName = bookingData.Email?.Split('@')[0] ?? "Khách",
					customerEmail = bookingData.Email,
					customerPhone = bookingData.Phone,
					date = dateStr,
					timeSlot = timeSlot,
					totalAmount = bookingData.TotalPrice,
					status = "Cancelled",
					statusId = 9,
					statusDescription = "Đã hủy",
					action = "cancelled",
					reason = "Payment cancelled by customer",
					message = "Đơn đặt sân đã bị hủy do hủy thanh toán",
					timestamp = DateTime.UtcNow.ToString("o")
				};

				Console.WriteLine($"📤 [CancelPayment] Sending cancellation notification to facility_{facilityId}");
				Console.WriteLine($"📤 [CancelPayment] Notification data: {JsonConvert.SerializeObject(cancelNotificationData, Formatting.Indented)}");

				// ✅ SEND SIGNALR NOTIFICATION WITH TRY-CATCH
				try
				{
					Console.WriteLine($"🔄 [CancelPayment] Calling NotifyBookingCancelled...");
					await _notificationService.NotifyBookingCancelled(facilityId, cancelNotificationData);
					Console.WriteLine($"✅ [CancelPayment] NotifyBookingCancelled completed successfully!");

					return Ok(new
					{
						success = true,
						paymentIntent = cancelledPaymentIntent,
						bookingId = bookingId,
						bookingCancelled = true,
						notificationSent = true,
						facilityId = facilityId,
						message = "Payment and booking cancelled successfully with notification sent."
					});
				}
				catch (Exception notifEx)
				{
					Console.WriteLine($"❌ [CancelPayment] Notification error: {notifEx.Message}");
					Console.WriteLine($"❌ [CancelPayment] Notification stack trace: {notifEx.StackTrace}");

					return Ok(new
					{
						success = true,
						paymentIntent = cancelledPaymentIntent,
						bookingId = bookingId,
						bookingCancelled = true,
						notificationSent = false,
						notificationError = notifEx.Message,
						message = "Payment and booking cancelled, but notification failed."
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ [CancelPayment] ERROR: {ex.Message}");
				Console.WriteLine($"❌ [CancelPayment] Stack trace: {ex.StackTrace}");
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message,
					stackTrace = ex.StackTrace
				});
			}
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

			switch (eventType)
			{
				case "payment_intent.succeeded":
					await _bookingService.MarkBookingCompleteAsync(bookingId);
					Console.WriteLine("Merchant đã nhận tiền");
					break;

				case "payment_intent.canceled":
					await _bookingService.MarkBookingCancelledAsync(bookingId);
					Console.WriteLine("Hủy đơn");

					// Gửi notification chi tiết như MarkCancel
					var bookingDetailsResponse = await _bookingService.GetByIdAsync(bookingId);
					if (bookingDetailsResponse.Success && bookingDetailsResponse.Data != null)
					{
						var bookingData = bookingDetailsResponse.Data;
						int facilityId = bookingData.FacilityId;
						if (facilityId != 0)
						{
							string courtName = "Sân thể thao";
							string timeSlot = "N/A";
							int courtId = 0;
							if (bookingData.Slots != null && bookingData.Slots.Count > 0)
							{
								var firstSlot = bookingData.Slots[0];
								courtName = firstSlot.CourtName ?? "Sân thể thao";
								courtId = firstSlot.CourtId;
								var startTime = firstSlot.StartTime.ToString(@"hh\:mm");
								var endTime = firstSlot.EndTime.ToString(@"hh\:mm");
								timeSlot = $"{startTime} - {endTime}";
							}
							string dateStr = bookingData.CheckInDate.ToString("dd/MM/yyyy");
							var cancelNotificationData = new
							{
								bookingId = bookingId,
								facilityId = facilityId,
								courtId = courtId,
								courtName = courtName,
								customerName = bookingData.Email?.Split('@')[0] ?? "Khách",
								customerEmail = bookingData.Email,
								customerPhone = bookingData.Phone,
								date = dateStr,
								timeSlot = timeSlot,
								totalAmount = bookingData.TotalPrice,
								status = "Cancelled",
								statusId = 9,
								statusDescription = "Đã hủy",
								action = "cancelled",
								reason = "Booking cancelled by customer",
								message = "Đơn đặt sân đã bị hủy",
								timestamp = DateTime.UtcNow.ToString("o")
							};
							await _notificationService.NotifyBookingCancelled(facilityId, cancelNotificationData);
							Console.WriteLine($"✅ Cancellation notification sent to facility_{facilityId}");
						}
					}
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

		[HttpPost("callback")]
		public async Task<IActionResult> StripeCallback([FromBody] JsonElement payload)
		{
			Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Stripe webhook received:");
			Console.WriteLine(payload);

			if (!payload.TryGetProperty("type", out var typeProp))
			{
				Console.WriteLine("Webhook không có type");
				return Ok();
			}

			var eventType = typeProp.GetString();
			int bookingId = 0;
			string stripePaymentIntentId = string.Empty;

			if (payload.TryGetProperty("data", out var dataProp) &&
				dataProp.TryGetProperty("object", out var objectProp))
			{
				if (objectProp.TryGetProperty("id", out var stripeIdProp))
				{
					stripePaymentIntentId = stripeIdProp.GetString();
					Console.WriteLine($"Stripe PaymentIntentId: {stripePaymentIntentId}");
				}

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

			try
			{
				switch (eventType)
				{
					case "payment_intent.succeeded":
						Console.WriteLine($"Processing payment_intent.succeeded for booking {bookingId}");
						await _bookingService.MarkBookingCompleteAsync(bookingId);
						Console.WriteLine("✅ Booking marked as completed - Merchant đã nhận tiền");
						break;

					case "payment_intent.canceled":
						Console.WriteLine($"🔄 Processing payment_intent.canceled for booking {bookingId}");

						// ✅ MARK BOOKING AS CANCELLED IN DATABASE
						await _bookingService.MarkBookingCancelledAsync(bookingId);
						Console.WriteLine("✅ Booking marked as cancelled in database");

						// ✅ GET BOOKING DETAILS AND SEND SIGNALR NOTIFICATION (SAME AS MarkCancel API)
						var bookingDetailsResponse = await _bookingService.GetByIdAsync(bookingId);
						if (bookingDetailsResponse.Success && bookingDetailsResponse.Data != null)
						{
							var bookingData = bookingDetailsResponse.Data;
							int facilityId = bookingData.FacilityId;

							Console.WriteLine($"📄 Booking details for SignalR:");
							Console.WriteLine($"   BookingId: {bookingId}");
							Console.WriteLine($"   FacilityId: {facilityId}");
							Console.WriteLine($"   FacilityName: {bookingData.FacilityName}");
							Console.WriteLine($"   CustomerEmail: {bookingData.Email}");

							if (facilityId != 0)
							{
								string courtName = "Sân thể thao";
								string timeSlot = "N/A";
								int courtId = 0;

								if (bookingData.Slots != null && bookingData.Slots.Count > 0)
								{
									var firstSlot = bookingData.Slots[0];
									courtName = firstSlot.CourtName ?? "Sân thể thao";
									courtId = firstSlot.CourtId;
									var startTime = firstSlot.StartTime.ToString(@"hh\:mm");
									var endTime = firstSlot.EndTime.ToString(@"hh\:mm");
									timeSlot = $"{startTime} - {endTime}";
								}

								string dateStr = bookingData.CheckInDate.ToString("dd/MM/yyyy");

								var cancelNotificationData = new
								{
									bookingId = bookingId,
									facilityId = facilityId,
									courtId = courtId,
									courtName = courtName,
									customerName = bookingData.Email?.Split('@')[0] ?? "Khách",
									customerEmail = bookingData.Email,
									customerPhone = bookingData.Phone,
									date = dateStr,
									timeSlot = timeSlot,
									totalAmount = bookingData.TotalPrice,
									status = "Cancelled",
									statusId = 9,
									statusDescription = "Đã hủy",
									action = "cancelled",
									reason = "Payment cancelled by customer", // ✅ DIFFERENT REASON FROM MarkCancel
									message = "Đơn đặt sân đã bị hủy do hủy thanh toán",
									timestamp = DateTime.UtcNow.ToString("o")
								};

								Console.WriteLine($"📤 Sending cancellation notification to facility_{facilityId}:");
								Console.WriteLine(JsonConvert.SerializeObject(cancelNotificationData, Formatting.Indented));

								// ✅ SEND SIGNALR NOTIFICATION
								await _notificationService.NotifyBookingCancelled(facilityId, cancelNotificationData);
								Console.WriteLine($"✅ Cancellation notification sent to facility_{facilityId} via SignalR");
							}
							else
							{
								Console.WriteLine($"⚠️ WARNING: Could not get facilityId from booking {bookingId}, skipping SignalR notification");
							}
						}
						else
						{
							Console.WriteLine($"❌ ERROR: Could not get booking details for {bookingId}");
						}
						break;

					case "payment_intent.amount_capturable_updated":
						Console.WriteLine($"Processing payment_intent.amount_capturable_updated for booking {bookingId}");
						await _bookingService.MarkBookingPaidAsync(bookingId, stripePaymentIntentId);
						Console.WriteLine("✅ Booking marked as paid - Player đã trả tiền (chờ capture)");
						break;

					case "payment_intent.payment_failed":
						Console.WriteLine($"Processing payment_intent.payment_failed for booking {bookingId}");
						// Có thể mark booking failed hoặc revert về unpaid
						break;

					case "payment_intent.requires_capture":
						Console.WriteLine($"Processing payment_intent.requires_capture for booking {bookingId}");
						await _bookingService.MarkBookingPaidAsync(bookingId, stripePaymentIntentId);
						Console.WriteLine("✅ Payment confirmed and ready for capture - marked as paid");
						break;

					case "payment_intent.created":
						Console.WriteLine("Tạo PaymentIntent thành công (không xử lý DB)");
						break;

					default:
						Console.WriteLine($"⚠️ Sự kiện chưa xử lý: {eventType}");
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error processing webhook: {ex.Message}");
				Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
				return StatusCode(500, "Internal server error");
			}

			return Ok();
		}

		[HttpGet("CheckCommission")]
        public IActionResult CheckCommission(int userId, int month, int year)
        {
            return _paymentService.IsCommissionExist(userId, month, year)
                ? Ok(new { exists = true })
                : NotFound(new { exists = false });
        }

        [HttpPost("CreateCommission")]
        public async Task<IActionResult> CreateCommission([FromBody] CreateCommissionRequest request)
        {
            var payment = await _paymentService.CreateCommissionAsync(request);
            if (payment == null)
            {
                return StatusCode(500, "Không thể tạo commission");
            }
            return Ok(payment);
        }

		[HttpPost("CreateOLK")]
		public async Task<IActionResult> CreateOK([FromBody] CreateCommissionRequest request)
		{
			var payment = await _paymentService.CreateCommissionAsync(request);
			if (payment == null)
			{
				return StatusCode(500, "Không thể tạo commission");
			}
			return Ok(payment);
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
