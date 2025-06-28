using B2P_API.Models;

namespace B2P_API.DTOs.Account
{
	public class GetAccountByIdResponse
	{
		public int UserId { get; set; }

		public string Statusname { get; set; }

		public string Username { get; set; } = null!;

		public string Email { get; set; } = null!;

		public string Phone { get; set; } = null!;

		public bool? IsMale { get; set; }

		public string? AvatarUrl { get; set; }

		public string RoleName { get; set; }

		public DateTime? CreateAt { get; set; }

		public string FullName { get; set; } = null!;

		public string? Address { get; set; }

		public DateOnly? Dob { get; set; }

	}
}
