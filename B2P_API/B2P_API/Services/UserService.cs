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

        public virtual async Task<ApiResponse<object>> SendPasswordResetOtpByEmailAsync(ForgotPasswordRequestByEmailDto? request)
        {
            try
            {
                // Validate request object
                if (request == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_80,
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
                    Message = MessagesCodes.MSG_91,
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

                if (!Regex.IsMatch(request.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự",
                        Status = 400,
                        Data = null
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

                if (!Regex.IsMatch(changePasswordRequest.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự",
                        Status = 400,
                        Data = null
                    };
                }

                if (string.IsNullOrEmpty(changePasswordRequest.ConfirmPassword?.Trim()))
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

                // 🎯 Check if user has existing password
                bool hasExistingPassword = !string.IsNullOrEmpty(user.Password);

                if (hasExistingPassword)
                {
                    // Case 1: User has existing password - require old password verification
                    if (string.IsNullOrEmpty(changePasswordRequest.OldPassword?.Trim()))
                    {
                        return new ApiResponse<object>
                        {
                            Data = null,
                            Message = "Mật khẩu cũ không được để trống",
                            Success = false,
                            Status = 400
                        };
                    }

                    // Verify old password
                    bool isValidOldPassword = BCrypt.Net.BCrypt.Verify(changePasswordRequest.OldPassword, user.Password);
                    if (!isValidOldPassword)
                    {
                        return new ApiResponse<object>
                        {
                            Data = null,
                            Message = MessagesCodes.MSG_15, // "Mật khẩu cũ không đúng"
                            Success = false,
                            Status = 400
                        };
                    }
                }
                // Case 2: First-time password setup - no old password required

                // Hash new password before saving
                user.Password = BCrypt.Net.BCrypt.HashPassword(changePasswordRequest.NewPassword);

                var result = await _userRepository.UpdateUserAsync(user);
                if (!result)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = "Cập nhật mật khẩu thất bại",
                        Success = false,
                        Status = 500
                    };
                }

                // Return appropriate success message
                string successMessage = hasExistingPassword
                    ? "Đổi mật khẩu thành công"
                    : "Thiết lập mật khẩu thành công";

                return new ApiResponse<object>
                {
                    Data = new
                    {
                        IsFirstTimeSetup = !hasExistingPassword,
                        Message = successMessage
                    },
                    Message = successMessage,
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
        public virtual async Task<ApiResponse<object>> SendPasswordResetOtpBySMSAsync(ForgotPasswordRequestBySmsDto? request)
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

                if (!Regex.IsMatch(request.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự",
                        Status = 400,
                        Data = null
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
                    Message = MessagesCodes.MSG_87,
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
                // Validate basic request
                var basicValidationResult = ValidateBasicRequest(userId, updateUserDto);
                if (!basicValidationResult.Success)
                {
                    return basicValidationResult;
                }

                // Validate user data
                var userValidationResult = await ValidateUserDataAsync(userId, updateUserDto);
                if (!userValidationResult.Success)
                {
                    return userValidationResult;
                }

                // Get existing user
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

                // Update user properties
                UpdateUserProperties(user, updateUserDto);

                // Handle bank account update if all required fields are provided
                var bankAccountResult = await HandleBankAccountUpdateAsync(userId, updateUserDto);
                if (!bankAccountResult.Success)
                {
                    return bankAccountResult;
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

        private ApiResponse<object> ValidateBasicRequest(int userId, UpdateUserRequest updateUserDto)
        {
            if (updateUserDto == null)
            {
                return CreateErrorResponse("Dữ liệu không hợp lệ", 400);
            }

            if (userId <= 0)
            {
                return CreateErrorResponse("UserId không hợp lệ", 400);
            }

            return CreateSuccessResponse();
        }

        private async Task<ApiResponse<object>> ValidateUserDataAsync(int userId, UpdateUserRequest updateUserDto)
        {
            // Validate FullName
            if (string.IsNullOrEmpty(updateUserDto.FullName?.Trim()))
            {
                return CreateErrorResponse("Tên người dùng không được để trống", 400);
            }

            if (updateUserDto.FullName.Length > 50)
            {
                return CreateErrorResponse("Tên người dùng không được vượt quá 50 ký tự", 400);
            }

            // Validate Email
            if (string.IsNullOrEmpty(updateUserDto.Email))
            {
                return CreateErrorResponse("Email không được để trống", 400);
            }

            if (!await IsRealEmailAsync(updateUserDto.Email))
            {
                return CreateErrorResponse("Địa chỉ Email không hợp lệ", 400);
            }

            // Check email exists
            var existingEmail = await _userRepository.CheckEmailExistedByUserId(userId, updateUserDto.Email);
            if (existingEmail != null)
            {
                return CreateErrorResponse("Email đã được sử dụng", 400);
            }

            // Validate Address
            if (string.IsNullOrEmpty(updateUserDto.Address?.Trim()))
            {
                return CreateErrorResponse("Địa chỉ không được để trống", 400);
            }

            if (updateUserDto.Address.Length > 255)
            {
                return CreateErrorResponse("Địa chỉ không được vượt quá 255 ký tự", 400);
            }

            // Validate Date of Birth
            if (!updateUserDto.Dob.HasValue)
            {
                return CreateErrorResponse("Ngày sinh không được để trống", 400);
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (updateUserDto.Dob.Value > today)
            {
                return CreateErrorResponse("Ngày sinh không được là ngày tương lai", 400);
            }

            var age = today.Year - updateUserDto.Dob.Value.Year;
            if (updateUserDto.Dob.Value > today.AddYears(-age)) age--;

            if (age < 15)
            {
                return CreateErrorResponse("Người dùng phải từ 15 tuổi trở lên", 400);
            }

            return CreateSuccessResponse();
        }

        private void UpdateUserProperties(User user, UpdateUserRequest updateUserDto)
        {
            user.FullName = updateUserDto.FullName.Trim();
            user.Email = updateUserDto.Email;
            user.IsMale = updateUserDto.IsMale;
            user.Address = updateUserDto.Address.Trim();
            user.Dob = updateUserDto.Dob;
        }

        private async Task<ApiResponse<object>> HandleBankAccountUpdateAsync(int userId, UpdateUserRequest updateUserDto)
        {
            // Check if all bank account fields are provided
            var accountNumber = updateUserDto.AccountNumber?.Trim();
            var accountHolder = updateUserDto.AccountHolder?.Trim();
            var bankTypeId = updateUserDto.BankTypeId;

            // If any of the required bank fields is empty, skip bank account update
            if (string.IsNullOrEmpty(accountNumber) ||
                string.IsNullOrEmpty(accountHolder) ||
                bankTypeId == null || bankTypeId <= 0)
            {
                return CreateSuccessResponse(); // Skip bank account update
            }

            // Validate bank account data
            var bankValidationResult = await ValidateBankAccountDataAsync(accountNumber, accountHolder, bankTypeId.Value);
            if (!bankValidationResult.Success)
            {
                return bankValidationResult;
            }

            // Update or create bank account
            var bankAccount = await _bankAccountRepository.GetBankAccountsByUserIdAsync(userId);
            if (bankAccount == null)
            {
                // Create new bank account
                bankAccount = new BankAccount
                {
                    UserId = userId,
                    AccountNumber = accountNumber,
                    AccountHolder = accountHolder,
                    BankTypeId = bankTypeId.Value
                };
                await _bankAccountRepository.AddBankAccountAsync(bankAccount);
            }
            else
            {
                // Update existing bank account
                bankAccount.AccountNumber = accountNumber;
                bankAccount.AccountHolder = accountHolder;
                bankAccount.BankTypeId = bankTypeId.Value;
                await _bankAccountRepository.UpdateBankAccountAsync(bankAccount);
            }

            return CreateSuccessResponse();
        }

        private async Task<ApiResponse<object>> ValidateBankAccountDataAsync(string accountNumber, string accountHolder, int bankTypeId)
        {
            // Validate account number
            if (!IsValidBankAccount(accountNumber))
            {
                return CreateErrorResponse("Số tài khoản không hợp lệ, chỉ chứa từ 9-16 ký tự", 400);
            }

            // Validate account holder
            if (accountHolder.Length > 50)
            {
                return CreateErrorResponse("Tên chủ tài khoản không được vượt quá 50 ký tự", 400);
            }

            // Validate bank type
            var bankType = await _bankAccountRepository.GetBankTypeByIdAsync(bankTypeId);
            if (bankType == null)
            {
                return CreateErrorResponse("Không tìm thấy kiểu ngân hàng đã chọn", 400);
            }

            return CreateSuccessResponse();
        }

        private ApiResponse<object> CreateErrorResponse(string message, int status)
        {
            return new ApiResponse<object>
            {
                Data = null,
                Message = message,
                Success = false,
                Status = status
            };
        }

        private ApiResponse<object> CreateSuccessResponse()
        {
            return new ApiResponse<object>
            {
                Data = null,
                Message = "Success",
                Success = true,
                Status = 200
            };
        }
        public async Task<ApiResponse<object>> CheckPasswordStatusAsync(int userId)
        {
            try
            {
                // Validate userId
                if (userId <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_90,
                        Success = false,
                        Status = 400
                    };
                }

                // Get user by id
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_65, // "Người dùng không tồn tại"
                        Success = false,
                        Status = 404
                    };
                }

                // Check if user is active
                if (user.StatusId != 1)
                {
                    return new ApiResponse<object>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_09, // "Tài khoản đã bị khóa"
                        Success = false,
                        Status = 400
                    };
                }

                // Check if user has password
                bool hasPassword = !string.IsNullOrEmpty(user.Password);

                return new ApiResponse<object>
                {
                    Data = new
                    {
                        UserId = userId,
                        HasPassword = hasPassword,
                        RequireOldPassword = hasPassword,
                        PasswordStatus = hasPassword ? "Đã thiết lập mật khẩu" : "Chưa thiết lập mật khẩu",
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone
                    },
                    Message = hasPassword ? MessagesCodes.MSG_89 : MessagesCodes.MSG_88,
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
        protected virtual async Task<bool> IsRealEmailAsync(string email)
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
        protected virtual bool IsValidBankAccount(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return false;

            if (!Regex.IsMatch(accountNumber, @"^[a-zA-Z0-9]+$"))
                return false;

            if (accountNumber.Length < 9 || accountNumber.Length > 16)
                return false;

            return true;
        }
        protected virtual string GenerateSecureOtp()
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
                return (randomNumber % 900000 + 100000).ToString(); // 6 digits OTP
            }
        }
        protected virtual bool IsValidPhoneNumber(string phone)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[3-9]\d{8}$");
        }

    }
}
