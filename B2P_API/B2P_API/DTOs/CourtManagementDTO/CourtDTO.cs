using B2P_API.Models;

namespace B2P_API.DTOs.CourtManagementDTO
{
    public class CourtDTO
    {
        public int CourtId { get; set; }

        public string? CourtName { get; set; }

        public int? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? StatusName { get; set; }

        public decimal? PricePerHour { get; set; }
    }
}
