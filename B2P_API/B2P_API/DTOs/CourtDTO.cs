using B2P_API.Models;

namespace B2P_API.DTOs
{
    public class CourtDTO
    {
        public int CourtId { get; set; }

        public string? CourtName { get; set; }

        public string? CategoryName { get; set; }

        public string? StatusName { get; set; }
    }
}
