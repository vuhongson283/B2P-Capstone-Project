namespace B2P_API.DTOs.AuthDTOs
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn => (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds;
        public UserInfoDto User { get; set; } = new();
        public bool IsNewUser { get; set; } = false; // Để biết có phải user mới không
    }
}
