namespace B2P_API.DTOs.ImageDTOs
{
    public class ImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; }
        public int ? Order { get; set; }
        public string Caption { get; set; }
    }
}
