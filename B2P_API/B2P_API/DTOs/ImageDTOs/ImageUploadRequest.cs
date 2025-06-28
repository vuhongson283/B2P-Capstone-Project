namespace B2P_API.DTOs.ImageDTOs
{
    public class ImageUploadRequest
    {
        public IFormFile File { get; set; }
        public int EntityId { get; set; }
        public string? Caption { get; set; }
    }
}
