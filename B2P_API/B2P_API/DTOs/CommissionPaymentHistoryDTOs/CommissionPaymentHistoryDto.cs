namespace B2P_API.DTOs.CommissionPaymentHistoryDTOs
{
   
        public class CommissionPaymentHistoryDto
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public decimal Amount { get; set; }
            public DateTime? PaidAt { get; set; }
            public int StatusId { get; set; }
            public string? Note { get; set; }
        }

        public class CommissionPaymentHistoryCreateDto
        {
            public int UserId { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public decimal Amount { get; set; }
            public int StatusId { get; set; }
            public string? Note { get; set; }
        }

        public class CommissionPaymentHistoryUpdateDto
        {
            public int StatusId { get; set; }
            public string? Note { get; set; }
        }
    

}
