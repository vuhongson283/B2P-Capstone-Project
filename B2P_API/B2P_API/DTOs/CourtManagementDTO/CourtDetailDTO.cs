using B2P_API.Models;

namespace B2P_API.DTOs.CourtManagementDTO
{
    public class CourtDetailDTO
    {
        public int CourtId { get; set; }

        public string? CourtName { get; set; }

        public decimal? PricePerHour { get; set; }

        public int? StatusId { get; set; }

        public string? StatusName { get; set; }

        public string? StatusDescription { get; set; }

        public int? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public int? FacilityId { get; set; }

        public string? FacilityName { get; set; }

        public string? Location { get; set; }

        public string? Contact { get; set; }
    }
}
