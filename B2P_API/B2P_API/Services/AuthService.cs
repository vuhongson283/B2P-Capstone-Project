using B2P_API.Interface;
using B2P_API.DTOs.AuthDTOs;
using B2P_API.Response;
using B2P_API.Models;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using Google.Apis.Auth;
namespace B2P_API.Services
{
    public class AuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IEmailService _emailService;
        private readonly ISMSService _smsService;
        private readonly JWTHelper _jwtHelper;
        private IMemoryCache _cache;

        public AuthService(
            IImageRepository imageRepository,
            IAuthRepository authRepository,
            JWTHelper jwtHelper,
            IMemoryCache cache,
            IEmailService emailService,
            ISMSService smsService
        )
        {
            _imageRepository = imageRepository;
            _authRepository = authRepository;
            _jwtHelper = jwtHelper;
            _cache = cache;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task<ApiResponse<OtpResponseDto>> SendOtpAsync(
            SendOtpRequestDto request,
            bool isNewUser = false,
            int? existingUserId = null,
            bool isGoogleLogin = false,
            string googleName = null,
            string googleSubject = null)
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

                // ✅ Lưu OTP vào cache với thông tin đầy đủ cho VerifyOtp
                var otpData = new
                {
                    Contact = contact,
                    Code = otp,
                    SessionToken = sessionToken,
                    IsEmail = isEmail,
                    IsNewUser = isNewUser,                    // ✅ Thêm flag để VerifyOtp biết tạo user mới
                    UserId = existingUserId,                  // ✅ UserId của user đã tồn tại (nếu có)
                    IsGoogleLogin = isGoogleLogin,            // ✅ Flag Google login
                    GoogleName = googleName,                  // ✅ Tên từ Google (nếu có)
                    GoogleSubject = googleSubject,            // ✅ Google Subject ID (nếu có)
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                // ✅ Fix syntax error: _cache thay vì *cache
                _cache.Set($"otp_{contact}_{sessionToken}", otpData, TimeSpan.FromMinutes(5));
                _cache.Set(rateLimitKey, true, TimeSpan.FromMinutes(1));

                Console.WriteLine($"📤 OTP created: {contact} (NewUser: {isNewUser}, ExistingUserId: {existingUserId}, Google: {isGoogleLogin})");

                if (isEmail)
                {
                    await _emailService.SendOtpEmailForLoginAsync(contact, otp);
                }
                else
                {
                    var smsResult = await _smsService.SendOTPAsync(contact, otp);
                    if (!smsResult.Success)
                    {
                        // ✅ Fix syntax error: _cache thay vì *cache
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
                Console.WriteLine($"❌ SendOTP error: {ex.Message}");
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

                // Lấy OTP từ cache
                var cacheKey = $"otp_{contact}_{request.SessionToken}";
                if (!_cache.TryGetValue(cacheKey, out dynamic? otpData))
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "OTP hết hạn hoặc session không hợp lệ",
                        Status = 400
                    };
                }

                // Kiểm tra OTP
                if (otpData.Code != request.Otp)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Mã OTP không đúng",
                        Status = 400
                    };
                }

                // Xóa OTP khỏi cache sau khi verify thành công
                _cache.Remove(cacheKey);
                _cache.Remove($"otp_rate_limit_{contact}");

                // Thông tin user
                bool isEmail = (bool)(GetSafeProperty(otpData, "IsEmail") ?? false);
                bool isNewUser = (bool)(GetSafeProperty(otpData, "IsNewUser") ?? false);

                User user = null;

                if (isNewUser)
                {
                    // Tạo user mới
                    user = new User
                    {
                        Phone = isEmail ? null : contact,
                        Email = isEmail ? contact : $"{contact}@b2p.temp",
                        FullName = isEmail
                            ? $"User {contact.Split('@')[0]}"
                            : $"User {contact.Substring(contact.Length - 4)}",
                        StatusId = 1,
                        RoleId = 2,
                        CreateAt = DateTime.UtcNow,
                        Address = "",
                        Password = null
                    };

                    user = await _authRepository.CreateUserAsync(user);
                }
                else
                {
                    // Lấy user cũ
                    user = await _authRepository.GetUserByEmailOrPhoneAsync(contact);
                }

                if (user == null)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Không thể tìm thấy hoặc tạo user",
                        Status = 500
                    };
                }

                if (user.StatusId != 1)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị khóa hoặc vô hiệu hóa",
                        Status = 403
                    };
                }

                // Generate JWT
                var tokens = _jwtHelper.GenerateTokens(user);

                await _authRepository.SaveUserTokenAsync(new UserToken
                {
                    UserId = user.UserId,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                });

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
                        Phone = user.Phone,
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.RoleName ?? "User",
                        CreateAt = user.CreateAt,
                        IsGoogleUser = false
                    }
                };

                return new ApiResponse<TokenResponseDto>
                {
                    Success = true,
                    Message = isNewUser ? "Tài khoản đã được tạo và đăng nhập thành công" : "Đăng nhập thành công",
                    Status = 200,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokenResponseDto>
                {
                    Success = false,
                    Message = $"Đăng nhập thất bại: {ex.Message}",
                    Status = 500
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
                        Phone = user.Phone ?? null,
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
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
        public async Task<ApiResponse<object>> GoogleLoginAsync(GoogleLoginRequestDto request)
        {
            try
            {
                // Verify Google ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.GoogleToken,
                    new GoogleJsonWebSignature.ValidationSettings()
                    {
                        Audience = new[] { "286138081852-t1j086t42d21k5ca6kv351b85tda7qus.apps.googleusercontent.com" }
                    });

                Console.WriteLine($"✅ Google user verified: {payload.Email}");

                var email = payload.Email.Trim();
                var existingUser = await _authRepository.GetUserByEmailAsync(email);

                User user;

                if (existingUser != null)
                {
                    // User đã tồn tại - kiểm tra status
                    if (existingUser.StatusId != 1)
                    {
                        Console.WriteLine($"❌ Google user {email} has inactive status: {existingUser.StatusId}");
                        return new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Tài khoản đã bị khóa hoặc vô hiệu hóa",
                            Status = 403,
                            Data = null
                        };
                    }

                    user = existingUser;
                    Console.WriteLine($"👤 Existing Google user: {email} - LOGIN DIRECTLY");
                }
                else
                {
                    // User chưa tồn tại - tạo user mới ngay
                    user = new User
                    {
                        Email = email,
                        FullName = payload.Name ?? email,
                        RoleId = 2, // Default role (adjust as needed)
                        StatusId = 1, // Active
                        CreateAt = DateTime.UtcNow,
                        IsMale = null, // Sẽ cập nhật sau nếu cần                 
                    };

                    var createdUser = await _authRepository.CreateUserAsync(user);
                    await _imageRepository.CreateUserDefaultImageAsync(createdUser.UserId);
                    if (createdUser == null)
                    {
                        return new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Không thể tạo tài khoản Google",
                            Status = 500,
                            Data = null
                        };
                    }

                    user = createdUser;
                    Console.WriteLine($"🆕 New Google user created: {email} with ID: {user.UserId}");
                }

                // Tạo JWT tokens
                var tokens = _jwtHelper.GenerateTokens(user);

                // Save tokens
                var userToken = new UserToken
                {
                    UserId = user.UserId,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                };

                await _authRepository.SaveUserTokenAsync(userToken);

                // Tạo response
                var loginResponse = new TokenResponseDto
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    TokenType = tokens.TokenType,
                    IsNewUser = existingUser == null, // true nếu user mới được tạo
                    User = new UserInfoDto
                    {
                        UserId = user.UserId,
                        Phone = user.Phone ?? null,
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.RoleName ?? "User",
                        CreateAt = user.CreateAt,
                        IsGoogleUser = true,
                        GoogleId = payload.Subject
                    }
                };

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = existingUser == null ?
                        "Đăng nhập Google thành công - Tài khoản mới đã được tạo" :
                        "Đăng nhập Google thành công",
                    Status = 200,
                    Data = loginResponse
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Google login error: {ex.Message}");

                if (ex.Message.Contains("Invalid token") || ex.Message.Contains("JWT") || ex.Message.Contains("Google"))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Token Google không hợp lệ hoặc đã hết hạn",
                        Status = 401,
                        Data = null
                    };
                }

                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống trong quá trình đăng nhập Google",
                    Status = 500,
                    Data = null
                };
            }
        }
        public async Task<ApiResponse<object>> LoginAsync(LoginRequestDto request)
        {
            try
            {
                Console.WriteLine($"🔑 Login attempt for: {request.PhoneOrEmail}");

                // FIND USER BY EMAIL OR PHONE
                var user = await _authRepository.GetUserByEmailOrPhoneAsync(request.PhoneOrEmail.Trim());

                if (user == null)
                {
                    Console.WriteLine($"❌ User not found: {request.PhoneOrEmail}");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email/số điện thoại hoặc mật khẩu không đúng",
                        Status = 401,
                        Data = null
                    };
                }

                // ✅ KIỂM TRA STATUSID NGAY SAU KHI TÌM THẤY USER
                if (user.StatusId != 1)
                {
                    Console.WriteLine($"❌ User {request.PhoneOrEmail} has inactive status: {user.StatusId}");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị khóa hoặc vô hiệu hóa",
                        Status = 403,
                        Data = null
                    };
                }

                // CHECK IF USER HAS PASSWORD (not Google-only user)
                if (string.IsNullOrEmpty(user.Password))
                {
                    Console.WriteLine($"❌ User {request.PhoneOrEmail} is Google-only user");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tài khoản này chỉ hỗ trợ đăng nhập bằng Google",
                        Status = 400,
                        Data = null
                    };
                }

                // VERIFY PASSWORD
                var isPasswordValid = await _authRepository.VerifyUserPasswordAsync(user.UserId, request.Password);

                if (!isPasswordValid)
                {
                    Console.WriteLine($"❌ Invalid password for user: {request.PhoneOrEmail}");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email/số điện thoại hoặc mật khẩu không đúng",
                        Status = 400,
                        Data = null
                    };
                }

                Console.WriteLine($"✅ Login successful for user: {request.PhoneOrEmail} (ID: {user.UserId})");

                // GENERATE JWT TOKENS
                var tokens = _jwtHelper.GenerateTokens(user);

                // SAVE TOKENS
                var userToken = new UserToken
                {
                    UserId = user.UserId,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                };

                await _authRepository.SaveUserTokenAsync(userToken);

                // CREATE RESPONSE
                var loginResponse = new TokenResponseDto
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    TokenType = tokens.TokenType,
                    IsNewUser = false,
                    User = new UserInfoDto
                    {
                        UserId = user.UserId,
                        Phone = user.Phone ?? null,
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.RoleName ?? "User",
                        CreateAt = user.CreateAt,
                        IsGoogleUser = false
                    }
                };

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đăng nhập thành công",
                    Status = 200,
                    Data = loginResponse
                };

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login error: {ex.Message}");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống trong quá trình đăng nhập",
                    Status = 500,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<object>> CheckUserExistAsync(string phoneOrEmail)
        {
            try
            {
                Console.WriteLine($"🔍 Checking user existence for: {phoneOrEmail}");
                var contact = phoneOrEmail.Trim();
                // Validate input format
                bool isEmail = IsValidEmail(contact);
                bool isPhone = IsValidPhone(contact);
                if (!isEmail && !isPhone)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Định dạng email hoặc số điện thoại không hợp lệ",
                        Status = 400,
                        Data = null
                    };
                }
                // FIND USER BY EMAIL OR PHONE
                var user = await _authRepository.GetUserByEmailOrPhoneAsync(contact);
                // CASE 1: User không tồn tại → Gửi OTP với flag isNewUser = true
                if (user == null)
                {
                    Console.WriteLine($"❌ User not found: {contact} - SENDING OTP");
                    var otpRequest = new SendOtpRequestDto { PhoneOrEmail = contact };
                    // ✅ FIX: Truyền isNewUser = true vì user chưa tồn tại
                    var otpResult = await SendOtpAsync(otpRequest, isNewUser: true);
                    return new ApiResponse<object>
                    {
                        Success = otpResult.Success,
                        Message = otpResult.Success ? "OTP đã được gửi để tạo tài khoản mới" : otpResult.Message,
                        Status = otpResult.Status,
                        Data = otpResult.Data
                    };
                }
                // CASE 2: User bị khóa
                if (user.StatusId != 1)
                {
                    Console.WriteLine($"❌ User {contact} has inactive status: {user.StatusId}");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị khóa hoặc vô hiệu hóa",
                        Status = 403,
                        Data = new CheckUserExistResponse
                        {
                            IsExist = true,
                            HasPassword = false
                        }
                    };
                }
                // CASE 3: User tồn tại nhưng không có password (Google user) → Gửi OTP
                bool hasPassword = !string.IsNullOrEmpty(user.Password);
                if (!hasPassword)
                {
                    Console.WriteLine($"⚠️ User {contact} exists but no password - SENDING OTP");
                    var otpRequest = new SendOtpRequestDto { PhoneOrEmail = contact };
                    // ✅ FIX: Sử dụng SendOtpAsync với isNewUser = false và existingUserId
                    var otpResult = await SendOtpAsync(otpRequest, isNewUser: false, existingUserId: user.UserId);
                    return new ApiResponse<object>
                    {
                        Success = otpResult.Success,
                        Message = otpResult.Success ? "OTP đã được gửi để đăng nhập" : otpResult.Message,
                        Status = otpResult.Status,
                        Data = otpResult.Data
                    };
                }
                // CASE 4: User tồn tại và có password → Yêu cầu nhập password
                Console.WriteLine($"✅ User exists and has password: {contact}");
                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Vui lòng nhập mật khẩu để đăng nhập",
                    Status = 200,
                    Data = new CheckUserExistResponse
                    {
                        IsExist = true,
                        HasPassword = true
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Check user error: {ex.Message}");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi kiểm tra tài khoản",
                    Status = 500,
                    Data = null
                };
            }
        }
        // Helper methods
        private object GetSafeProperty(dynamic obj, string propertyName)
        {
            try
            {
                return ((dynamic)obj).GetType().GetProperty(propertyName)?.GetValue(obj);
            }
            catch
            {
                try
                {
                    // Fallback: try direct property access
                    switch (propertyName)
                    {
                        case "IsGoogleLogin":
                            return ((dynamic)obj).IsGoogleLogin;
                        case "IsNewUser":
                            return ((dynamic)obj).IsNewUser;
                        case "GoogleName":
                            return ((dynamic)obj).GoogleName;
                        case "IsEmail":
                            return ((dynamic)obj).IsEmail;
                        case "GoogleSubject":
                            return ((dynamic)obj).GoogleSubject;
                        default:
                            return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
        private string GetSafeGoogleSubject(dynamic otpData)
        {
            try
            {
                var googleSubject = GetSafeProperty(otpData, "GoogleSubject");
                return googleSubject?.ToString();
            }
            catch
            {
                return null;
            }
        }
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