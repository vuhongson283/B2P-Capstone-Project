using B2P_API.Models;
using B2P_API.DTOs;

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

    public class FacilityDetailsDto
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Location { get; set; }
        public string Contact { get; set; }

        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }

        public List<FImageDto> Images { get; set; }
        public List<CategoryDto> Categories { get; set; }
        public List<RatingDTO.RatingDto> Ratings { get; set; }
    }

    public class FImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public int? Order { get; set; }
    }
    

public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }


}
