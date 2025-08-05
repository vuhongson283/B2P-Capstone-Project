using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
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

		public AccountService(
			IAccountRepository accountRepository,
			IImageRepository imageRepository,
			IMapper mapper,
			IConfiguration configuration)
		{
			_imageRepository = imageRepository;
			_accountRepository = accountRepository;
			_mapper = mapper;
			_configuration = configuration;
		}

		public async Task<ApiResponse<string>> RegisterCourtOwnerAsync(RegisterAccountRequest request)
		{
			try
			{
				// Check request is not null
				if (request == null)
				{
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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
					return new ApiResponse<string>
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

				return new ApiResponse<string>
				{
					Success = true,
					Message = "Đăng ký thành công.",
					Status = 201,
					Data = createdUser.UserId.ToString()
				};
			}
			catch (Exception ex)
			{
				var inner = ex.InnerException?.Message;
				return new ApiResponse<string>
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