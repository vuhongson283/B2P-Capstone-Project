using System.ComponentModel.DataAnnotations;
using B2P_API.Models;

namespace B2P_API.DTOs.UserDTO
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? Dob { get; set; }
        public string? AccountNumber { get; set; } = null!;
        public string? AccountHolder { get; set; }
        public int? BankTypeId { get; set; }
        public bool? IsMale { get; set; }

    }
}