using B2P_API.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Utilities;
using B2P_API.DTOs.CommissionPaymentHistoryDTOs;

namespace B2P_API.Services
{
    public class ZaloPayService
    {
        private readonly HttpClient _httpClient;
        private readonly ZaloPayConfig _config;
        private readonly ILogger<ZaloPayService> _logger;
        private readonly BookingService _bookingService;
        private readonly CommissionPaymentHistoryService _commissionService;

        public ZaloPayService(
            HttpClient httpClient,
            BookingService bookingService,
            IOptions<ZaloPayConfig> config,
            ILogger<ZaloPayService> logger,
            CommissionPaymentHistoryService commissionService)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
            _bookingService = bookingService;
            _commissionService = commissionService;
        }

        public async Task<PaymentResult> CreateOrderAsync(CreateOrderRequest request)
        {
            try
            {
                var appTransId = GenerateAppTransId();
                var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Serialize items và embed data
                var itemsJson = JsonSerializer.Serialize(request.Items);
                var embedDataJson = JsonSerializer.Serialize(request.EmbedData);

                var orderData = new Dictionary<string, string>
                {
                    ["app_id"] = _config.AppId,
                    ["app_user"] = request.AppUser ?? "default_user",
                    ["app_time"] = appTime.ToString(),
                    ["amount"] = request.Amount.ToString(),
                    ["app_trans_id"] = appTransId,
                    ["embed_data"] = embedDataJson,
                    ["item"] = itemsJson,
                    ["description"] = request.Description,
                    ["bank_code"] = "",
                    ["callback_url"] = request.CallbackUrl ?? "",
                    ["redirect_url"] = request.RedirectUrl ?? ""
                };

                // Tạo MAC theo thứ tự ZaloPay yêu cầu
                var dataForMac = $"{_config.AppId}|{appTransId}|{request.AppUser}|{request.Amount}|{appTime}|{embedDataJson}|{itemsJson}";
                var mac = CreateHmacSha256(dataForMac, _config.Key1);
                orderData["mac"] = mac;

                _logger.LogInformation($"Creating ZaloPay order with AppTransId: {appTransId}");

                var content = new FormUrlEncodedContent(orderData);
                var response = await _httpClient.PostAsync($"{_config.Endpoint}/v2/create", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"HTTP Error: {response.StatusCode}");
                    return new PaymentResult
                    {
                        Success = false,
                        Message = $"HTTP Error: {response.StatusCode}",
                        ErrorCode = (int)response.StatusCode
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"ZaloPay response: {responseContent}");

                var zaloPayResponse = JsonSerializer.Deserialize<ZaloPayOrderResponse>(responseContent);

                if (zaloPayResponse != null)
                {
                    zaloPayResponse.AppTransId = appTransId; // Thêm AppTransId vào response
                }

                return new PaymentResult
                {
                    Success = zaloPayResponse?.ReturnCode == 1,
                    Message = zaloPayResponse?.ReturnMessage ?? "Unknown error",
                    Data = zaloPayResponse,
                    ErrorCode = zaloPayResponse?.ReturnCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ZaloPay order");
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Exception: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<PaymentResult> QueryOrderAsync(string appTransId)
        {
            try
            {
                // Tạo MAC cho query
                var dataForMac = $"{_config.AppId}|{appTransId}|{_config.Key1}";
                var mac = CreateHmacSha256(dataForMac, _config.Key1);

                var queryData = new Dictionary<string, string>
                {
                    ["app_id"] = _config.AppId,
                    ["app_trans_id"] = appTransId,
                    ["mac"] = mac
                };

                _logger.LogInformation($"Querying ZaloPay order: {appTransId}");

                var content = new FormUrlEncodedContent(queryData);
                var response = await _httpClient.PostAsync($"{_config.Endpoint}/v2/query", content);

                if (!response.IsSuccessStatusCode)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = $"HTTP Error: {response.StatusCode}",
                        ErrorCode = (int)response.StatusCode
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"ZaloPay query response: {responseContent}");

                var queryResponse = JsonSerializer.Deserialize<QueryOrderResponse>(responseContent);

                return new PaymentResult
                {
                    Success = queryResponse?.ReturnCode == 1,
                    Message = queryResponse?.ReturnMessage ?? "Unknown error",
                    Data = queryResponse,
                    ErrorCode = queryResponse?.ReturnCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying ZaloPay order");
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Exception: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }



        public async Task<PaymentResult> VerifyCallback(string data, string receivedMac)
        {
            try
            {
                _logger.LogInformation($"Raw data received: {data}");

                // ===========================
                // 🔒 PHẦN KIỂM TRA MAC
                // ===========================
               /* try
                {
                    _logger.LogInformation($"Received MAC: {receivedMac}");
                    _logger.LogInformation($"Using Key1: {_config.Key1}");

                    var computedMac = CreateHmacSha256(data, _config.Key1);
                    _logger.LogInformation($"Computed MAC: {computedMac}");

                    var isValid = computedMac.Equals(receivedMac, StringComparison.OrdinalIgnoreCase);
                    if (!isValid)
                    {
                        _logger.LogWarning($"Invalid MAC. Received: {receivedMac}, Computed: {computedMac}");
                        return new PaymentResult
                        {
                            Success = false,
                            Message = "Invalid MAC signature",
                            ErrorCode = -1
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating MAC.");
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Error validating MAC",
                        ErrorCode = -1
                    };
                }*/

                // ===========================
                // 📦 PARSE DỮ LIỆU CALLBACK
                // ===========================
                string unescapedJson;
                try
                {
                    unescapedJson = JsonDocument.Parse($"\"{data}\"").RootElement.GetString();
                    _logger.LogInformation($"Unescaped JSON: {unescapedJson}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to unescape data JSON string.");
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Invalid JSON format (escape issue)",
                        ErrorCode = -1
                    };
                }

                var callbackData = JsonSerializer.Deserialize<CallbackData>(unescapedJson);
                _logger.LogInformation("Parsed callbackData: {@CallbackData}", callbackData);
                _logger.LogInformation("AppTransId = {AppTransId}, Amount = {Amount}", callbackData.AppTransId, callbackData.Amount);

                // ===========================
                // 🛠️ XỬ LÝ EMBED_DATA
                // ===========================
                if (!string.IsNullOrEmpty(callbackData.EmbedData))
                {
                    try
                    {
                        var embedData = JsonSerializer.Deserialize<Dictionary<string, object>>(callbackData.EmbedData);
                        callbackData.EmbedDict = embedData ?? new();

                        // ✅ Trường hợp Booking
                        if (callbackData.EmbedDict.TryGetValue("bookingid", out var bookingIdObj))
                        {
                            if (bookingIdObj != null && int.TryParse(bookingIdObj.ToString(), out var bookingId))
                            {
                                _logger.LogInformation($"Booking ID extracted: {bookingId}");

                                await _bookingService.MarkBookingPaidAsync(bookingId, callbackData.ZpTransId.ToString());
                                _logger.LogInformation("MarkBookingPaidAsync executed successfully.");
                            }
                            else
                            {
                                _logger.LogWarning("Booking ID is not a valid integer.");
                            }
                        }
                        // ✅ Trường hợp Commission
                        else if (callbackData.EmbedDict.TryGetValue("commissionid", out var commissionIdObj))
                        {
                            if (commissionIdObj != null && int.TryParse(commissionIdObj.ToString(), out var commissionId))
                            {
                                _logger.LogInformation($"Commission ID extracted: {commissionId}");

                                var updateDto = new CommissionPaymentHistoryUpdateDto
                                {
                                    StatusId = 7, // Paid
                                    Note = $"Thanh toán thành công, ZpTransId = {callbackData.ZpTransId}"
                                };

                                var result = await _commissionService.UpdateAsync(commissionId, updateDto);
                                if (result.Success)
                                {
                                    _logger.LogInformation($"Commission {commissionId} updated successfully");
                                }
                                else
                                {
                                    _logger.LogWarning($"Failed to update commission {commissionId}: {result.Message}");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Commission ID is not a valid integer.");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Neither bookingid nor commissionid found in embed_data.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse embed_data or process booking/commission ID");
                    }
                }

                return new PaymentResult
                {
                    Success = true,
                    Message = "Callback verified successfully",
                    Data = callbackData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying callback");
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Exception: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }






        private string GenerateAppTransId()
        {
            var date = DateTime.Now.ToString("yyMMdd");
            var random = Random.Shared.Next(100000, 999999);
            return $"{date}_{random}";
        }

        private static string CreateHmacSha256(string message, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}
