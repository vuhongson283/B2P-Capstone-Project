namespace B2P_API.DTOs.BookingDTOs
{
    public class BookingDto
    {
    }
    public class BookingRequestDto
    {
        public int? UserId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        public DateTime CheckInDate { get; set; }
        public List<int> TimeSlotIds { get; set; }
        public int FacilityId { get; set; }
        public int CategoryId { get; set; }
    }
}
