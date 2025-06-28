namespace B2P_API.DTOs
{
    public class UpdateCourtRequest
    {
        public int CourtId {  get; set; }

        public int? StatusId { get; set; }

        public string? CourtName { get; set; }

        public int? CategoryId { get; set; }

        public decimal? PricePerHour { get; set; }
    }
}
