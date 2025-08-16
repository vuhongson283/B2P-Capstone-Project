using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;

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
				if (request == null)
					return new ApiResponse<PagedResponse<GetListAccountResponse>>
					{
						Success = false,
						Message = "Request bị null.",
						Status = 400,
						Data = null
					};

				if (request.PageSize <= 0)
					request.PageSize = 10;

				var users = await _repo.GetAllAsync(
					request.PageNumber,
					request.PageSize,
					request.Search,
					request.RoleId,
					request.StatusId);

				if (users == null)
				{
					return new ApiResponse<PagedResponse<GetListAccountResponse>>
					{
						Success = false,
						Message = "Không thể tải danh sách tài khoản. Repository trả về null.",
						Status = 500,
						Data = null
					};
				}

				if (users.Any(u => u == null))
				{
					return new ApiResponse<PagedResponse<GetListAccountResponse>>
					{
						Success = false,
						Message = "Có phần tử null trong danh sách users.",
						Status = 500,
						Data = null
					};
				}

				var totalItems = await _repo.GetTotalAccountsAsync(
					request.Search,
					request.RoleId,
					request.StatusId);

				var mapped = users
	   .Where(u => u != null)
	   .Select(u => new GetListAccountResponse
	   {
		   UserId = u.UserId,
		   FullName = u.FullName,
		   Email = u.Email,
		   Phone = u.Phone,
		   RoleName = u.Role?.RoleName ?? "(Unknown Role)",
		   StatusName = u.Status?.StatusName ?? "(Unknown Status)"
	   }).ToList();



				if (!mapped.Any())
				{
					return new ApiResponse<PagedResponse<GetListAccountResponse>>
					{
						Success = false,
						Message = "Không có tài khoản nào khớp với tiêu chí tìm kiếm của bạn.",
						Status = 404,
						Data = null
					};
				}

				var paged = new PagedResponse<GetListAccountResponse>
				{
					CurrentPage = request.PageNumber,
					ItemsPerPage = request.PageSize,
					TotalItems = totalItems,
					TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize),
					Items = mapped
				};

				return new ApiResponse<PagedResponse<GetListAccountResponse>>
				{
					Success = true,
					Message = "Tải Lên Tài Khoản Thành Công.",
					Status = 200,
					Data = paged
				};
			}
			catch (Exception ex)
			{
				var inner = ex.InnerException?.Message;
				return new ApiResponse<PagedResponse<GetListAccountResponse>>
				{
					Success = false,
					Message = $"{MessagesCodes.MSG_45}: {ex.Message}" +
							  (inner != null ? $"\nInner: {inner}" : "") +
							  $"\nStackTrace: {ex.StackTrace}",
					Status = 500,
					Data = null
				};
			}
		}



		public async Task<ApiResponse<GetAccountByIdResponse>> GetAccountByIdAsync(int userId)
		{
			try
			{
				// ✅ SỬA: Dùng method tối ưu
				var user = await _repo.GetByIdForDisplayAsync(userId);
				if (user == null)
				{
					return new ApiResponse<GetAccountByIdResponse>
					{
						Success = false,
						Message = MessagesCodes.MSG_46,
						Status = 404,
						Data = null
					};
				}

				var avatarUrl = user.Images
					.Where(i => i.UserId == user.UserId)
					.OrderBy(i => i.Order)
					.FirstOrDefault()?.ImageUrl;

				var dto = new GetAccountByIdResponse
				{
					UserId = user.UserId,
					Statusname = user.Status?.StatusName ?? string.Empty,
					Email = user.Email,
					Phone = user.Phone,
					IsMale = user.IsMale,
					AvatarUrl = avatarUrl,
					RoleName = user.Role?.RoleName ?? string.Empty,
					CreateAt = user.CreateAt,
					FullName = user.FullName,
					Address = user.Address,
					Dob = user.Dob
				};

				return new ApiResponse<GetAccountByIdResponse>
				{
					Success = true,
					Message = "Lấy thông tin tài khoản thành công.",
					Status = 200,
					Data = dto
				};
			}
			catch (Exception ex)
			{
				var inner = ex.InnerException?.Message;
				return new ApiResponse<GetAccountByIdResponse>
				{
					Success = false,
					Message = $"{MessagesCodes.MSG_50}: {ex.Message}" +
							  (inner != null ? $"\nInner: {inner}" : ""),
					Status = 500,
					Data = null
				};
			}
		}

		public async Task<ApiResponse<string>> BanUserAsync(int userId)
		{
			try
			{
				var user = await _repo.GetByIdAsync(userId);
				if (user == null)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_46,
						Status = 404,
						Data = null
					};
				}

				if (user.StatusId == 4)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = "Tài khoản này đã bị cấm rồi.",
						Status = 400,
						Data = null
					};
				}

				await _repo.UpdateStatusAsync(user, 4);

				return new ApiResponse<string>
				{
					Success = true,
					Message = "Khoá tài khoản thành công.",
					Status = 200,
					Data = userId.ToString()
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

		public async Task<ApiResponse<string>> UnBanUserAsync(int userId)
		{
			try
			{
				var user = await _repo.GetByIdAsync(userId);
				if (user == null)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_46,
						Status = 404,
						Data = null
					};
				}

				if (user.StatusId == 1)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = "Tài khoản này đã được hoạt động rồi, không thể gỡ cấm.",
						Status = 400,
						Data = null
					};
				}

				await _repo.UpdateStatusAsync(user, 1);

				return new ApiResponse<string>
				{
					Success = true,
					Message = "Mở khoá tài khoản thành công.",
					Status = 200,
					Data = userId.ToString()
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

		public async Task<ApiResponse<string>> DeleteUserAsync(int userId)
		{
			try
			{
				var user = await _repo.GetByIdAsync(userId);
				if (user == null)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_46,
						Status = 404,
						Data = null
					};
				}

				if (user.StatusId != 4)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = "Chỉ được xóa tài khoản đang ở trạng thái banned.",
						Status = 400,
						Data = null
					};
				}

				var deleted = await _repo.DeleteUserAsync(user);
				if (!deleted)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_50,
						Status = 500,
						Data = null
					};
				}

				return new ApiResponse<string>
				{
					Success = true,
					Message = MessagesCodes.MSG_48,
					Status = 200,
					Data = userId.ToString()
				};
			}
			catch (Exception ex)
			{
				var inner = ex.InnerException?.Message;
				return new ApiResponse<string>
				{
					Success = false,
					Message = $"{MessagesCodes.MSG_50}: {ex.Message}" +
							  (inner != null ? $"\nInner: {inner}" : ""),
					Status = 500,
					Data = null
				};
			}
		}
	}
}
