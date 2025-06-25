using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace B2P_API.Repository
{
	public class AccountRepository : IAccountRepository
	{
		private readonly SportBookingDbContext _context;

		public AccountRepository(SportBookingDbContext context)
		{
			_context = context;
		}

		public async Task<bool> IsEmailExistsAsync(string email)
		{
			try
			{
				return await _context.Users
					.AsNoTracking()
					.AnyAsync(u => u.Email == email);
			}
			catch (Exception ex)
			{
				// Log exception if needed
				throw new Exception(MessagesCodes.MSG_06);
			}
		}

		public async Task<bool> IsPhoneExistsAsync(string phone)
		{
			try
			{
				return await _context.Users
					.AsNoTracking()
					.AnyAsync(u => u.Phone == phone);
			}
			catch (Exception ex)
			{
				// Log exception if needed
				throw new Exception(MessagesCodes.MSG_06);
			}
		}

		public async Task<ApiResponse<string>> RegisterAccountAsync(RegisterAccountRequest request)
		{
			try
			{
				// Kiểm tra email
				if (await IsEmailExistsAsync(request.Email))
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_64,
						Status = 400,
						Data = null
					};
				}

				// Kiểm tra số điện thoại
				if (await IsPhoneExistsAsync(request.PhoneNumber))
				{
					return new ApiResponse<string>
					{
						Success = false,
						Message = MessagesCodes.MSG_64,
						Status = 400,
						Data = null
					};
				}

				// Tạo user mới
				var user = new User
				{
					FullName = request.FullName,
					Email = request.Email,
					Phone = request.PhoneNumber,
					Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
					IsMale = request.IsMale,
					RoleId = 2,
					StatusId = 1,
					Address = request.Address,
					CreateAt = DateTime.UtcNow
				};

				_context.Users.Add(user);
				await _context.SaveChangesAsync();

				return new ApiResponse<string>
				{
					Success = true,
					Message = "Đăng ký thành công.",
					Status = 201,
					Data = user.UserId.ToString()
				};
			}
			catch (Exception ex)
			{
				return new ApiResponse<string>
				{
					Success = false,
					Message = MessagesCodes.MSG_37+ex.Message,
					Status = 500,
					Data = null
				};
			}
		}
	}
}
