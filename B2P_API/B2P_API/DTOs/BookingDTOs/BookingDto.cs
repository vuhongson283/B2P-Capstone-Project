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

    public class BookingQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "checkindate";
        public string? SortDirection { get; set; } = "desc";
        public int? StatusId { get; set; } // Thêm lọc theo trạng thái
        public int? FacilityId { get; set; }
    }

    public class BookingResponseDto
    {
        public int? UserId { get; set; }
        public string? Phone { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public DateTime CheckInDate { get; set; }
		public DateTime CreateDate { get; set; }
		public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<BookingSlotDto> Slots { get; set; } = new();
        public List<DTOs.RatingDTO.RatingDto> Ratings { get; set; } = new();
    }

    public class BookingSlotDto
    {
        public int CourtId { get; set; }
        public int TimeSlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }



}
