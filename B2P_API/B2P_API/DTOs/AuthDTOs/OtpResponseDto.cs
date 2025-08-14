namespace B2P_API.DTOs.AuthDTOs
{
    public class OtpResponseDto
    {
        public string SessionToken { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string MaskedContact { get; set; } = string.Empty; // SĐT/Email bị che: 098****123 hoặc v***@gmail.com
        public DateTime ExpiresAt { get; set; }
        public int ExpiresInSeconds => (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds;
    }
}
