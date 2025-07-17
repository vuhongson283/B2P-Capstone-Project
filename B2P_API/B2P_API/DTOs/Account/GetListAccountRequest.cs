namespace B2P_API.DTOs.Account
{
	public class GetListAccountRequest
	{
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public string? Search { get; set; }
		public int? RoleId { get; set; }
		public int? StatusId { get; set; }
	}

}
