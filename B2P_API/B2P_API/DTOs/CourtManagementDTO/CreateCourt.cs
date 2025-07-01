using B2P_API.Models;

namespace B2P_API.DTOs.CourtManagementDTO
{
    public class CreateCourt
    {
        public int? FacilityId { get; set; }

        public string? CourtName { get; set; }

        public int? CategoryId { get; set; }

        public decimal? PricePerHour { get; set; }

    }
}
