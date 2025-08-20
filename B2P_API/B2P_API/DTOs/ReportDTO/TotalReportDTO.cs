namespace B2P_API.DTOs.ReportDTO
{
    public class TotalReportDTO
    {
        public int TotalFacility {  get; set; }
        public int TotalBooking { get; set; }
        public int TotalCourt { get; set; }
        public decimal? TotalCost { get; set; }
        public decimal? CommissionPayment { get; set; }
    }
}
