using B2P_API.Models;

namespace B2P_API.Interface
{
	public interface IAccountManagementRepository
	{
		Task<User?> GetByIdAsync(int userId);

		Task<List<User>> GetAllAsync(
			int pageNumber,
			int pageSize,
			string? search,
			int? roleId,
			int? statusId
		);

		Task<int> GetTotalAccountsAsync(
			string? search,
			int? roleId,
			int? statusId
		);

		Task<bool> UpdateStatusAsync(User user, int newStatusId);

		Task<bool> DeleteUserAsync(User user);
	}
}
