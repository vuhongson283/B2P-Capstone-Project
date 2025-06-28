namespace B2P_API.DTOs.ImageDTOs
{
    public class ImageResponseDto
    {
        public int ImageId { get; set; }
        public int? FacilityId { get; set; }
        public int? BlogId { get; set; }
        public int? UserId { get; set; }
        public int? SlideId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public int? Order { get; set; }
        public string? Caption { get; set; }
    }
}
