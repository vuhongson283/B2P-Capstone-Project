using System.ComponentModel.DataAnnotations;

namespace B2P_API.DTOs.UserDTO
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = null!;
        [Required]
        [MinLength(8,ErrorMessage = "Mật khẩu có ít nhất 8 ký tự.")]
        public string NewPassword { get; set; } = null!;
        [Required]
        public string ConfirmPassword { get; set; } = null!;
        [Required]
        public int UserId { get; set; }
    }
}
