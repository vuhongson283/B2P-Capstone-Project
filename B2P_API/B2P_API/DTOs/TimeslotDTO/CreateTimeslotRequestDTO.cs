namespace B2P_API.DTOs.TimeslotDTO
{
    public class CreateTimeslotRequestDTO
    {
        public int FacilityId { get; set; }
        public int StatusId { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public decimal? Discount { get; set; }
    }
}
