using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using DnsClient;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace B2P_API.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ISMSService _sMSService;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IImageRepository _imageRepository;

        public UserService(
            IUserRepository userRepository,
            IEmailService emailService,
            ISMSService sMSService,
            IMemoryCache cache,
            IBankAccountRepository bankAccountRepository,
            IImageRepository imageRepository)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
            _sMSService = sMSService;
            _bankAccountRepository = bankAccountRepository;
            _imageRepository = imageRepository;
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

                if (!await IsRealEmailAsync(request.Email))
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

                if (!await IsRealEmailAsync(request.Email))
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

                if (!await IsRealEmailAsync(request.Email))
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

                // Send OTP SMS - Thêm error handling
                var smsResult = await _sMSService.SendOTPAsync(request.PhoneNumber, otp);

                // Kiểm tra kết quả gửi SMS
                if (!smsResult.Success)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Không thể gửi OTP: " + smsResult.Message,
                        Success = false,
                        Status = 500
                    };
                }


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

                if (!await IsRealEmailAsync(updateUserDto.Email))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Địa chỉ Email không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(updateUserDto.Address?.Trim()))
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

                if (age < 15)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Người dùng phải từ 15 tuổi trở lên",
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

                // Check email exists
                var existingEmail = await _userRepository.CheckEmailExistedByUserId(userId, updateUserDto.Email);
                if (existingEmail != null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Email đã được sử dụng",
                        Success = false,
                        Status = 400
                    };
                }

                // Update user properties
                user.FullName = updateUserDto.FullName;
                user.Email = updateUserDto.Email;
                user.IsMale = updateUserDto.IsMale;
                user.Address = updateUserDto.Address.Trim();
                user.Dob = updateUserDto.Dob;

                updateUserDto.AccountHolder = updateUserDto.AccountHolder?.Trim();
                updateUserDto.AccountNumber = updateUserDto.AccountNumber.Trim();

                // Validate bank account
                if (!IsValidBankAccount(updateUserDto.AccountNumber))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Số tài khoản không hợp lệ,chỉ chứa từ 9-16 ký tự",
                        Success = false,
                        Status = 400
                    };
                }

                if (string.IsNullOrEmpty(updateUserDto.AccountHolder))
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Tên chủ tài khoản không được để trống",
                        Success = false,
                        Status = 400
                    };
                }

                if (updateUserDto.AccountHolder.Length > 50)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Tên chủ tài khoản không được vượt quá 50 ký tự",
                        Success = false,
                        Status = 400
                    };
                }

                //Validate bank type
                if (updateUserDto.BankTypeId == null || updateUserDto.BankTypeId <=0)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Loại ngân hàng không hợp lệ",
                        Success = false,
                        Status = 400
                    };
                }
                var bankType = await _bankAccountRepository.GetBankTypeByIdAsync(updateUserDto.BankTypeId);
                if (bankType == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Không tìm thấy kiểu ngân hàng đã chọn",
                        Success = false,
                        Status = 400
                    };
                }

                //Update bank account
                var bankAccount = await _bankAccountRepository.GetBankAccountsByUserIdAsync(userId);
                if (bankAccount == null)
                {
                    // Create new bank account if it doesn't exist
                    bankAccount = new BankAccount
                    {
                        UserId = userId,
                        AccountNumber = updateUserDto.AccountNumber,
                        AccountHolder = updateUserDto.AccountHolder,
                        BankTypeId = updateUserDto.BankTypeId.Value
                    };
                    var createResult = await _bankAccountRepository.AddBankAccountAsync(bankAccount);
                    if (!createResult)
                    {
                        return new ApiResponse<object>
                        {
                            Data = null,
                            Message = "Tạo tài khoản ngân hàng thất bại",
                            Success = false,
                            Status = 500
                        };
                    }
                }
                else
                {
                    // Update existing bank account
                    bankAccount.AccountNumber = updateUserDto.AccountNumber;
                    bankAccount.AccountHolder = updateUserDto.AccountHolder;
                    bankAccount.BankTypeId = updateUserDto.BankTypeId.Value;
                    var updateResult = await _bankAccountRepository.UpdateBankAccountAsync(bankAccount);
                    if (!updateResult)
                    {
                        return new ApiResponse<object>
                        {
                            Data = null,
                            Message = "Cập nhật tài khoản ngân hàng thất bại",
                            Success = false,
                            Status = 500
                        };
                    }
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

        public async Task<bool> IsRealEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                var domain = addr.Host;

                var lookup = new LookupClient();
                var result = await lookup.QueryAsync(domain, QueryType.MX);

                return result.Answers.MxRecords().Any();
            }
            catch
            {
                return false;
            }
        }
        public bool IsValidBankAccount(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return false;

            if (!Regex.IsMatch(accountNumber, @"^[a-zA-Z0-9]+$"))
                return false;

            if (accountNumber.Length < 9 || accountNumber.Length > 16)
                return false;

            return true;
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
        private bool IsValidPhoneNumber(string phone)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[3-9]\d{8}$");
        }

    }
}
