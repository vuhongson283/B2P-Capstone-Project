using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace B2P_API.Services
{
    public class eSmsService : ISMSService
    {
        private readonly ESMSSettings _settings;
        private readonly HttpClient _httpClient;
        private const string EsmsSendUrl = "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";

        public eSmsService(IOptions<ESMSSettings> options)
        {
            _settings = options.Value;
            _httpClient = new HttpClient();
        }

        public async Task<ApiResponse<object>> SendOTPAsync(string phoneNumber, string otp)
        {
            string content = $"{otp} la ma xac minh dang ky Baotrixemay cua ban";
            return await SendSMSInternal(phoneNumber, content);
        }

        public async Task<ApiResponse<object>> SendSMSAsync(string phoneNumber, string message)
        {
            string content = message;
            return await SendSMSInternal(phoneNumber, content);
        }

        private async Task<ApiResponse<object>> SendSMSInternal(string phone, string content)
        {
            var requestId = Guid.NewGuid().ToString();

            var requestBody = new
            {
                ApiKey = _settings.ApiKey,
                SecretKey = _settings.SecretKey,
                Phone = phone,
                Brandname = "Baotrixemay",
                Content = content,
                SmsType = "2",
                IsUnicode = "0",
                RequestId = requestId,
                CallbackUrl = "https://esms.vn/webhook/"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(EsmsSendUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                var esmsResponse = JsonSerializer.Deserialize<ESMSResponse>(responseContent);

                if (esmsResponse != null && esmsResponse.CodeResult == "100")
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = $"SMS đã được gửi đến {phone}",
                        Status = 200,
                        Data = new { SMSID = esmsResponse.SMSID, Phone = phone, Content = content }
                    };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Gửi SMS thất bại: " + (esmsResponse?.ErrorMessage ?? "Không rõ lỗi"),
                        Status = 500,
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi gửi SMS: " + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
        }

        private class ESMSResponse
        {
            public string CodeResult { get; set; }
            public int CountRegenerate { get; set; }
            public string SMSID { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}
