namespace B2P_API.DTOs.UserDTO
{
    public class SendSMSRequest
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }
}
