using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Utils;
using BCrypt.Net;
using DnsClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

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
				throw new Exception($"{MessagesCodes.MSG_06}. Details: {ex.Message}");
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
				throw new Exception($"{MessagesCodes.MSG_06}. Details: {ex.Message}");
			}
		}

		public async Task<bool> IsRealEmailAsync(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
				return false;

			try
			{
				var addr = new MailAddress(email);
				var domain = addr.Host;

				var lookup = new LookupClient();
				var result = await lookup.QueryAsync(domain, QueryType.MX);

				return result.Answers.MxRecords().Any();
			}
			catch
			{
				return false;
			}
		}

		public async Task<User> RegisterAccountAsync(User user)
		{
			_context.Users.Add(user);
			await _context.SaveChangesAsync();
			return user;
		}
	}
}
