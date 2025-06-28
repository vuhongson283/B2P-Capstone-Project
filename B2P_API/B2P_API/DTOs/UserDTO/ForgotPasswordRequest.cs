using System.ComponentModel.DataAnnotations;
using B2P_API.Utils;

namespace B2P_API.DTOs.UserDTO
{
    public class ForgotPasswordRequestByEmailDto
    {
        public string Email { get; set; }
    }

    public class VerifyOtpDtoByEmail
    {
        public string Email { get; set; }

        public string OtpCode { get; set; }

        public string NewPassword { get; set; }

        public string ConfirmPassword { get; set; }

    }

    public class ResendOtpDtoByEmail
    {
        public string Email { get; set; }
    }


    //SMS DTOs
    public class ForgotPasswordRequestBySmsDto
    {
        public string PhoneNumber { get; set; }
    }

    public class VerifyOtpBySmsDto
    {
        public string PhoneNumber { get; set; }
        public string OtpCode { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class ResendOtpBySmsDto
    {
        public string PhoneNumber { get; set; }
    }

}
