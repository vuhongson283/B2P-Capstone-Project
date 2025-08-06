using System.Text.Json.Serialization;

namespace B2P_API.DTOs.AuthDTOs
{
    public class LogoutRequestDto
    {
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("logout_type")]
        public string? LogoutType { get; set; } = "single"; // "single" hoặc "all"
    }
}
