using System.ComponentModel.DataAnnotations;
using B2P_API.Utils;

namespace B2P_API.DTOs.UserDTO
{
    public class ForgotPasswordRequestByEmailDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        public string Email { get; set; }
    }

    public class VerifyOtpDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        public string Email { get; set; }
        [Required]
        public string OtpCode { get; set; }
        [Required]
        [MinLength(8, ErrorMessage = MessagesCodes.MSG_13)]
        public string NewPassword { get; set; }
        [Required]
        public string ConfirmPassword { get; set; }

    }

    public class ResendOtpDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        public string Email { get; set; }
    }
}
