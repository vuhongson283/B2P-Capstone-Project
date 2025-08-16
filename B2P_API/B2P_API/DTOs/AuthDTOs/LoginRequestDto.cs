using System.ComponentModel.DataAnnotations;

namespace B2P_API.DTOs.AuthDTOs
{
    public class LoginRequestDto
    {
        public string PhoneOrEmail { get; set; }
        public string Password { get; set; }
    }
}
