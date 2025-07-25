using B2P_API.Interface;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
	public class AccountManagementRepository : IAccountManagementRepository
	{
		private readonly SportBookingDbContext _context;

		public AccountManagementRepository(SportBookingDbContext context)
		{
			_context = context;
		}

		public async Task<User?> GetByIdAsync(int userId)
		{
			return await _context.Users
				.Include(u => u.Role)
				.Include(u => u.Status)
				.Include(u => u.Images)
					.Include(u => u.UserTokens)
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
			.ThenInclude(b => b.BookingDetails)
		.Include(u => u.Comments)
		.Include(u => u.BankAccount)
				.FirstOrDefaultAsync(u => u.UserId == userId);
		}


		public async Task<List<User>> GetAllAsync(
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

			if (!string.IsNullOrWhiteSpace(search))
			{
				var loweredSearch = search.ToLower();

				query = query.Where(u =>
					u.FullName.ToLower().Contains(loweredSearch) ||
					u.Email.ToLower().Contains(loweredSearch) ||
					u.Phone.ToLower().Contains(loweredSearch) ||
					u.UserId.ToString().Contains(loweredSearch));
			}


			if (roleId.HasValue)
				query = query.Where(u => u.RoleId == roleId.Value);

			if (statusId.HasValue)
				query = query.Where(u => u.StatusId == statusId.Value);

			return await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
		}

		public async Task<int> GetTotalAccountsAsync(string? search, int? roleId, int? statusId)
		{
			var allowedRoles = new[] { 2, 3 };

			var query = _context.Users
				.Where(u => allowedRoles.Contains(u.RoleId))
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				var loweredSearch = search.ToLower();

				query = query.Where(u =>
					u.FullName.ToLower().Contains(loweredSearch) ||
					u.Email.ToLower().Contains(loweredSearch) ||
					u.Phone.ToLower().Contains(loweredSearch) ||
					u.UserId.ToString().Contains(loweredSearch));
			}

			if (roleId.HasValue)
				query = query.Where(u => u.RoleId == roleId.Value);

			if (statusId.HasValue)
				query = query.Where(u => u.StatusId == statusId.Value);

			return await query.CountAsync();
		}


		public async Task<bool> UpdateStatusAsync(User user, int newStatusId)
		{
			user.StatusId = newStatusId;
			_context.Users.Update(user);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteUserAsync(User user)
		{
			if (user.Images.Any())
				_context.Images.RemoveRange(user.Images);

			var blogComments = user.Blogs.SelectMany(b => b.Comments);
			if (blogComments.Any()) _context.Comments.RemoveRange(blogComments);

			var blogImages = user.Blogs.SelectMany(b => b.Images);
			if (blogImages.Any()) _context.Images.RemoveRange(blogImages);

			var facilityCourts = user.Facilities.SelectMany(f => f.Courts);
			var facilityImages = user.Facilities.SelectMany(f => f.Images);
			var facilityTimeSlots = user.Facilities.SelectMany(f => f.TimeSlots);
			if (facilityCourts.Any()) _context.Courts.RemoveRange(facilityCourts);
			if (facilityImages.Any()) _context.Images.RemoveRange(facilityImages);
			if (facilityTimeSlots.Any()) _context.TimeSlots.RemoveRange(facilityTimeSlots);

			var bookingDetails = user.Bookings.SelectMany(b => b.BookingDetails);
			if (bookingDetails.Any())
				_context.Set<BookingDetail>().RemoveRange(bookingDetails);

			var payments = user.Bookings.SelectMany(b => b.Payments);
			var ratings = user.Bookings.SelectMany(b => b.Ratings);
			if (payments.Any()) _context.Payments.RemoveRange(payments);
			if (ratings.Any()) _context.Ratings.RemoveRange(ratings);

			if (user.BankAccount != null) _context.BankAccounts.Remove(user.BankAccount);
			if (user.UserTokens.Any()) _context.UserTokens.RemoveRange(user.UserTokens);
			if (user.Comments.Any()) _context.Comments.RemoveRange(user.Comments);
			if (user.Bookings.Any()) _context.Bookings.RemoveRange(user.Bookings);
			if (user.Facilities.Any()) _context.Facilities.RemoveRange(user.Facilities);
			if (user.Blogs.Any()) _context.Blogs.RemoveRange(user.Blogs);

			_context.Users.Remove(user);
			await _context.SaveChangesAsync();

			return true;
		}
	}
}
