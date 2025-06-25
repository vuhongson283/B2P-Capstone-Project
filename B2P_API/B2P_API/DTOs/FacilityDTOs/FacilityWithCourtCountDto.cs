namespace B2P_API.DTOs.FacilityDTOs
{
    using B2P_API.DTOs.ImageDTOs;
    using B2P_API.DTOs.StatuDTOs;
    using B2P_API.Models;
    public class FacilityWithCourtCountDto
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public StatusDto Status { get; set; } // ✅ đúng kiểu
        public ICollection<ImageDto> Images { get; set; }
        public int CourtCount { get; set; }
    }
}
