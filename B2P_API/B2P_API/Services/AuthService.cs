
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

                // Lấy OTP từ cache
                var cacheKey = $"otp_{contact}_{request.SessionToken}";
                if (!_cache.TryGetValue(cacheKey, out dynamic? otpData))
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "OTP hết hạn hoặc session không hợp lệ",
                        Status = 400,
                        Data = null
                    };
                }

                // Kiểm tra OTP
                if (otpData.Code != request.Otp)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Mã OTP không đúng",
                        Status = 400,
                        Data = null
                    };
                }

                // Xóa OTP khỏi cache
                _cache.Remove(cacheKey);
                _cache.Remove($"otp_rate_limit_{contact}");

                // ✅ USER ĐÃ TỒN TẠI (được tạo trong GoogleLogin hoặc SendOtp)
                bool isGoogleLogin = otpData.IsGoogleLogin == true;
                bool isNewUser = otpData.IsNewUser == true;

                Console.WriteLine($"🔍 Verifying OTP: {contact} (Google: {isGoogleLogin}, New: {isNewUser})");

                // ✅ LẤY USER (CHẮC CHẮN TỒN TẠI)
                User user = null;
                if (otpData.UserId != null)
                {
                    // Case 1: Có UserId từ cache (Google login)
                    user = await _authRepository.GetUserByIdAsync((int)otpData.UserId);
                }
                else
                {
                    // Case 2: Tìm theo email/phone (regular login)
                    if (otpData.IsEmail == true)
                    {
                        user = await _authRepository.GetUserByEmailAsync(contact);
                    }
                    else
                    {
                        user = await _authRepository.GetUserByPhoneAsync(contact);
                    }

                    // ✅ TẠO USER CHO REGULAR LOGIN NỀU CHƯA CÓ
                    if (user == null)
                    {
                        isNewUser = true;
                        user = new User
                        {
                            Phone = otpData.IsEmail == true ? "" : contact,
                            Email = otpData.IsEmail == true ? contact : $"{contact}@b2p.temp",
                            FullName = otpData.IsEmail == true
                                ? $"User {contact.Split('@')[0]}"
                                : $"User {contact.Substring(contact.Length - 4)}",
                            StatusId = 1,
                            RoleId = 1,
                            CreateAt = DateTime.UtcNow
                        };

                        await _authRepository.CreateUserAsync(user);
                        user = await _authRepository.GetUserByEmailAsync(contact);

                        Console.WriteLine($"🆕 Created regular user: {contact}");
                    }
                }

                if (user == null)
                {
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Không thể tìm thấy hoặc tạo user",
                        Status = 500,
                        Data = null
                    };
                }

                // ✅ GENERATE JWT TOKENS
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
                var response = new TokenResponseDto
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    TokenType = tokens.TokenType,
                    IsNewUser = isNewUser,
                    // Trong response UserInfoDto:
                    User = new UserInfoDto
                    {
                        UserId = user.UserId,

                        // ✅ Phone empty cho Google users - OK!
                        Phone = user.Phone ?? "",

                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
                        Address = user.Address?.StartsWith("https://") == true ? "" : user.Address,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.RoleName ?? "User",
                        CreateAt = user.CreateAt,

                        // ✅ THÊM INFO CHO GOOGLE USER
                        AvatarUrl = user.Address?.StartsWith("https://") == true ? user.Address : null,
                        IsGoogleUser = string.IsNullOrEmpty(user.Phone), // Simple detection
                        GoogleId = null // Không cần store GoogleId nữa
                    }
                };

                string successMessage = isGoogleLogin
                    ? (isNewUser ? "Tài khoản Google đã được tạo và đăng nhập thành công" : "Đăng nhập Google thành công")
                    : (isNewUser ? "Tài khoản đã được tạo và đăng nhập thành công" : "Đăng nhập thành công");

                return new ApiResponse<TokenResponseDto>
                {
                    Success = true,
                    Message = successMessage,
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

                var contact = payload.Email.Trim();

                // KIỂM TRA USER ĐÃ TỒN TẠI
                var existingUser = await _authRepository.GetUserByEmailAsync(payload.Email);

                // CASE 1: USER ĐÃ TỒN TẠI → TẠO JWT LUÔN (KHÔNG CẦN OTP)
                if (existingUser != null)
                {
                    Console.WriteLine($"👤 Existing Google user: {contact} with ID: {existingUser.UserId} - LOGIN DIRECTLY");

                    // TẠO JWT TOKENS NGAY
                    var tokens = _jwtHelper.GenerateTokens(existingUser);

                    // Save tokens
                    var userToken = new UserToken
                    {
                        UserId = existingUser.UserId,
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken
                    };

                    await _authRepository.SaveUserTokenAsync(userToken);

                    // TẠO RESPONSE ĐẦY ĐỦ
                    var loginResponse = new TokenResponseDto
                    {
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken,
                        ExpiresAt = tokens.ExpiresAt,
                        TokenType = tokens.TokenType,
                        IsNewUser = false,
                        User = new UserInfoDto
                        {
                            UserId = existingUser.UserId,
                            Phone = existingUser.Phone ?? "",
                            FullName = existingUser.FullName ?? "",
                            Email = existingUser.Email ?? "",
                            IsMale = existingUser.IsMale,
                            Dob = existingUser.Dob,
                            Address = existingUser.Address?.StartsWith("https://") == true ? "" : existingUser.Address,
                            RoleId = existingUser.RoleId,
                            RoleName = existingUser.Role?.RoleName ?? "User",
                            CreateAt = existingUser.CreateAt,
                            AvatarUrl = existingUser.Address?.StartsWith("https://") == true ? existingUser.Address : null,
                            IsGoogleUser = true,
                            GoogleId = payload.Subject
                        }
                    };

                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Đăng nhập Google thành công",
                        Status = 200,
                        Data = loginResponse
                    };
                }

                // CASE 2: USER CHƯA TỒN TẠI → TẠO USER MỚI VÀ GỬI OTP

                // Check rate limiting cho user mới
                var rateLimitKey = $"otp_rate_limit_{contact}";
                if (_cache.TryGetValue(rateLimitKey, out _))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Vui lòng đợi trước khi yêu cầu OTP khác",
                        Status = 429,
                        Data = null
                    };
                }

                // TẠO USER MỚI NGAY
                var newUser = new User
                {
                    Email = contact,
                    FullName = payload.Name ?? $"User {contact.Split('@')[0]}",
                    Phone = "", // Google users không có phone
                    IsMale = null,
                    RoleId = 1, // Default role
                    CreateAt = DateTime.UtcNow,
                    Password = null, // Google users không có password
                    Address = payload.Picture // Store Google avatar URL
                };

                Console.WriteLine($"🆕 Creating new Google user: {contact}");

                await _authRepository.CreateUserAsync(newUser);
                var createdUser = await _authRepository.GetUserByEmailAsync(contact);

                Console.WriteLine($"✅ Created Google user: {contact} with ID: {createdUser?.UserId}");

                // GENERATE OTP CHO USER MỚI
                var otp = GenerateOtp();
                var sessionToken = GenerateSessionToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(5);

                // Lưu OTP data vào cache
                var otpData = new
                {
                    Contact = contact,
                    Code = otp,
                    SessionToken = sessionToken,
                    IsEmail = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsGoogleLogin = true,
                    GoogleSubject = payload.Subject,
                    GoogleName = payload.Name,
                    GooglePicture = payload.Picture,
                    IsNewUser = true,
                    UserId = createdUser?.UserId
                };

                _cache.Set($"otp_{contact}_{sessionToken}", otpData, TimeSpan.FromMinutes(5));
                _cache.Set(rateLimitKey, true, TimeSpan.FromMinutes(1));

                // GỬI OTP EMAIL
                try
                {
                    await _emailService.SendOtpEmailForLoginAsync(contact, otp);
                }
                catch (Exception emailEx)
                {
                    // Cleanup nếu gửi email thất bại
                    _cache.Remove($"otp_{contact}_{sessionToken}");
                    _cache.Remove(rateLimitKey);

                    Console.WriteLine($"❌ Gửi email thất bại: {emailEx.Message}");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể gửi email xác thực",
                        Status = 500,
                        Data = null
                    };
                }

                Console.WriteLine($"📧 Đã gửi OTP cho user Google mới: {contact}");

                // TRẢ VỀ OTP RESPONSE CHO USER MỚI
                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "OTP đã được gửi đến email Google của bạn",
                    Status = 200,
                    Data = new OtpResponseDto
                    {
                        SessionToken = sessionToken,
                        Message = $"Mã OTP đã được gửi đến {MaskContact(contact, true)} để xác thực tài khoản Google mới",
                        MaskedContact = MaskContact(contact, true),
                        ExpiresAt = expiresAt
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Google login: {ex.Message}");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Xác thực Google thất bại: {ex.Message}",
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

                //  FIND USER BY EMAIL OR PHONE
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

                //  CHECK IF USER HAS PASSWORD (not Google-only user)
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

                //  VERIFY PASSWORD
                var isPasswordValid = await _authRepository.VerifyUserPasswordAsync(user.UserId, request.Password);

                if (!isPasswordValid)
                {
                    Console.WriteLine($"❌ Invalid password for user: {request.PhoneOrEmail}");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email/số điện thoại hoặc mật khẩu không đúng",
                        Status = 401,
                        Data = null
                    };
                }

                Console.WriteLine($"Login successful for user: {request.PhoneOrEmail} (ID: {user.UserId})");

                //  GENERATE JWT TOKENS
                var tokens = _jwtHelper.GenerateTokens(user);

                //  SAVE TOKENS
                var userToken = new UserToken
                {
                    UserId = user.UserId,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                };

                await _authRepository.SaveUserTokenAsync(userToken);

                //  CREATE RESPONSE
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
                        Phone = user.Phone ?? "",
                        FullName = user.FullName ?? "",
                        Email = user.Email ?? "",
                        IsMale = user.IsMale,
                        Dob = user.Dob,
                        Address = user.Address?.StartsWith("https://") == true ? "" : user.Address,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.RoleName ?? "User",
                        CreateAt = user.CreateAt,
                        AvatarUrl = user.Address?.StartsWith("https://") == true ? user.Address : null,
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
