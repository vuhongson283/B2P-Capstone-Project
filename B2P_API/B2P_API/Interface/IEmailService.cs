namespace B2P_API.Interface
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, string otpCode);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
