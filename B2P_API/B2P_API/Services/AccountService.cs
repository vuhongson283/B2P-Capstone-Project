using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;

namespace B2P_API.Services
{
	public class AccountService
	{
		private readonly IAccountRepository _accountRepository;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public AccountService(
			IAccountRepository accountRepository,
			IMapper mapper,
			IConfiguration configuration)
		{
			_accountRepository = accountRepository;
			_mapper = mapper;
			_configuration = configuration;
		}

		public async Task<ApiResponse<string>> RegisterAccountAsync(RegisterAccountRequest request)
		{
			try
			{
				// Check email exists
				if (await _accountRepository.IsEmailExistsAsync(request.Email))
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_64,
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
						Message = MessagesCodes.MSG_68,
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
						Message = MessagesCodes.MSG_64,
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
					RoleId = 2,
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
