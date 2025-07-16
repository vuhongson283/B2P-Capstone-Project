using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace B2P_API.Services
{
    public class TwilioSMSService : ISMSService
    {
        private readonly TwilioSettings _twilioSettings;

        public TwilioSMSService(IOptions<TwilioSettings> twilioSettings)
        {
            _twilioSettings = twilioSettings.Value;
            TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);
        }

        // Format số điện thoại Việt Nam
        private string FormatVietnamesePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            // Loại bỏ khoảng trắng và ký tự đặc biệt
            phoneNumber = phoneNumber.Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(".", "");

            // Nếu đã có mã quốc gia +84, trả về luôn
            if (phoneNumber.StartsWith("+84"))
            {
                return phoneNumber;
            }

            // Nếu bắt đầu bằng 84 (không có +)
            if (phoneNumber.StartsWith("84") && phoneNumber.Length >= 10)
            {
                return "+" + phoneNumber;
            }

            // Nếu bắt đầu bằng 0 (số điện thoại trong nước)
            if (phoneNumber.StartsWith("0") && phoneNumber.Length >= 10)
            {
                return "+84" + phoneNumber.Substring(1);
            }

            // Nếu không bắt đầu bằng 0 hoặc 84, thêm +84
            if (phoneNumber.Length >= 9)
            {
                return "+84" + phoneNumber;
            }

            // Trả về số gốc nếu không match pattern nào
            return phoneNumber;
        }

        // Gửi OTP
        public async Task<ApiResponse<object>> SendOTPAsync(string phoneNumber, string otp)
        {
            try
            {
                // Validation input
                if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(otp))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Số điện thoại và mã OTP không được để trống",
                        Status = 400,
                        Data = null
                    };
                }

                // Format số điện thoại Việt Nam
                var formattedPhone = FormatVietnamesePhoneNumber(phoneNumber);

                var message = await MessageResource.CreateAsync(
                    body: $"{otp} là mã xác minh từ B2P - BookToPlay. Mã này có hiệu lực trong 5 phút.",
                    from: new PhoneNumber(_twilioSettings.PhoneNumber),
                    to: new PhoneNumber(formattedPhone)
                );

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = $"OTP đã được gửi thành công đến {formattedPhone}",
                    Status = 200,
                    Data = new
                    {
                        MessageSid = message.Sid,
                        Status = message.Status.ToString(),
                        FormattedPhone = formattedPhone
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
        }

        // Gửi SMS tùy ý
        public async Task<ApiResponse<object>> SendSMSAsync(string phoneNumber, string message)
        {
            try
            {
                // Validation input
                if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Số điện thoại và nội dung tin nhắn không được để trống",
                        Status = 400,
                        Data = null
                    };
                }

                // Format số điện thoại Việt Nam
                var formattedPhone = FormatVietnamesePhoneNumber(phoneNumber);

                var smsMessage = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_twilioSettings.PhoneNumber),
                    to: new PhoneNumber(formattedPhone)
                );

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = $"SMS đã được gửi thành công đến {formattedPhone}",
                    Status = 200,
                    Data = new
                    {
                        MessageSid = smsMessage.Sid,
                        Status = smsMessage.Status.ToString(),
                        FormattedPhone = formattedPhone
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Status = 500,
                    Data = null
                };
            }
        }
    }
}
