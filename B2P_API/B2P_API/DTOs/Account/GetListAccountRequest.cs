namespace B2P_API.DTOs.Account
{
	public class GetListAccountRequest
	{
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 10;
		public string? Search { get; set; } = "";
		public int? RoleId { get; set; }
		public int? StatusId { get; set; }
	}
}
