using System.ComponentModel.DataAnnotations;

namespace B2P_API.DTOs.UserDTO
{
    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
        public int UserId { get; set; }
    }
}
