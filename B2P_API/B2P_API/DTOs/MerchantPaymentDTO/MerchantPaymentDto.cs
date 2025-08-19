namespace B2P_API.DTOs.MerchantPaymentDTO
{
   
        public class MerchantPaymentCreateDto
        {
            public int UserId { get; set; }
            public int PaymentMethodId { get; set; }
            public string PaymentKey { get; set; } = string.Empty;
            public int StatusId { get; set; }
        }


    public class MerchantPaymentUpdateDto
        {
            public string PaymentKey { get; set; } = string.Empty;
            public int StatusId { get; set; }
        }

        public class MerchantPaymentResponseDto
        {
            public int MerchantPaymentId { get; set; }
            public int UserId { get; set; }
            public int PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; } = string.Empty;
        public string PaymentKey { get; set; } = string.Empty;
            public int StatusId { get; set; }
        public string? StatusName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        }
    

}
