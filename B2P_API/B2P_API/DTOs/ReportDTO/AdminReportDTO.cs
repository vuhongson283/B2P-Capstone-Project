using B2P_API.DTOs.BookingDTOs;

namespace B2P_API.DTOs.ReportDTO
{
    public class AdminReportDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public MonthlyStatsDTO MonthlyStats { get; set; }
        public List<FacilityStatDTO> TopFacilities { get; set; }
        public List<CourtCategoryStatDTO> PopularCourtCategories { get; set; }
    }
    public class MonthlyStatsDTO
    {
        public int TotalBooking { get; set; }
        public decimal? TotalRevenue { get; set; }
        public decimal? AverageRevenuePerBooking { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int TotalFacilities { get; set; }
        public int TotalCourts { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class FacilityStatDTO
    {
        public int FacilityId { get; set; }
        public string? FacilityName { get; set; }
        public int TotalBooking { get; set; }
        public decimal? TotalRevenue { get; set; }
    }

    public class CourtCategoryStatDTO
    {
        public string CategoryName { get; set; }
        public int TotalBooking { get; set; }
    }
}
