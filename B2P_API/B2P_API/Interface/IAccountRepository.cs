using B2P_API.DTOs.Account;
using B2P_API.Models;
using System.Threading.Tasks;

namespace B2P_API.Interface
{
	public interface IAccountRepository
	{
		Task<bool> IsEmailExistsAsync(string email);
		Task<bool> IsPhoneExistsAsync(string phone);
		Task<bool> IsRealEmailAsync(string email);
		Task<User> RegisterAccountAsync(User user);
	}
}
