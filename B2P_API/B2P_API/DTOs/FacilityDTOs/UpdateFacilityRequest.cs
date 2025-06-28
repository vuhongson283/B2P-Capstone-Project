namespace B2P_API.DTOs.FacilityDTOs
{
    public class UpdateFacilityRequest
    {
        public string FacilityName { get; set; } = null!;
        public string? Location { get; set; }
        public string? Contact { get; set; }
        public int StatusId { get; set; }
        public int OpenHour { get; set; }
        public int CloseHour { get; set; }
        public int SlotDuration { get; set; }
    }
}
