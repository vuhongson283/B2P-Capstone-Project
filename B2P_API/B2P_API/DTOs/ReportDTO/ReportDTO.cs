namespace B2P_API.DTOs.ReportDTO
{
    public class ReportDTO
    {
        public int FacilityId {  get; set; }
        public string? FacilityName { get; set; }
        public int CourtId { get; set; }
        public string? CourtName {  get; set; }
        public string? CourtCategory {  get; set; }
        public decimal? PricePerHour { get; set; }
        public int BookingId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public DateTime CheckInDate { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateTime BookingTime { get; set; }
        public string? BookingStatus { get; set; }
        public int PaymentId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaymentTime { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
