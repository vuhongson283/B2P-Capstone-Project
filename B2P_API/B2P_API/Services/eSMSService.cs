using B2P_API.Interface;
using B2P_API.Repository;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Verify.V2.Service;
using Twilio.Types;
namespace B2P_API.Services
{
    public class eSMSService : ISMSService
    {
        
            private static readonly HttpClient client = new HttpClient();
            private const string ApiKey = "3D7D79DECD4B5C70EA0C4EAFC0B376";
            private const string SecretKey = "5B4405CC7D3FD6705B8DCDB56D53DD"; 
            private const string CampaignId = "Cảm ơn đã sử dụng dịch vụ của B2P";

            // Gửi OTP
            public async Task<ApiResponse<object>> SendOTPAsync(string phoneNumber, string otp)
            {
            try
            {
                var url = "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";

                var requestBody = new
                {
                    ApiKey = ApiKey,
                    Content = $"{otp} la ma xac minh dang ky cua ban",
                    Phone = phoneNumber,
                    SecretKey = SecretKey,
                    SmsType = "2",
                    IsUnicode = "1",
                    campaignid = CampaignId,
                    RequestId = Guid.NewGuid().ToString(),
                    CallbackUrl = "https://esms.vn/webhook/"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = responseContent,
                    Status = 200,
                    Data = null
                };
            }
            catch(Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Status = 200,
                    Data = null
                };
            }
            }

            // Gửi SMS tùy ý
            public async Task<ApiResponse<object>> SendSMSAsync(string phoneNumber, string message)
            {
            try
            {
                var url = "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";

                var requestBody = new
                {
                    ApiKey = ApiKey,
                    Content = message,
                    Phone = phoneNumber,
                    SecretKey = SecretKey,
                    SmsType = "2",
                    IsUnicode = "1",
                    campaignid = CampaignId,
                    RequestId = Guid.NewGuid().ToString(),
                    CallbackUrl = "https://esms.vn/webhook/"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = responseContent,
                    Status = 200,
                    Data = null
                };
            }
            catch(Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Status = 200,
                    Data = null
                };
            }
            }

    }
    
}
