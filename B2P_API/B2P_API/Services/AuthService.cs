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
                    Console.WriteLine($"❌ OTP cache miss for key: {cacheKey}");
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "OTP hết hạn hoặc session không hợp lệ",
                        Status = 400,
                        Data = null
                    };
                }

                // ✅ DEBUG: Log otpData type và properties
                Console.WriteLine($"📋 OTP Data type: {otpData?.GetType()?.FullName}");

                try
                {
                    // List all properties của otpData để debug
                    var properties = otpData.GetType().GetProperties();

                }
                catch (Exception propEx)
                {
                    Console.WriteLine($"⚠️ Could not get properties: {propEx.Message}");
                }

                // Kiểm tra OTP
                if (otpData.Code != request.Otp)
                {
                    Console.WriteLine($"❌ OTP mismatch. Expected: {otpData.Code}, Received: {request.Otp}");
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

                bool isGoogleLogin = otpData.IsGoogleLogin == true;
                bool isNewUser = otpData.IsNewUser == true;

                Console.WriteLine($"🔍 Verifying OTP: {contact} (Google: {isGoogleLogin}, New: {isNewUser})");

                // ✅ SAFE ACCESS ĐẾN UserId
                User user = null;
                int? userId = null;

                try
                {
                    // Kiểm tra nếu UserId property tồn tại
                    var userIdProperty = otpData.GetType().GetProperty("UserId");
                    if (userIdProperty != null)
                    {
                        var userIdValue = userIdProperty.GetValue(otpData);
                        if (userIdValue != null)
                        {
                            if (userIdValue is int intUserId)
                            {
                                userId = intUserId;
                            }
                            else if (int.TryParse(userIdValue.ToString(), out int parsedUserId))
                            {
                                userId = parsedUserId;
                            }
                        }
                        Console.WriteLine($"📋 UserId from otpData: {userId}");
                    }
                    else
                    {
                        Console.WriteLine($"📋 UserId property not found in otpData (normal for new users)");
                    }
                }
                catch (Exception userIdEx)
                {
                    Console.WriteLine($"⚠️ Error accessing UserId: {userIdEx.Message}");
                    // Continue without UserId
                }

                if (userId.HasValue)
                {
                    try
                    {
                        user = await _authRepository.GetUserByIdAsync(userId.Value);
                        Console.WriteLine($"👤 Found user by ID: {userId.Value}, User: {user?.UserId}");
                    }
                    catch (Exception getUserEx)
                    {
                        Console.WriteLine($"❌ Error getting user by ID {userId.Value}: {getUserEx.Message}");
                        // Continue to search by email/phone
                    }
                }

                if (user == null)
                {
                    try
                    {
                        if (otpData.IsEmail == true)
                        {
                            user = await _authRepository.GetUserByEmailAsync(contact);
                            Console.WriteLine($"📧 Searched user by email: {contact}, Found: {user != null}");
                        }
                        else
                        {
                            user = await _authRepository.GetUserByPhoneAsync(contact);
                            Console.WriteLine($"📱 Searched user by phone: {contact}, Found: {user != null}");
                        }
                    }
                    catch (Exception searchEx)
                    {
                        Console.WriteLine($"❌ Error searching user: {searchEx.Message}");
                        throw;
                    }

                    // TẠO USER NỀU CHƯA CÓ (CHO CẢ REGULAR VÀ GOOGLE)
                    if (user == null)
                    {
                        Console.WriteLine($"🆕 Creating new user for: {contact}");
                        isNewUser = true;

                        try
                        {
                            if (isGoogleLogin)
                            {
                                // TẠO GOOGLE USER SAU KHI VERIFY OTP
                                string googleName = "User";
                                try
                                {
                                    googleName = otpData.GoogleName ?? $"User {contact.Split('@')[0]}";
                                }
                                catch (Exception)
                                {
                                    googleName = $"User {contact.Split('@')[0]}";
                                }

                                user = new User
                                {
                                    Email = contact,
                                    FullName = googleName,
                                    Phone = null, // Google user không có phone
                                    IsMale = null,
                                    RoleId = 2,
                                    StatusId = 1,
                                    CreateAt = DateTime.UtcNow,
                                    Password = null, // Google user không có password
                                    Address = "",
                                    Dob = null
                                };

                                Console.WriteLine($"🔨 Creating Google user: Email={user.Email}, FullName={user.FullName}");
                                user = await _authRepository.CreateUserAsync(user);
                                Console.WriteLine($"✅ Google user created successfully: ID={user.UserId}");

                                // TẠO ẢNH MẶC ĐỊNH CHO GOOGLE USER
                                try
                                {
                                    var defaultImage = await _imageRepository.CreateUserDefaultImageAsync(user.UserId);
                                    if (defaultImage != null)
                                    {
                                        Console.WriteLine($"✅ Default image created for Google user - ImageId: {defaultImage.ImageId}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"⚠️ Default image creation returned null for user {user.UserId}");
                                    }
                                }
                                catch (Exception imageEx)
                                {
                                    Console.WriteLine($"⚠️ Could not create default image for Google user: {imageEx.Message}");
                                    // Không fail quá trình login vì ảnh không quan trọng
                                }
                            }
                            else
                            {
                                // TẠO REGULAR USER
                                user = new User
                                {
                                    Phone = otpData.IsEmail == true ? null : contact,
                                    Email = otpData.IsEmail == true ? contact : $"{contact}@b2p.temp",
                                    FullName = otpData.IsEmail == true
                                        ? $"User {contact.Split('@')[0]}"
                                        : $"User {contact.Substring(contact.Length - 4)}",
                                    StatusId = 1,
                                    RoleId = 2,
                                    CreateAt = DateTime.UtcNow,
                                    Address = "",
                                    Password = null
                                };

                                Console.WriteLine($"🔨 Creating regular user: Email={user.Email}, Phone={user.Phone}");
                                user = await _authRepository.CreateUserAsync(user);
                                Console.WriteLine($"✅ Regular user created successfully: ID={user.UserId}");
                            }
                        }
                        catch (Exception createEx)
                        {
                            Console.WriteLine($"❌ Failed to create user: {createEx.Message}");
                            Console.WriteLine($"❌ Create user stack trace: {createEx.StackTrace}");
                            throw new Exception($"Không thể tạo tài khoản: {createEx.Message}");
                        }

                        // Lấy lại user để đảm bảo có đầy đủ thông tin
                        try
                        {
                            if (otpData.IsEmail == true)
                            {
                                user = await _authRepository.GetUserByEmailAsync(contact);
                            }
                            else
                            {
                                user = await _authRepository.GetUserByPhoneAsync(contact);
                            }
                            Console.WriteLine($"🔄 Re-fetched user: {user?.UserId}");
                        }
                        catch (Exception refetchEx)
                        {
                            Console.WriteLine($"⚠️ Warning: Could not re-fetch user: {refetchEx.Message}");
                            // Continue with existing user object
                        }
                    }
                }

                if (user == null)
                {
                    Console.WriteLine($"❌ User is still null after all operations");
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Không thể tìm thấy hoặc tạo user",
                        Status = 500,
                        Data = null
                    };
                }

                // ✅ KIỂM TRA STATUSID - QUAN TRỌNG!
                if (user.StatusId != 1)
                {
                    Console.WriteLine($"❌ User {contact} has inactive status: {user.StatusId}");
                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị khóa hoặc vô hiệu hóa",
                        Status = 403,
                        Data = null
                    };
                }

                Console.WriteLine($"✅ User validation passed: ID={user.UserId}, Status={user.StatusId}");

                // GENERATE JWT TOKENS
                try
                {
                    var tokens = _jwtHelper.GenerateTokens(user);
                    Console.WriteLine($"✅ JWT tokens generated successfully");

                    // Save tokens
                    var userToken = new UserToken
                    {
                        UserId = user.UserId,
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken
                    };

                    await _authRepository.SaveUserTokenAsync(userToken);
                    Console.WriteLine($"✅ User tokens saved to database");

                    // ✅ SAFE ACCESS ĐẾN GoogleSubject
                    string googleSubject = null;
                    if (isGoogleLogin)
                    {
                        try
                        {
                            var googleSubjectProperty = otpData.GetType().GetProperty("GoogleSubject");
                            if (googleSubjectProperty != null)
                            {
                                googleSubject = googleSubjectProperty.GetValue(otpData)?.ToString();
                            }
                        }
                        catch (Exception gsEx)
                        {
                            Console.WriteLine($"⚠️ Could not get GoogleSubject: {gsEx.Message}");
                        }
                    }

                    // Tạo response
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
                            Phone = user.Phone ?? null,
                            FullName = user.FullName ?? "",
                            Email = user.Email ?? "",
                            IsMale = user.IsMale,
                            Dob = user.Dob,
                            RoleId = user.RoleId,
                            RoleName = user.Role?.RoleName ?? "User",
                            CreateAt = user.CreateAt,
                            IsGoogleUser = isGoogleLogin, // ✅ Dùng flag từ otpData
                            GoogleId = googleSubject // ✅ Lấy GoogleId từ cache an toàn
                        }
                    };

                    string successMessage = isGoogleLogin
                        ? (isNewUser ? "Tài khoản Google đã được tạo và đăng nhập thành công" : "Đăng nhập Google thành công")
                        : (isNewUser ? "Tài khoản đã được tạo và đăng nhập thành công" : "Đăng nhập thành công");

                    Console.WriteLine($"✅ Login successful: {successMessage}");

                    return new ApiResponse<TokenResponseDto>
                    {
                        Success = true,
                        Message = successMessage,
                        Status = 200,
                        Data = response
                    };
                }
                catch (Exception tokenEx)
                {
                    Console.WriteLine($"❌ Error generating/saving tokens: {tokenEx.Message}");
                    Console.WriteLine($"❌ Token stack trace: {tokenEx.StackTrace}");
                    throw new Exception($"Lỗi tạo token: {tokenEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ General error in VerifyOtpAndLoginAsync: {ex.Message}");
                Console.WriteLine($"❌ Full stack trace: {ex.StackTrace}");

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

                var contact = payload.Email.Trim();

                // KIỂM TRA USER ĐÃ TỒN TẠI
                var existingUser = await _authRepository.GetUserByEmailAsync(payload.Email);

                // CASE 1: USER ĐÃ TỒN TẠI → LOGIN THẲNG
                if (existingUser != null)
                {
                    // ✅ KIỂM TRA STATUSID TRƯỚC KHI LOGIN
                    if (existingUser.StatusId != 1)
                    {
                        Console.WriteLine($"❌ Google user {contact} has inactive status: {existingUser.StatusId}");
                        return new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Tài khoản đã bị khóa hoặc vô hiệu hóa",
                            Status = 403,
                            Data = null
                        };
                    }

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
                            Phone = existingUser.Phone ?? null,
                            FullName = existingUser.FullName ?? "",
                            Email = existingUser.Email ?? "",
                            IsMale = existingUser.IsMale,
                            Dob = existingUser.Dob,
                            RoleId = existingUser.RoleId,
                            RoleName = existingUser.Role?.RoleName ?? "User",
                            CreateAt = existingUser.CreateAt,
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

                // CASE 2: USER CHƯA TỒN TẠI → CHỈ GỬI OTP, KHÔNG TẠO USER

                Console.WriteLine($"🆕 New Google user detected: {contact} - SENDING OTP (NOT CREATING USER YET)");

                // Check rate limiting
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

                // GENERATE OTP - KHÔNG TẠO USER
                var otp = GenerateOtp();
                var sessionToken = GenerateSessionToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(5);

                // Lưu thông tin Google vào cache để tạo user sau khi verify OTP
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
                    IsNewUser = true
                    // Không có UserId vì chưa tạo user
                };

                _cache.Set($"otp_{contact}_{sessionToken}", otpData, TimeSpan.FromMinutes(5));
                _cache.Set(rateLimitKey, true, TimeSpan.FromMinutes(1));

                // GỬI OTP EMAIL
                try
                {
                    await _emailService.SendOtpEmailForLoginAsync(contact, otp);
                    Console.WriteLine($"📧 OTP sent to new Google user: {contact}");
                }
                catch (Exception emailEx)
                {
                    // Cleanup nếu gửi email thất bại
                    _cache.Remove($"otp_{contact}_{sessionToken}");
                    _cache.Remove(rateLimitKey);

                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể gửi email xác thực",
                        Status = 500,
                        Data = null
                    };
                }

                // TRẢ VỀ OTP RESPONSE
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

        public async Task<ApiResponse<CheckUserExistResponse>> CheckUserExistAsync(string phoneOrEmail)
        {
            try
            {
                Console.WriteLine($"🔍 Checking user existence for: {phoneOrEmail}");

                // FIND USER BY EMAIL OR PHONE
                var user = await _authRepository.GetUserByEmailOrPhoneAsync(phoneOrEmail.Trim());

                if (user == null)
                {
                    Console.WriteLine($"❌ User not found: {phoneOrEmail}");
                    return new ApiResponse<CheckUserExistResponse>
                    {
                        Success = false,
                        Message = "Email/số điện thoại không tồn tại",
                        Status = 404,
                        Data = new CheckUserExistResponse
                        {
                            IsExist = false,
                            HasPassword = false
                        }
                    };
                }

                // ✅ KIỂM TRA STATUSID
                if (user.StatusId != 1)
                {
                    Console.WriteLine($"❌ User {phoneOrEmail} has inactive status: {user.StatusId}");
                    return new ApiResponse<CheckUserExistResponse>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị khóa hoặc vô hiệu hóa",
                        Status = 403,
                        Data = new CheckUserExistResponse
                        {
                            IsExist = true,
                            HasPassword = false // Không cần tiết lộ thêm thông tin
                        }
                    };
                }

                // CHECK IF USER HAS PASSWORD
                bool hasPassword = !string.IsNullOrEmpty(user.Password);

                if (!hasPassword)
                {
                    Console.WriteLine($"⚠️ User {phoneOrEmail} is Google-only user");
                    return new ApiResponse<CheckUserExistResponse>
                    {
                        Success = false,
                        Message = "Tài khoản này chỉ hỗ trợ đăng nhập bằng Google hoặc OTP",
                        Status = 400,
                        Data = new CheckUserExistResponse
                        {
                            IsExist = true,
                            HasPassword = false
                        }
                    };
                }

                Console.WriteLine($"✅ User exists and has password: {phoneOrEmail}");
                return new ApiResponse<CheckUserExistResponse>
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
                return new ApiResponse<CheckUserExistResponse>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi kiểm tra tài khoản",
                    Status = 500,
                    Data = new CheckUserExistResponse
                    {
                        IsExist = false,
                        HasPassword = false
                    }
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