namespace B2P_API.DTOs.FacilityDTO
{
    public class SearchFormRequest
    {
        public string? Name { get; set; }
        public List<int>? Type { get; set; }
        public string? City { get; set; }
        public string? Ward { get; set; }

        // Xếp theo tiêu chí( 1:Giá thấp-> Cao, 2:Giá cao -> Thấp; 3: Số sao cao -> thấp  )
        public int Order { get; set; } = 1; 
    }
}
