using B2P_API.DTOs.ImageDTOs;

namespace B2P_API.DTOs.SliderDTOs
{
	public class GetListSliderRequest
	{
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 10;
		public string? Search { get; set; } 
		public int? StatusId { get; set; }

	}
}
