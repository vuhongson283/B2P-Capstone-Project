using B2P_API.DTOs.Account;
using B2P_API.Models;
using B2P_API.Response;
using System.Threading.Tasks;

namespace B2P_API.Interface
{
	public interface IAccountRepository
	{
		/// Đăng ký tài khoản mới.
		Task<ApiResponse<string>> RegisterAccountAsync(RegisterAccountRequest request);
		/// Kiểm tra email đã tồn tại hay chưa.
		Task<bool> IsEmailExistsAsync(string email);
		/// Kiểm tra số điện thoại đã tồn tại hay chưa.
		Task<bool> IsPhoneExistsAsync(string phone);
	}
}
