namespace B2P_API.DTOs.AuthDTOs
{
    public class VerifyOtpRequestDto
    {
        public string PhoneOrEmail { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
    }
}
