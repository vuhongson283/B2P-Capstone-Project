namespace B2P_API.DTOs.ImageDTOs
{
    public class UpdateImageRequest
    {
        public IFormFile? File { get; set; } // File ảnh mới để upload
        public int? Order { get; set; }
        public string? Caption { get; set; }
        // Không cần ImageUrl vì sẽ được generate từ file upload
    }
}
