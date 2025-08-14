using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using B2P_API.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace B2P_API.Services
{
    public class VNPayService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<VNPayService> _logger;
        private readonly HttpClient _httpClient;

        public VNPayService(
            IConfiguration config,
            ILogger<VNPayService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public string CreatePaymentUrl(decimal amount, string orderId, string orderInfo, string ipAddress)
        {
            if (amount <= 0) throw new ArgumentException("Số tiền phải lớn hơn 0");
            if (string.IsNullOrEmpty(orderId)) throw new ArgumentException("OrderId không được trống");

            try
            {
                var vnpay = new VnPayLibrary();
                var vnpayConfig = _config.GetSection("VNPay");

                // Required parameters
                var requestData = new Dictionary<string, string>
                {
                    ["vnp_Version"] = "2.1.0",
                    ["vnp_Command"] = vnpayConfig["Command"],
                    ["vnp_TmnCode"] = vnpayConfig["TmnCode"],
                    ["vnp_Amount"] = ((int)(amount * 100m)).ToString(),
                    ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    ["vnp_CurrCode"] = "VND",
                    ["vnp_IpAddr"] = ipAddress ?? "127.0.0.1",
                    ["vnp_Locale"] = "vn",
                    ["vnp_OrderInfo"] = orderInfo,
                    ["vnp_OrderType"] = "other",
                    ["vnp_ReturnUrl"] = vnpayConfig["ReturnUrl"],
                    ["vnp_TxnRef"] = orderId
                };

                foreach (var item in requestData)
                {
                    vnpay.AddRequestData(item.Key, item.Value);
                }

                string paymentUrl = vnpay.CreateRequestUrl(
                    vnpayConfig["PaymentUrl"],
                    vnpayConfig["HashSecret"]);

                _logger.LogInformation($"Created VNPay URL for order {orderId}");
                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating URL for order {orderId}");
                throw new ApplicationException("Lỗi khi tạo URL thanh toán", ex);
            }
        }

        public PaymentValidationResult ValidateResponse(Dictionary<string, string> parameters)
        {
            try
            {
                var vnpay = new VnPayLibrary();
                var responseData = new Dictionary<string, string>();

                foreach (var param in parameters)
                {
                    if (!string.IsNullOrEmpty(param.Value))
                    {
                        vnpay.AddResponseData(param.Key, param.Value);
                        responseData[param.Key] = param.Value;
                    }
                }

                if (!parameters.TryGetValue("vnp_SecureHash", out var secureHash))
                {
                    _logger.LogWarning("Missing secure hash in response");
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Thiếu chữ ký bảo mật"
                    };
                }

                bool isValid = vnpay.ValidateSignature(
                    secureHash,
                    _config["VNPay:HashSecret"]);

                return new PaymentValidationResult
                {
                    IsValid = isValid,
                    ResponseData = responseData,
                    ErrorMessage = isValid ? null : "Chữ ký không hợp lệ"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment response");
                return new PaymentValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Lỗi hệ thống khi xác thực"
                };
            }
        }

        public async Task<PaymentCaptureResult> CapturePaymentAsync(string orderId, string vnpTransactionNo, string transactionDate)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentNullException(nameof(orderId));

            try
            {
                var vnpayConfig = _config.GetSection("VNPay");

                var request = new
                {
                    vnp_RequestId = Guid.NewGuid().ToString(),
                    vnp_Version = "2.1.0",
                    vnp_Command = "capture",
                    vnp_TmnCode = vnpayConfig["TmnCode"],
                    vnp_TxnRef = orderId,
                    vnp_TransactionNo = vnpTransactionNo,
                    vnp_TransactionDate = transactionDate,
                    vnp_CreateBy = "B2P_API"
                };

                var content = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    vnpayConfig["ApiUrl"],
                    content);

                if (response.IsSuccessStatusCode)
                {
                    return new PaymentCaptureResult
                    {
                        IsSuccess = true,
                        OrderId = orderId,
                        TransactionNo = vnpTransactionNo,
                        CaptureDate = DateTime.Now,
                        Message = "Capture thành công"
                    };
                }

                return new PaymentCaptureResult
                {
                    IsSuccess = false,
                    Message = await GetErrorMessage(response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Capture failed for order {orderId}");
                return new PaymentCaptureResult
                {
                    IsSuccess = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        private async Task<string> GetErrorMessage(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    return $"VNPay error: {content}";
                }
                return $"VNPay error: {response.ReasonPhrase}";
            }
            catch
            {
                return "Không thể đọc thông báo lỗi từ VNPay";
            }
        }
    }

    public class PaymentValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, string> ResponseData { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PaymentCaptureResult
    {
        public bool IsSuccess { get; set; }
        public string OrderId { get; set; }
        public string TransactionNo { get; set; }
        public DateTime CaptureDate { get; set; }
        public string Message { get; set; }
    }
}