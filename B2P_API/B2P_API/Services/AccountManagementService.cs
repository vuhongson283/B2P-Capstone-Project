using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace B2P_API.Services
{
	public class AccountManagementService
	{
		private readonly IAccountManagementRepository _repo;
		private readonly IMapper _mapper;

		public AccountManagementService(
			IAccountManagementRepository repo,
			IMapper mapper)
		{
			_repo = repo;
			_mapper = mapper;
		}

		public async Task<ApiResponse<PagedResponse<GetListAccountResponse>>> GetAllAccountsAsync(GetListAccountRequest request)
		{
			try
			{
				var paged = await _repo.GetAllAccountsAsync(
					request.PageNumber,
					request.PageSize,
					request.Search,
					request.RoleId,
					request.StatusId
				);

				// Thành công, có kết quả
				return new ApiResponse<PagedResponse<GetListAccountResponse>>
				{
					Success = true,
					Message = "Tải Lên Tài Khoản Thành Công",
					Status =200,
					Data = paged
				};
			}
			catch (Exception)
			{
				return new ApiResponse<PagedResponse<GetListAccountResponse>>
				{
					Success = false,
					Message = MessagesCodes.MSG_46,
					Status = 500,
					Data = null
				};
			}
		}
		public async Task<ApiResponse<GetAccountByIdResponse>> GetAccountByIdAsync(int userId)
		{
			try
			{
				// Gọi repo để lấy luôn ApiResponse<GetAccountByIdResponse>
				var repoResult = await _repo.GetAccountByIdAsync(userId);

				// Nếu repository trả về lỗi (ví dụ 404), pass thẳng qua cho tầng trên
				if (!repoResult.Success)
				{
					return repoResult;
				}

				// Thành công, trả về dữ liệu
				return new ApiResponse<GetAccountByIdResponse>
				{
					Success = true,
					Message = repoResult.Message,
					Status = 200,
					Data = repoResult.Data
				};
			}
			catch (Exception)
			{
				return new ApiResponse<GetAccountByIdResponse>
				{
					Success = false,
					Message = MessagesCodes.MSG_46,
					Status = 500,
					Data = null
				};
			}
		}

		public async Task<ApiResponse<string>> BanUserAsync(int userId)
		{
			try
			{
				// Gọi repo, repo sẽ trả ApiResponse<string>
				var result = await _repo.BanUserAsync(userId);
				return result;
			}
			catch (Exception)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = MessagesCodes.MSG_46,
					Status = 500,
					Data = null
				};
			}
		}
		public async Task<ApiResponse<string>> UnBanUserAsync(int userId)
		{
			try
			{
				// Gọi repo, repo sẽ trả ApiResponse<string>
				var result = await _repo.UnbanUserAsync(userId);
				return result;
			}
			catch (Exception)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = MessagesCodes.MSG_46,
					Status = 500,
					Data = null
				};
			}
		}

		public async Task<ApiResponse<string>> DeleteUserAsync(int userId)
		{
			try
			{
				// Gọi repo, repo sẽ trả ApiResponse<string>
				var result = await _repo.DeleteUserAsync(userId);
				return result;
			}
			catch (Exception)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = MessagesCodes.MSG_50,
					Status = 500,
					Data = null
				};
			}
		}
	}
}