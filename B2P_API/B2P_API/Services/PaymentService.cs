namespace B2P_API.Services
{
    using B2P_API.Models;
    using B2P_API.Repositories;
    using B2P_API.Repository;
    using Newtonsoft.Json;
    using System.Security.Cryptography;
    using System.Text;

    public class PaymentService
    {
        private readonly PaymentRepository _paymentRepo;
        private readonly BookingRepository _bookingRepo;

        public PaymentService(PaymentRepository paymentRepo, BookingRepository bookingRepo)
        {
            _paymentRepo = paymentRepo;
            _bookingRepo = bookingRepo;
        }

        public async Task<string> CreatePaymentAsync(int bookingId, decimal amount)
        {
            var booking = _bookingRepo.GetById(bookingId);
            if (booking == null) throw new Exception("Booking not found");

            var payment = new Payment
            {
                BookingId = bookingId,
                StatusId = 1, // Pending
                Amount = amount,
                TimeStamp = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);

            var momoUrl = await CreateMomoPaymentLinkAsync(payment);
            return momoUrl;
        }

        public async Task HandleCallbackAsync(int paymentId, bool success)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId);
            if (payment == null) return;

            payment.StatusId = success ? 2 : 3; // Success = 2, Failed = 3
            payment.TimeStamp = DateTime.UtcNow;

            await _paymentRepo.UpdateAsync(payment);
        }

        private async Task<string> CreateMomoPaymentLinkAsync(Payment payment)
        {
            // Test endpoint của momo, live thì thay URL khác

            string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
            string partnerCode = "MOMO";
            string accessKey = "F8BBA842ECF85";
            string secretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
            string redirectUrl = "https://momo.vn";
            string ipnUrl = "https://webhook.site/5bdbfd27-9913-48d7-91b2-40c4c4b75df1"; 



            var orderId = $"PAY{DateTime.Now:yyyyMMddHHmmssfff}";
            var requestId = $"{partnerCode}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";


            var rawHash =
                $"accessKey={accessKey}&amount={payment.Amount}&extraData=&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo=Thanh toán booking {payment.BookingId}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType=captureWallet";

            // Ký SHA256
            var signature = Convert.ToHexString(
                new HMACSHA256(Encoding.UTF8.GetBytes(secretKey))
                    .ComputeHash(Encoding.UTF8.GetBytes(rawHash))
            ).ToLower();

            var requestBody = new
            {
                partnerCode,
                partnerName = "B2P",
                storeId = "B2P",
                requestId,
                amount = payment.Amount,
                orderId,
                orderInfo = $"Thanh toán booking {payment.BookingId}",
                redirectUrl,
                ipnUrl,
                lang = "vi",
                extraData = "",
                requestType = "captureWallet",
                signature
            };

            using var client = new HttpClient();
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody); // <-- in ra để đọc lỗi trả về
            dynamic resObj = JsonConvert.DeserializeObject(responseBody)!;
            return resObj.payUrl;

        }
    }

}
