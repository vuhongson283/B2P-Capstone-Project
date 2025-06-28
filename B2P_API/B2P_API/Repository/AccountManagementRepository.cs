using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2P_API.Repository
{
	public class AccountManagementRepository : IAccountManagementRepository
	{
		private readonly SportBookingDbContext _context;
		private readonly IMapper _mapper;

		public AccountManagementRepository(SportBookingDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<ApiResponse<string>> BanUserAsync(int userId)
		{
			try
			{
				var user = await _context.Users.FindAsync(userId);
				if (user == null)
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_46, // Không tìm thấy tài khoản
						Status = 404,
						Data = null
					};
				}

				// Giả sử StatusId = 3 là "banned"
				user.StatusId = 4;
				_context.Users.Update(user);
				await _context.SaveChangesAsync();

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
				return new ApiResponse<string>
				{
					Success = false,
					Message = MessagesCodes.MSG_37 + ex.Message, // Lỗi server
					Status = 500,
					Data = null
				};
			}
		}

		public async Task<ApiResponse<string>> UnbanUserAsync(int userId)
		{
			try
			{
				var user = await _context.Users.FindAsync(userId);
				if (user == null)
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_46,
						Status = 404,
						Data = null
					};

				user.StatusId = 1;
				_context.Users.Update(user);
				await _context.SaveChangesAsync();

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
				return new ApiResponse<string>
				{
					Success = false,
					Message = MessagesCodes.MSG_37 + ex.Message,
					Status = 500,
					Data = null
				};
			}
		}

		public async Task<ApiResponse<GetAccountByIdResponse>> GetAccountByIdAsync(int userId)
		{
			
				var user = await _context.Users
					.Include(u => u.Role)
					.Include(u => u.Status)
					.Include(u=>u.Images)
					.AsNoTracking()
					.FirstOrDefaultAsync(u => u.UserId == userId);

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
            .Where(i => i.UserId == user.UserId)   // chỉ ảnh mà UserId = user.UserId
            .OrderBy(i => i.Order)                 // nếu bạn có Order để ưu tiên
            .FirstOrDefault()?.ImageUrl;
			// Manual mapping sang DTO
			var dto = new GetAccountByIdResponse
			{
				UserId = user.UserId,
				Statusname = user.Status?.StatusName ?? string.Empty,
				Email = user.Email,
				Phone = user.Phone,
				IsMale = user.IsMale,
				AvatarUrl = avatarUrl,               // đúng ảnh của user
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

		public async Task<PagedResponse<GetListAccountResponse>> GetAllAccountsAsync(
			int pageNumber,
			int pageSize,
			string? search,
			int? roleId,
			int? statusId)
		{
			var allowedRoles = new[] { 2, 3 };

			var query = _context.Users
				.Include(u => u.Role)
				.Include(u => u.Status)
				 .Where(u => allowedRoles.Contains(u.RoleId))
				.AsQueryable();

			// Tìm kiếm
			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(u =>
					u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					u.Phone.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					u.UserId.ToString().Contains(search));
			}

			// Lọc theo Role
			if (roleId.HasValue)
			{
				query = query.Where(u => u.RoleId == roleId.Value);
			}

			// Lọc theo Status
			if (statusId.HasValue)
			{
				query = query.Where(u => u.StatusId == statusId.Value);
			}

			// Tổng số bản ghi
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			// Phân trang
			var data = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Ánh xạ sang DTO
			var mappedData = _mapper.Map<List<GetListAccountResponse>>(data);

			// Nếu không có kết quả phù hợp
			if (mappedData == null || !mappedData.Any())
			{
				throw new Exception(MessagesCodes.MSG_46);
			}

			return new PagedResponse<GetListAccountResponse>
			{
				CurrentPage = pageNumber,
				ItemsPerPage = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = mappedData
			};
		}

		public async Task<ApiResponse<string>> DeleteUserAsync(int userId)
		{
			try
			{
				var user = await _context.Users
					.Include(u => u.Blogs)
						.ThenInclude(b => b.Comments)
					.Include(u => u.Blogs)
						.ThenInclude(b => b.Images)
					.Include(u => u.Facilities)
						.ThenInclude(f => f.Images)
					.Include(u => u.Facilities)
						.ThenInclude(f => f.Courts)
					.Include(u => u.Facilities)
						.ThenInclude(f => f.TimeSlots)
					.Include(u => u.Bookings)
						.ThenInclude(b => b.Payments)
					.Include(u => u.Bookings)
						.ThenInclude(b => b.Ratings)
					.Include(u => u.Bookings)
						.ThenInclude(b => b.BookingDetails)    // <-- thêm include này
					.Include(u => u.Comments)
					.Include(u => u.UserTokens)
					.Include(u => u.BankAccount)
					.Include(u => u.Images)                    // nhớ include ảnh user nếu có
					.FirstOrDefaultAsync(u => u.UserId == userId);

				if (user == null)
					return new ApiResponse<string> { Success = false, Message = MessagesCodes.MSG_46, Status = 404 };

				if (user.StatusId != 4)
					return new ApiResponse<string> { Success = false, Message = "Chỉ được xóa tài khoản đang ở trạng thái banned.", Status = 400 };

				// 1) Xóa ảnh user
				if (user.Images.Any())
					_context.Images.RemoveRange(user.Images);

				// 2) Xóa "cháu" của Blog
				var blogComments = user.Blogs.SelectMany(b => b.Comments);
				if (blogComments.Any()) _context.Comments.RemoveRange(blogComments);

				var blogImages = user.Blogs.SelectMany(b => b.Images);
				if (blogImages.Any()) _context.Images.RemoveRange(blogImages);

				// 3) Xóa "cháu" của Facility
				var facilityCourts = user.Facilities.SelectMany(f => f.Courts);
				var facilityImages = user.Facilities.SelectMany(f => f.Images);
				var facilityTimeSlots = user.Facilities.SelectMany(f => f.TimeSlots);
				if (facilityCourts.Any()) _context.Courts.RemoveRange(facilityCourts);
				if (facilityImages.Any()) _context.Images.RemoveRange(facilityImages);
				if (facilityTimeSlots.Any()) _context.TimeSlots.RemoveRange(facilityTimeSlots);

				// 4) Xóa "cháu" của Booking
				// 4.1) BookingDetails
				var bookingDetails = user.Bookings.SelectMany(b => b.BookingDetails);
				if (bookingDetails.Any())
					_context.Set<BookingDetail>().RemoveRange(bookingDetails);

				// 4.2) Payments, Ratings
				var payments = user.Bookings.SelectMany(b => b.Payments);
				var ratings = user.Bookings.SelectMany(b => b.Ratings);
				if (payments.Any()) _context.Payments.RemoveRange(payments);
				if (ratings.Any()) _context.Ratings.RemoveRange(ratings);

				// 5) Xóa các con cấp 1
				if (user.BankAccount != null) _context.BankAccounts.Remove(user.BankAccount);
				if (user.UserTokens.Any()) _context.UserTokens.RemoveRange(user.UserTokens);
				if (user.Comments.Any()) _context.Comments.RemoveRange(user.Comments);
				if (user.Bookings.Any()) _context.Bookings.RemoveRange(user.Bookings);
				if (user.Facilities.Any()) _context.Facilities.RemoveRange(user.Facilities);
				if (user.Blogs.Any()) _context.Blogs.RemoveRange(user.Blogs);

				// 6) Xóa User
				_context.Users.Remove(user);
				await _context.SaveChangesAsync();

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
					Message = $"{MessagesCodes.MSG_50}: {ex.Message}"
							  + (inner != null ? $"\nInner: {inner}" : ""),
					Status = 500,
					Data = null
				};
			}
		}







	}
}