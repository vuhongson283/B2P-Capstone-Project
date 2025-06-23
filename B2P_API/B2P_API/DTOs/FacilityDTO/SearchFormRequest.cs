namespace B2P_API.DTOs.FacilityDTO
{
    public class SearchFormRequest
    {
        public string? Name { get; set; }
        public List<int>? Type { get; set; }
        public string? City { get; set; }
        public string? Ward { get; set; }
        //public int Order { get; set; } = 1; // Xếp theo giá( 1: Thấp-> Cao, 2: Cao -> Thấp )
    }
}
