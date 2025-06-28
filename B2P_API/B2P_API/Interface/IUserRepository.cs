using B2P_API.DTOs.UserDTO;
using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface IUserRepository
    {
        Task<bool> UpdateUserAsync(User user);
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> UpdateAvatar(Image image);
        Task<Image?> GetImageByUserId(int userId);
        Task<User?> CheckPhoneExistedByUserId(int userId, string phone);
        Task<User?>CheckEmailExistedByUserId(int userId, string email);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByPhoneAsync(string phone);

    }
}
