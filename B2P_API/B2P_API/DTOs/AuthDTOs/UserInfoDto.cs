namespace B2P_API.DTOs.AuthDTOs
{
    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string? Phone { get; set; }    // Google user chưa chắc có
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool? IsMale { get; set; }
        public DateOnly? Dob { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? CreateAt { get; set; }

        // Flags
        public bool IsGoogleUser { get; set; }
        public string? GoogleId { get; set; }
    }
}
