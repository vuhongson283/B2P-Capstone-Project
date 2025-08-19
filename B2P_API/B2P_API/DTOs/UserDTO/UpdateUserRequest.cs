using System.ComponentModel.DataAnnotations;
using B2P_API.Models;

namespace B2P_API.DTOs.UserDTO
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = null!;

        public string? Phone { get; set; }

        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateOnly? Dob { get; set; }
        public bool? IsMale { get; set; }

    }
}