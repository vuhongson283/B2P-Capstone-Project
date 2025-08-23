using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.DTOs.AuthDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using B2P_API.Utils;
using System.Text.RegularExpressions;

namespace B2P_API.Services
{
	public class AccountService
	{
		private readonly IAccountRepository _accountRepository;
		private readonly IImageRepository _imageRepository;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;
		private readonly JWTHelper _jwtHelper;
		private readonly IAuthRepository _authRepository;
        public AccountService(
			IAccountRepository accountRepository,
			IImageRepository imageRepository,
			IMapper mapper,
			IConfiguration configuration,
			JWTHelper jWTHelper,
			IAuthRepository authRepository)
		{
			_imageRepository = imageRepository;
			_accountRepository = accountRepository;
			_mapper = mapper;
			_configuration = configuration;
			_jwtHelper = jWTHelper;
			_authRepository = authRepository;
        }

		public async Task<ApiResponse<TokenResponseDto>> RegisterCourtOwnerAsync(RegisterAccountRequest request)
		{
			try
			{
				// Check request is not null
				if (request == null)
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Request không hợp lệ",
						Status = 400,
						Data = null
					};
				}

				// Check FullName is not null or empty
				if (string.IsNullOrWhiteSpace(request.FullName))
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Tên không được để trống",
						Status = 400,
						Data = null
					};
				}

				// Check Address is not null or empty
				if (string.IsNullOrWhiteSpace(request.Address))
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Địa chỉ không được để trống",
						Status = 400,
						Data = null
					};
				}

				// Check email exists
				if (await _accountRepository.IsEmailExistsAsync(request.Email))
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Email đã tồn tại trong hệ thống",
						Status = 400,
						Data = null
					};
				}

				// Check email MX record
				var isRealEmail = await _accountRepository.IsRealEmailAsync(request.Email);
				if (!isRealEmail)
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Format Email bị sai !",
						Status = 400,
						Data = null
					};
				}

				// Check phone format (only digits, exactly 10 characters)
				if (!Regex.IsMatch(request.PhoneNumber, @"^\d{10}$"))
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Format số điện thoại không phù hợp",
						Status = 400,
						Data = null
					};
				}

				// Check phone exists
				if (await _accountRepository.IsPhoneExistsAsync(request.PhoneNumber))
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Số điện thoại đã tồn tại",
						Status = 400,
						Data = null
					};
				}

				// Check password requirements
				if (!Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự",
						Status = 400,
						Data = null
					};
				}

				// Check password and confirm password match
				if (request.Password != request.ConfirmPassword)
				{
					return new ApiResponse<TokenResponseDto>
					{
						Success = false,
						Message = "Password và ConfirmPassword không khớp",
						Status = 400,
						Data = null
					};
				}

				// Hash password
				var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

				// Create user entity
				var newUser = new User
				{
					FullName = request.FullName,
					Email = request.Email,
					Phone = request.PhoneNumber,
					Password = hashedPassword,
					IsMale = request.IsMale,
					RoleId = 3, // CourtOwner role
					StatusId = 1,
					Address = request.Address,
					CreateAt = DateTime.UtcNow
				};

				// Save user
				var createdUser = await _accountRepository.RegisterAccountAsync(newUser);
				await _imageRepository.CreateUserDefaultImageAsync(createdUser.UserId);

				// ✅ Generate JWT ngay sau khi đăng ký
				var tokens = _jwtHelper.GenerateTokens(createdUser);

				// Lưu token vào DB
				await _authRepository.SaveUserTokenAsync(new UserToken
				{
					UserId = createdUser.UserId,
					AccessToken = tokens.AccessToken,
					RefreshToken = tokens.RefreshToken
				});

				// Chuẩn bị response
				var response = new TokenResponseDto
				{
					AccessToken = tokens.AccessToken,
					RefreshToken = tokens.RefreshToken,
					ExpiresAt = tokens.ExpiresAt,
					TokenType = tokens.TokenType,
					IsNewUser = true,
					User = new UserInfoDto
					{
						UserId = createdUser.UserId,
						Phone = createdUser.Phone,
						FullName = createdUser.FullName ?? "",
						Email = createdUser.Email ?? "",
						IsMale = createdUser.IsMale,
						Dob = createdUser.Dob,
						RoleId = createdUser.RoleId,
						RoleName = createdUser.Role?.RoleName ?? "CourtOwner",
						CreateAt = createdUser.CreateAt,
						IsGoogleUser = false
					}
				};

				return new ApiResponse<TokenResponseDto>
				{
					Success = true,
					Message = "Đăng ký thành công và đã đăng nhập.",
					Status = 200,
					Data = response
				};
			}


			catch (Exception ex)
			{
				var inner = ex.InnerException?.Message;
				return new ApiResponse<TokenResponseDto>
				{
					Success = false,
					Message = $"{MessagesCodes.MSG_37}: {ex.Message}" +
							  (inner != null ? $"\nInner: {inner}" : ""),
					Status = 500,
					Data = null
				};
			}
		}
	}
}