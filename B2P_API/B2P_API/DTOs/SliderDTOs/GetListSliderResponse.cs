using B2P_API.DTOs.ImageDTOs;

namespace B2P_API.DTOs.SliderDTOs
{
	public class GetListSliderResponse
	{
		public int SlideId { get; set; }

		public string? SlideDescription { get; set; }

		public string? SlideUrl { get; set; }

		public string? StatusName { get; set; }
		public string? ImageUrl { get; set; } 

	}
}
