
using B2P_API.Interface;
using B2P_API.DTOs.AuthDTOs;
using B2P_API.Response;
using B2P_API.Models;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
namespace B2P_API.Services
{
    public class AuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IEmailService _emailService;
        private readonly ISMSService _smsService;
        private readonly JWTHelper _jwtHelper;
        private IMemoryCache _cache;

        public AuthService(
            IAuthRepository authRepository,
            JWTHelper jwtHelper,
            IMemoryCache cache,
            IEmailService emailService,   
            ISMSService smsService         
        )
        {
            _authRepository = authRepository;
            _jwtHelper = jwtHelper;
            _cache = cache;
            _emailService = emailService;   
            _smsService = smsService;       
        }

        public async Task<ApiResponse<OtpResponseDto>> SendOtpAsync(SendOtpRequestDto request)
        {
            try
            {
                var contact = request.PhoneOrEmail.Trim();

                // Validate input
                if (string.IsNullOrEmpty(contact))
                {
                    return new ApiResponse<OtpResponseDto>
                    {
                        Success = false,
                        Message = "Phone number or email is required",
                        Status = 400,
                        Data = null
                    };
                }

                // Check format
                bool isEmail = IsValidEmail(contact);
                bool isPhone = IsValidPhone(contact);

                if (!isEmail && !isPhone)
                {
                    return new ApiResponse<OtpResponseDto>
                    {
                        Success = false,
                        Message = "Invalid phone number or email format",
                        Status = 400,
                        Data = null
                    };
                }

                // Check rate limiting (1 phút 1 lần)
                var rateLimitKey = $"otp_rate_limit_{contact}";
                if (_cache.TryGetValue(rateLimitKey, out _))
                {
                    return new ApiResponse<OtpResponseDto>
                    {
                        Success = false,
                        Message = "Please wait before requesting another OTP",
                        Status = 429,
                        Data = null
                    };
                }

                // Generate OTP và session token
                var otp = GenerateOtp();
                var sessionToken = GenerateSessionToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(5);

                // Lưu OTP vào cache
                var otpData = new
                {
                    Contact = contact,
                    Code = otp,
                    SessionToken = sessionToken,
                    IsEmail = isEmail,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                _cache.Set($"otp_{contact}_{sessionToken}", otpData, TimeSpan.FromMinutes(5));
                _cache.Set(rateLimitKey, true, TimeSpan.FromMinutes(1));

                
                if (isEmail)
                {
                    await _emailService.SendOtpEmailForLoginAsync(contact, otp);
                }
                else
                {
                    var smsResult = await _smsService.SendOTPAsync(contact, otp);
                    if (!smsResult.Success)
                    {
                        // Xóa cache nếu SMS thất bại
                        _cache.Remove($"otp_{contact}_{sessionToken}");
                        _cache.Remove(rateLimitKey);

                        return new ApiResponse<OtpResponseDto>
                        {
                            Success = false,
                            Message = $"Failed to send SMS: {smsResult.Message}",
                            Status = 500,
                            Data = null
                        };
                    }
                }

                return new ApiResponse<OtpResponseDto>
                {
                    Success = true,
                    Message = "OTP sent successfully",
                    Status = 200,
                    Data = new OtpResponseDto
                    {
                        SessionToken = sessionToken,
                        Message = $"OTP has been sent to {MaskContact(contact, isEmail)}",
                        MaskedContact = MaskContact(contact, isEmail),
                        ExpiresAt = expiresAt
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<OtpResponseDto>
                {
                    Success = false,
                    Message = $"Failed to send OTP: {ex.Message}",
                    Status = 500,
                    Data = null
                };
            }
        }
        public async Task<ApiResponse<TokenResponseDto>> VerifyOtpAndLoginAsync(VerifyOtpRequestDto request)
        {
            try
            {
                var contact = request.PhoneOrEmail.Trim();

                // Bước 1: Lấy OTP từ cache
                var cacheKey = $"otp_{contact}_{request.SessionToken}";
                if (!_cache.TryGetValue(cacheKey, out dynamic? otpData))
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "OTP expired or invalid session",
                        Status = 400,
                        Data = null
                    };
                }

                // Bước 2: Kiểm tra OTP có đúng không
                if (otpData.Code != request.Otp)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Invalid OTP code",
                        Status = 400,
                        Data = null
                    };
                }

                // Bước 3: Xóa OTP khỏi cache (đã sử dụng)
                _cache.Remove(cacheKey);
                _cache.Remove($"otp_rate_limit_{contact}");

                // Bước 4: Tìm user trong database
                User? user = null;
                bool isNewUser = false;
                bool isEmail = otpData.IsEmail;

                if (isEmail)
                {
                    user = await _authRepository.GetUserByEmailAsync(contact);
                }
                else
                {
                    user = await _authRepository.GetUserByPhoneAsync(contact);
                }

                // Bước 5: Tạo user mới nếu chưa có (Auto Register)
                if (user == null)
                {
                    isNewUser = true;
                    user = new User
                    {
                        Phone = isEmail ? "" : contact,
                        Email = isEmail ? contact : $"{contact}@b2p.temp",
                        FullName = isEmail
                            ? $"User {contact.Split('@')[0]}"
                            : $"User {contact.Substring(contact.Length - 4)}",
                        StatusId = 1, // Active
                        RoleId = 1,   // User role  
                        CreateAt = DateTime.UtcNow
                    };

                    await _authRepository.CreateUserAsync(user);

                    // Lấy lại user với đầy đủ thông tin
                    if (isEmail)
                    {
                        user = await _authRepository.GetUserByEmailAsync(contact);
                    }
                    else
                    {
                        user = await _authRepository.GetUserByPhoneAsync(contact);
                    }
                }

                if (user == null)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Failed to create or retrieve user",
                        Status = 500,
                        Data = null
                    };
                }

                // Bước 6: Generate JWT tokens
                var tokens = _jwtHelper.GenerateTokens(user);

                // Bước 7: Lưu tokens vào database
                var userToken = new UserToken
                {
                    UserId = user.UserId,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                };

                await _authRepository.SaveUserTokenAsync(userToken);

                // Bước 8: Tạo response
                var response = new TokenResponseDto
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    TokenType = tokens.TokenType,
                    IsNewUser = isNewUser,
                    User = new UserInfoDto
                    {
                        UserId = user.UserId,
                        Phone = user.Phone ?? "",
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
                        Address = user.Address,
                        RoleName = user.Role?.RoleName ?? "User",
                        CreateAt = user.CreateAt
                    }
                };

                return new ApiResponse<TokenResponseDto>
                {
                    Success = true,
                    Message = isNewUser ? "Account created and login successful" : "Login successful",
                    Status = 200,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokenResponseDto>
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}",
                    Status = 500,
                    Data = null
                };
            }
        }
        public async Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            try
            {
                // Bước 1: Validate input
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Refresh token is required",
                        Status = 400,
                        Data = null
                    };
                }

                // Bước 2: Verify refresh token format và decode
                var principal = _jwtHelper.ValidateRefreshToken(request.RefreshToken);
                if (principal == null)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Invalid refresh token",
                        Status = 401,
                        Data = null
                    };
                }

                // Bước 3: Lấy UserId từ token
                var userIdClaim = principal.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Invalid token claims",
                        Status = 401,
                        Data = null
                    };
                }

                // Bước 4: Kiểm tra refresh token có tồn tại trong database không
                var existingToken = await _authRepository.GetUserTokenByRefreshTokenAsync(request.RefreshToken);
                if (existingToken == null)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Refresh token not found or revoked",
                        Status = 401,
                        Data = null
                    };
                }

                // Bước 5: Kiểm tra user có tồn tại và active không
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null || user.StatusId != 1)
                {
                    // Xóa token cũ nếu user không tồn tại hoặc bị deactivate
                    await _authRepository.DeleteUserTokenAsync(existingToken.UserTokenId); // ✅ Dùng UserTokenId

                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "User not found or account deactivated",
                        Status = 401,
                        Data = null
                    };
                }

                // Bước 6: Generate tokens mới
                var newTokens = _jwtHelper.GenerateTokens(user);

                // Bước 7: Update tokens trong database
                existingToken.AccessToken = newTokens.AccessToken;
                existingToken.RefreshToken = newTokens.RefreshToken;
                // ✅ Bỏ UpdatedAt vì không có field này

                await _authRepository.UpdateUserTokenAsync(existingToken);

                // Bước 8: Tạo response (giữ nguyên như cũ)
                var response = new TokenResponseDto
                {
                    AccessToken = newTokens.AccessToken,
                    RefreshToken = newTokens.RefreshToken,
                    ExpiresAt = newTokens.ExpiresAt,
                    TokenType = newTokens.TokenType,
                    IsNewUser = false,
                    User = new UserInfoDto
                    {
                        UserId = user.UserId,
                        Phone = user.Phone ?? "",
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
                        Address = user.Address,
                        RoleName = user.Role?.RoleName ?? "User",
                        CreateAt = user.CreateAt
                    }
                };

                return new ApiResponse<TokenResponseDto>
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Status = 200,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokenResponseDto>
                {
                    Success = false,
                    Message = $"Token refresh failed: {ex.Message}",
                    Status = 500,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<object>> LogoutAsync(LogoutRequestDto request)
        {
            try
            {
                // Bước 1: Validate input
                if (string.IsNullOrEmpty(request.AccessToken))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Access token is required",
                        Status = 400,
                        Data = null
                    };
                }

                // Bước 2: Validate access token và lấy thông tin user
                var principal = _jwtHelper.ValidateAccessToken(request.AccessToken);
                if (principal == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid access token",
                        Status = 401,
                        Data = null
                    };
                }

                // Bước 3: Lấy UserId từ token
                var userIdClaim = principal.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid token claims",
                        Status = 401,
                        Data = null
                    };
                }

                // Bước 4: Tìm user token trong database
                var userToken = await _authRepository.GetUserTokenByAccessTokenAsync(request.AccessToken);
                if (userToken == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Token not found or already revoked",
                        Status = 401,
                        Data = null
                    };
                }

                // Bước 5: Xử lý logout theo loại
                bool logoutSuccess = false;

                if (request.LogoutType == "single")
                {
                    // ✅ Logout chỉ device hiện tại
                    logoutSuccess = await _authRepository.DeleteUserTokenAsync(userToken.UserTokenId);
                }
                else if (request.LogoutType == "all")
                {
                    // ✅ Logout tất cả devices
                    await _authRepository.RevokeAllUserTokensAsync(userId);
                    logoutSuccess = true;
                }
                else
                {
                    // Default: logout single device
                    logoutSuccess = await _authRepository.DeleteUserTokenAsync(userToken.UserTokenId);
                }

                if (!logoutSuccess)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Logout failed",
                        Status = 500,
                        Data = null
                    };
                }

                // Bước 6: Clear cache liên quan (nếu có)
                try
                {
                    var user = userToken.User;
                    if (user != null)
                    {
                        // Clear OTP cache nếu có
                        var email = user.Email;
                        var phone = user.Phone;

                        if (!string.IsNullOrEmpty(email))
                        {
                            var cacheKeys = new[]
                            {
                        $"otp_{email}_*",
                        $"otp_rate_limit_{email}"
                    };

                            foreach (var pattern in cacheKeys)
                            {
                                // Xóa cache pattern (implementation tùy thuộc vào cache provider)
                                // _cache.Remove(pattern); // Simplified
                            }
                        }

                        if (!string.IsNullOrEmpty(phone))
                        {
                            var cacheKeys = new[]
                            {
                        $"otp_{phone}_*",
                        $"otp_rate_limit_{phone}"
                    };

                            foreach (var pattern in cacheKeys)
                            {
                                // _cache.Remove(pattern); // Simplified
                            }
                        }
                    }
                }
                catch
                {
                    // Cache clear không quan trọng, không fail logout
                }

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = request.LogoutType == "all"
                        ? "Logged out from all devices successfully"
                        : "Logged out successfully",
                    Status = 200,
                    Data = new
                    {
                        LoggedOutAt = DateTime.UtcNow,
                        LogoutType = request.LogoutType ?? "single",
                        UserId = userId
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Logout failed: {ex.Message}",
                    Status = 500,
                    Data = null
                };
            }
        }
        // Helper methods
        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string GenerateSessionToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailRegex.IsMatch(email);
        }

        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return false;
            var cleanPhone = Regex.Replace(phone, @"[\s\-\(\)]", "");
            var phoneRegex = new Regex(@"^(\+84|84|0)[0-9]{8,9}$");
            return phoneRegex.IsMatch(cleanPhone);
        }

        private string MaskContact(string contact, bool isEmail)
        {
            if (isEmail)
            {
                var parts = contact.Split('@');
                if (parts.Length == 2)
                {
                    var username = parts[0];
                    var domain = parts[1];
                    var maskedUsername = username.Length > 2
                        ? $"{username.Substring(0, 1)}***{username.Substring(username.Length - 1)}"
                        : "***";
                    return $"{maskedUsername}@{domain}";
                }
            }
            else
            {
                if (contact.Length > 6)
                {
                    return $"{contact.Substring(0, 3)}****{contact.Substring(contact.Length - 3)}";
                }
            }
            return contact;
        }
    }
}
