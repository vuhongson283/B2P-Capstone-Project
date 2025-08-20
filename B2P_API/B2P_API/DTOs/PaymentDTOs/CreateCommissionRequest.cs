namespace B2P_API.DTOs.PaymentDTOs
{
    public class CreateCommissionRequest
    {
        public int UserId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal Amount { get; set; }
    }
}
