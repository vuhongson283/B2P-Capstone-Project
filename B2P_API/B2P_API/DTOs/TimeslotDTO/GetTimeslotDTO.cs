using B2P_API.DTOs.StatuDTOs;

namespace B2P_API.DTOs.TimeslotDTO
{
    public class GetTimeslotDTO
    {
        public int TimeSlotId { get; set; }
        public int? FacilityId { get; set; }
        public int StatusId { get; set; }
        public string? StartTime { get; set; } // Convert từ TimeOnly sang string
        public string? EndTime { get; set; }   // Convert từ TimeOnly sang string
        public decimal? Discount { get; set; }
        public StatusDto Status { get; set; }
    }
}
