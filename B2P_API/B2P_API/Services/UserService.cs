using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace B2P_API.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ISMSService _sMSService;

        public UserService(
            IUserRepository userRepository,
            IEmailService emailService,
            ISMSService sMSService,
            IMemoryCache cache)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
            _sMSService = sMSService;
        }

        public async Task<ApiResponse<object>> SendPasswordResetOtpByEmailAsync(ForgotPasswordRequestByEmailDto? request)
        {
            try
            {
                // Validate request object
                if (request == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Email không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!IsValidEmail(request.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_08,
                        Success = false,
                        Status = 400
                    };
                }

                var email = request.Email.Trim();
                var user = await _userRepository.GetUserByEmailAsync(request.Email);

                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_11,
                        Success = false,
                        Status = 404
                    };
                }

                if (user.StatusId != 1)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_09,
                        Success = false,
                        Status = 404
                    };
                }

                // Generate OTP
                var otp = GenerateSecureOtp();
                var cacheKey = $"password_reset_otp_{request.Email}";

                // Store OTP in cache with 5 minutes expiration
                var otpData = new
                {
                    UserId = user.UserId,
                    OtpCode = otp,
                    CreatedAt = DateTime.UtcNow,
                    Email = request.Email
                };

                _cache.Set(cacheKey, otpData, TimeSpan.FromMinutes(5));

                // Send OTP email
                await _emailService.SendOtpEmailAsync(request.Email, otp);

                return new ApiResponse<object>
                {
                    Data = null,
                    Message = "Mã OTP đã được gửi đến email của bạn.",
                    Success = true,
                    Status = 200
                };


            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<object>> VerifyOtpAndResetPasswordByEmailAsync(VerifyOtpDtoByEmail? request)
        {
            try
            {

                // Validate request object
                if (request == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Email không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!IsValidEmail(request.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_68,
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.OtpCode.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mã OTP không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.OtpCode, @"^\d{6}$"))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mã OTP phải gồm 6 chữ số",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.NewPassword.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mật khẩu mới không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (request.NewPassword.Length < 8)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_13,
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.ConfirmPassword.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Xác nhận mật khẩu không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!request.NewPassword.Equals(request.ConfirmPassword))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_14,
                        Success = false,
                        Status = 400
                    };
                }

                var cacheKey = $"password_reset_otp_{request.Email}";

                // Check if OTP exists and is valid
                if (!_cache.TryGetValue(cacheKey, out var cachedOtpData)) {

                    return new ApiResponse<object>{
                        Data = null,
                        Message = MessagesCodes.MSG_12,
                        Success = false,
                        Status = 400
                    };
                };
                
                var otpData = (dynamic)cachedOtpData;

                if (otpData.OtpCode != request.OtpCode)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_12,
                        Success = false,
                        Status = 400
                    };
                }

                _cache.Remove(cacheKey);

                // Get user and update password
                var user = await _userRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return new ApiResponse<object>{
                        Data = null,
                        Message = MessagesCodes.MSG_65,
                        Success = false,
                        Status = 404
                    };
                }

                if (!request.NewPassword.Equals(request.ConfirmPassword))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_14,
                        Success = false,
                        Status = 400
                    };
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _userRepository.UpdateUserAsync(user);

                return new ApiResponse<object>{
                    Data = null,
                    Message = MessagesCodes.MSG_10,
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<object>> ResendPasswordResetOtpByEmailAsync(ResendOtpDtoByEmail? request)
        {
            try
            {
                // Validate request object
                if (request == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Email không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!IsValidEmail(request.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_68,
                        Success = false,
                        Status = 400
                    };
                }

                request.Email = request.Email.Trim();

                var user = await _userRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Email không tồn tại trong hệ thống",
                        Success = false,
                        Status = 404
                    };
                }

                if (user.StatusId != 1)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_09,
                        Success = false,
                        Status = 400
                    };
                }

                // Check rate limiting - prevent spam
                var rateLimitKey = $"rate_limit_otp_{request.Email}";
                if (_cache.TryGetValue(rateLimitKey, out _))
                {
                    return new ApiResponse<object>{
                        Data = null,
                        Message = "Vui lòng đợi 1 phút trước khi gửi lại OTP",
                        Success = false,
                        Status = 500
                    };
                }

                // Set rate limit for 1 minute
                _cache.Set(rateLimitKey, true, TimeSpan.FromMinutes(1));

                var resend = new ForgotPasswordRequestByEmailDto { Email = request.Email };
                return await SendPasswordResetOtpByEmailAsync(resend);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
        {
            try
            {
                // Validate request
                if (changePasswordRequest == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (changePasswordRequest.UserId <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "UserId không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(changePasswordRequest.OldPassword.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mật khẩu cũ không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(changePasswordRequest.NewPassword.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mật khẩu mới không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (changePasswordRequest.NewPassword.Length < 8)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mật khẩu có ít nhất 8 ký tự",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(changePasswordRequest.ConfirmPassword.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Xác nhận mật khẩu không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (changePasswordRequest.NewPassword != changePasswordRequest.ConfirmPassword)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_14,
                        Success = false,
                        Status = 400
                    };
                }

                if (changePasswordRequest.OldPassword == changePasswordRequest.NewPassword)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mật khẩu mới phải khác mật khẩu cũ",
                        Success = false,
                        Status = 400
                    };
                }

                if (changePasswordRequest.NewPassword != changePasswordRequest.ConfirmPassword)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_14,
                        Success = false,
                        Status = 400
                    };
                }

                // Check if user exists
                var user = await _userRepository.GetUserByIdAsync(changePasswordRequest.UserId);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_65,
                        Success = false,
                        Status = 404
                    };
                }

                // Verify old password
                bool isValid = BCrypt.Net.BCrypt.Verify(changePasswordRequest.OldPassword, user.Password);
                if (!isValid)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_15,
                        Success = false,
                        Status = 400
                    };
                }

                // Hash new password before saving
                user.Password = BCrypt.Net.BCrypt.HashPassword(changePasswordRequest.NewPassword);

                var result = await _userRepository.UpdateUserAsync(user);
                if (!result)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Đổi mật khẩu thất bại",
                        Success = false,
                        Status = 500
                    };
                }

                return new ApiResponse<object>
                {
                    Data = null,
                    Message = "Đổi mật khẩu thành công",
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }

        public async Task<ApiResponse<object>> SendPasswordResetOtpBySMSAsync(ForgotPasswordRequestBySmsDto? request)
        {
            try
            {
                // Validate request object
                if (request == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.PhoneNumber) || !IsValidPhoneNumber(request.PhoneNumber))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Số điện thoại không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                var email = request.PhoneNumber.Trim();
                var user = await _userRepository.GetUserByPhoneAsync(request.PhoneNumber);

                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Số điện thoại không tồn tại trong hệ thống",
                        Success = false,
                        Status = 404
                    };
                }

                if (user.StatusId != 1)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_09,
                        Success = false,
                        Status = 404
                    };
                }

                // Generate OTP
                var otp = GenerateSecureOtp();
                var cacheKey = $"password_reset_otp_{request.PhoneNumber}";

                // Store OTP in cache with 5 minutes expiration
                var otpData = new
                {
                    UserId = user.UserId,
                    OtpCode = otp,
                    CreatedAt = DateTime.UtcNow,
                    PhoneNumber = request.PhoneNumber
                };

                _cache.Set(cacheKey, otpData, TimeSpan.FromMinutes(5));

                // Send OTP SMS
                await _sMSService.SendOTPAsync(request.PhoneNumber, otp);

                return new ApiResponse<object>
                {
                    Data = null,
                    Message = "Mã OTP đã được gửi đến tin nhắn của bạn.",
                    Success = true,
                    Status = 200
                };


            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<object>> VerifyOtpAndResetPasswordBySMSAsync(VerifyOtpBySmsDto? request)
        {
            try
            {

                // Validate request object
                if (request == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.PhoneNumber)|| !IsValidPhoneNumber(request.PhoneNumber))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Số điện thoại không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }


                if (string.IsNullOrEmpty(request.OtpCode.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mã OTP không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.OtpCode, @"^\d{6}$"))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mã OTP phải gồm 6 chữ số",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.NewPassword.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Mật khẩu mới không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (request.NewPassword.Length < 8)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_13,
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.ConfirmPassword.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Xác nhận mật khẩu không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!request.NewPassword.Equals(request.ConfirmPassword))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_14,
                        Success = false,
                        Status = 400
                    };
                }

                var cacheKey = $"password_reset_otp_{request.PhoneNumber}";

                // Check if OTP exists and is valid
                if (!_cache.TryGetValue(cacheKey, out var cachedOtpData))
                {

                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_12,
                        Success = false,
                        Status = 400
                    };
                }
                ;

                var otpData = (dynamic)cachedOtpData;

                if (otpData.OtpCode != request.OtpCode)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_12,
                        Success = false,
                        Status = 400
                    };
                }

                _cache.Remove(cacheKey);

                // Get user and update password
                var user = await _userRepository.GetUserByPhoneAsync(request.PhoneNumber);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_65,
                        Success = false,
                        Status = 404
                    };
                }

                if (!request.NewPassword.Equals(request.ConfirmPassword))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_14,
                        Success = false,
                        Status = 400
                    };
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _userRepository.UpdateUserAsync(user);

                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_10,
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<object>> ResendPasswordResetOtpBySMSAsync(ResendOtpBySmsDto? request)
        {
            try
            {
                // Validate request object
                if (request == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(request.PhoneNumber)|| !IsValidPhoneNumber(request.PhoneNumber))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Số điện thoại không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                request.PhoneNumber = request.PhoneNumber.Trim();

                var user = await _userRepository.GetUserByPhoneAsync(request.PhoneNumber);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Số điện thoại không tồn tại trong hệ thống",
                        Success = false,
                        Status = 404
                    };
                }

                if (user.StatusId != 1)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_09,
                        Success = false,
                        Status = 400
                    };
                }

                // Check rate limiting - prevent spam
                var rateLimitKey = $"rate_limit_otp_{request.PhoneNumber}";
                if (_cache.TryGetValue(rateLimitKey, out _))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Vui lòng đợi 1 phút trước khi gửi lại OTP",
                        Success = false,
                        Status = 500
                    };
                }

                // Set rate limit for 1 minute
                _cache.Set(rateLimitKey, true, TimeSpan.FromMinutes(1));

                var resend = new ForgotPasswordRequestBySmsDto { PhoneNumber = request.PhoneNumber };
                return await SendPasswordResetOtpBySMSAsync(resend);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }

        public async Task<ApiResponse<UserInfoResponse?>> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<UserInfoResponse?>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_65,
                        Success = false,
                        Status = 404
                    };
                }

                var userInfo = new UserInfoResponse
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsMale = user.IsMale,
                    Dob = user.Dob,
                    Address=user.Address,
                    CreateAt = user.CreateAt,
                    AccountHolder = user.BankAccount?.AccountHolder ?? null,
                    AccountNumber = user.BankAccount?.AccountNumber ?? string.Empty,
                    BankTypeId = user.BankAccount?.BankTypeId ?? 0,
                    BankName = user.BankAccount?.BankType?.BankName ?? "Unknown",
                    StatusDescription = user.Status?.StatusDescription ?? string.Empty,
                    ImageId = user.Images.FirstOrDefault()?.ImageId ?? 0,
                    ImageUrl = user.Images.FirstOrDefault()?.ImageUrl ?? string.Empty
                };

                return new ApiResponse<UserInfoResponse?>
                {
                    Data = userInfo,
                    Message = "Tải thông tin người dùng thành công",
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserInfoResponse?>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }
        public async Task<ApiResponse<object>> UpdateUserAsync(int userId, UpdateUserRequest updateUserDto)
        {
            try
            {
                // Validate request
                if (updateUserDto == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Dữ liệu không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (userId <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "UserId không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(updateUserDto.FullName.Trim()))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Tên người dùng không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (updateUserDto.FullName.Length > 50)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Tên người dùng không được vượt quá 50 ký tự",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(updateUserDto.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Email không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (!IsValidEmail(updateUserDto.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Địa chỉ Email không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(updateUserDto.Address))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Địa chỉ không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (updateUserDto.Address.Length > 255)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Địa chỉ không được vượt quá 255 ký tự",
                        Success = false,
                        Status = 400
                    };
                }

                if (!updateUserDto.Dob.HasValue)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Ngày sinh không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                var today = DateOnly.FromDateTime(DateTime.Today);
                var age = today.Year - updateUserDto.Dob.Value.Year;
                if (updateUserDto.Dob.Value > today.AddYears(-age)) age--;

                if (age < 18)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Người dùng phải từ 18 tuổi trở lên",
                        Success = false,
                        Status = 400
                    };
                }

                if (updateUserDto.Dob.Value > today)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Ngày sinh không được là ngày tương lai",
                        Success = false,
                        Status = 400
                    };
                }


                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_65,
                        Success = false,
                        Status = 404
                    };
                }

                // Check phone exists
                var existingEmail = await _userRepository.CheckPhoneExistedByUserId(userId, updateUserDto.Email);
                if (existingEmail != null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_66,
                        Success = false,
                        Status = 400
                    };
                }

                // Update user properties
                user.FullName = updateUserDto.FullName;
                user.Email = updateUserDto.Email;
                user.IsMale = updateUserDto.IsMale;
                user.Address = updateUserDto.Address;
                user.Dob = updateUserDto.Dob;

                // Update image
                var updatedImage = await _userRepository.GetImageByUserId(userId);
                if (updatedImage == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Không tìm được ảnh",
                        Success = false,
                        Status = 404
                    };
                }

                updatedImage.ImageUrl = updateUserDto.ImageUrl;
                var updateImageResult = await _userRepository.UpdateAvatar(updatedImage);
                if (!updateImageResult)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_67,
                        Success = false,
                        Status = 500
                    };
                }

                // Update user
                var updatedUserResult = await _userRepository.UpdateUserAsync(user);
                if (!updatedUserResult)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Cập nhật thông tin người dùng thất bại",
                        Success = false,
                        Status = 500
                    };
                }

                return new ApiResponse<object>
                {
                    Data = null,
                    Message = "Cập nhật thông tin người dùng thành công",
                    Success = true,
                    Status = 200
                };
            }
            catch (Exception ex)
            {

                return new ApiResponse<object>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }

        private string GenerateSecureOtp()
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
                return (randomNumber % 900000 + 100000).ToString(); // 6 digits OTP
            }
        }
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhoneNumber(string phone)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[3-9]\d{8}$");
        }

    }
}
