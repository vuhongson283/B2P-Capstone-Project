using System.Text.Json.Serialization;

namespace B2P_API.DTOs.AuthDTOs
{
    public class SendOtpResponseDto
    {   
        public string SessionToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public string ContactInfo { get; set; } = string.Empty; // Email hoặc phone đã được mask

        public string OtpType { get; set; } = string.Empty; // "email" hoặc "sms"

        public DateTime? ResendAvailableAt { get; set; }

        public int AttemptsRemaining { get; set; } = 3;
    }
}
