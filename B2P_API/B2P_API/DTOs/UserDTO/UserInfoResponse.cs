using B2P_API.Models;

namespace B2P_API.DTOs.UserDTO;

public class UserInfoResponse
{
    public string FullName { get; set; } = null!;
    public string? Email { get; set; } = null!;
    public string? Phone { get; set; } = null!;
    public bool? IsMale { get; set; }
    public string? Address { get; set; }
    public DateOnly? Dob { get; set; }
    public DateTime? CreateAt { get; set; }
    public string? StatusDescription { get; set; }
    public int? ImageId { get; set; }
    public string? ImageUrl { get; set; } = null!;

}