namespace B2P_API.DTOs.TimeslotDTO
{
    public class UpdateTimeslotDTO
    {
        public int StatusId { get; set; }
        public TimeOnly ?StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public decimal ? Discount { get; set; }
    }
}
