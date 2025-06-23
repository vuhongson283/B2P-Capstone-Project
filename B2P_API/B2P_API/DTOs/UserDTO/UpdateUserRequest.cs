using System.ComponentModel.DataAnnotations;
using B2P_API.Models;

namespace B2P_API.DTOs.UserDTO
{
    public class UpdateUserRequest
    {
        [Required]
        [MaxLength(50, ErrorMessage = "Tên người dùng không được vượt quá 50 ký tự.")]
        public string Username { get; set; } = null!;
        [Required]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        public string Email { get; set; } = null!;
        [Required]
        [RegularExpression(@"^0[3-9]\d{8}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu từ 0 và chữ số thứ 2 từ 3-9")]
        public string Phone { get; set; } = null!;
        [Required]
        [MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
        public string? Address { get; set; }
        [Required]
        public DateOnly? Dob { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool? IsMale { get; set; }

    }
}