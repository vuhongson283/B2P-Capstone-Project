namespace B2P_API.DTOs.ImageDTOs
{
    public class ImageUploadResponse
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public int Order { get; set; }
        public string Message { get; set; } = null!;
    }
}
