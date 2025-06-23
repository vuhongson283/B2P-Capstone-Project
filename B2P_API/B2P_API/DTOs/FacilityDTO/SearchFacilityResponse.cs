using B2P_API.Models;

namespace B2P_API.DTOs.FacilityDTO
{
    public class SearchFacilityResponse
    {
        public int FacilityId { get; set; }

        public int? UserId { get; set; }

        public int StatusId { get; set; }

        public string? Location { get; set; }

        public string FacilityName { get; set; } = null!;

        public string OpenTime { get; set; }

    }
}
