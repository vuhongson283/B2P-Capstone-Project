using B2P_API.DTOs.Account;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Interface
{
	public interface IAccountManagementRepository
	{
		Task<PagedResponse<GetListAccountResponse>> GetAllAccountsAsync(
			int pageNumber, int pageSize,
			string? search,
			int? roleId, int? statusId);

		Task<ApiResponse<GetAccountByIdResponse>> GetAccountByIdAsync(int userId);
		Task<ApiResponse<string>> BanUserAsync(int userId);
		Task<ApiResponse<string>> UnbanUserAsync(int userId);
		Task<ApiResponse<string>> DeleteUserAsync(int userId);
	}

}
