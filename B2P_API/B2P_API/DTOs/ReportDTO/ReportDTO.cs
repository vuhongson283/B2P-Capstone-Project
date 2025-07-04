namespace B2P_API.DTOs.ReportDTO
{
    public class ReportDTO
    {
        public int CourtCount { get; set; }
        public string? CourtCategories { get; set; }
        public int BookingId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public int TimeSlotCount { get; set; }
        public string? CheckInDate { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateTime BookingTime { get; set; }
        public string? BookingStatus { get; set; }
        public int PaymentId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaymentTime { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
