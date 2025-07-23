namespace B2P_API.DTOs.BankTypeDTOs
{
    public class BankTypeResponse
    {
        public int BankTypeId { get; set; }

        public string BankName { get; set; } = null!;

        public string? Description { get; set; }
    }
}
