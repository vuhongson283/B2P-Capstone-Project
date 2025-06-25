using B2P_API.Models;

namespace B2P_API.DTOs.FacilityDTO
{
    public class SearchFacilityResponse
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Location { get; set; }
        public string OpenTime { get; set; }
        public string? FirstImage { get; set; }
        public double AverageRating { get; set; }
        public decimal PricePerHour { get; set; }  
        public decimal MinPrice { get; set; }      
        public decimal MaxPrice { get; set; }

    }
}
