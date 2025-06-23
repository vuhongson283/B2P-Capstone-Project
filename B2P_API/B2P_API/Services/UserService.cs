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

        public UserService(
            IUserRepository userRepository,
            IEmailService emailService,
            IMemoryCache cache)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<ApiResponse<object>> SendPasswordResetOtpAsync(ForgotPasswordRequestByEmailDto request)
        {
            try
            {
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

        public async Task<ApiResponse<object>> VerifyOtpAndResetPasswordAsync(VerifyOtpDto request)
        {
            try
            {
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

                // Verify OTP code
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

                // Remove OTP from cache after successful verification
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

        public async Task<ApiResponse<object>> ResendPasswordResetOtpAsync(string email)
        {
            try
            {
                // Check rate limiting - prevent spam
                var rateLimitKey = $"rate_limit_otp_{email}";
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

                var request = new ForgotPasswordRequestByEmailDto { Email = email };
                return await SendPasswordResetOtpAsync(request);
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
                    Username = user.Username,
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
        public async Task<ApiResponse<object>> UpdateUserAsync(int userId, [FromBody] UpdateUserRequest updateUserDto)
        {
            try
            {
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
                var existingPhone = await _userRepository.CheckPhoneExistedByUserId(userId, updateUserDto.Phone);
                if (existingPhone != null)
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
                user.Username = updateUserDto.Username;
                user.Email = updateUserDto.Email;
                user.Phone = updateUserDto.Phone;
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

    }
}
