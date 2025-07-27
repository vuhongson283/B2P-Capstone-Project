namespace B2P_API.DTOs.FacilityDTOs
{
    public class UpdateFacilityRequest
    {
        public string FacilityName { get; set; } = null!;
        public string? Location { get; set; }
        public string? Contact { get; set; }
        public int StatusId { get; set; }
        
    }
}
