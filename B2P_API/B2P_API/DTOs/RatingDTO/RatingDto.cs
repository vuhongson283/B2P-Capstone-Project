namespace B2P_API.DTOs.RatingDTO
{
    public class RatingDto
    {
        public int RatingId { get; set; }
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public int BookingId { get; set; }
    }
    public class ResponseRatingDto
    {
        public int RatingId { get; set; }
        public int BookingId { get; set; }
        public string? Comment { get; set; }
        public DateTime CreateAt { get; set; }
        public int Stars { get; set; }
    }

    public class CreateRatingDto
    {
        public int BookingId { get; set; }
        public string? Comment { get; set; }
        public int Stars { get; set; }
    }


}
