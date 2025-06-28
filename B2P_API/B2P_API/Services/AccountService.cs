using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Repository;
using B2P_API.Response;

namespace B2P_API.Services
{
	public class AccountService
	{

		private readonly AccountRepository _accountRepository;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;
		public AccountService(AccountRepository accountManageRepository, IMapper mapper, IConfiguration configuration)
		{
			_accountRepository = accountManageRepository;
			_mapper = mapper;
			_configuration = configuration;
		}
		public async Task<ApiResponse<string>> RegisterAccountAsync(RegisterAccountRequest request)
		{

			var result = await _accountRepository.RegisterAccountAsync(request);

			if (result.Success)
			{
				// Ví dụ: sinh JWT
				// string token = GenerateJwtToken(user);
				// result.Data = token;
			}

			return result;
		}


	}
}
