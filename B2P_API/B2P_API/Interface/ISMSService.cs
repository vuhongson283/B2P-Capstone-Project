using B2P_API.Response;

namespace B2P_API.Interface
{
    public interface ISMSService
    {
        Task<ApiResponse<object>> SendOTPAsync(string phoneNumber,string otp);
        Task<ApiResponse<object>> SendSMSAsync(string phoneNumber, string message);
    }
}
