namespace B2P_API.DTOs.CourtManagementDTO
{
    public class CourtRequestDTO
    {
        public int PageNumber {  get; set; }
        public int PageSize { get; set; }
        public int FacilityId {  get; set; }
        public string? Search {  get; set; }
        public int? Status { get; set; }
        public int? CategoryId { get; set; }
    }
}
