namespace B2P_API.DTOs.Account
{
	public class GetListAccountResponse
	{
		public int UserId { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public string? RoleName { get; set; }
		public string? StatusName { get; set; }
	}
}
