using B2P_API.Models;

namespace B2P_API.Interface
{
    public interface IAuthRepository
    {
        // User operations
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByPhoneAsync(string phone);
        Task<User?> GetUserByIdAsync(int userId);
        Task<int> CreateUserAsync(User user);
        Task UpdateUserVerificationAsync(int userId);

        // JWT Token operations
        Task SaveUserTokenAsync(UserToken userToken);
        Task<UserToken?> GetUserTokenByRefreshTokenAsync(string refreshToken);
        Task<UserToken?> GetUserTokenByAccessTokenAsync(string accessToken);
        Task<bool> IsTokenValidAsync(string accessToken);
        Task RevokeUserTokenAsync(string accessToken);
        Task RevokeAllUserTokensAsync(int userId);
        Task<UserToken?> RefreshTokenAsync(string refreshToken);
        Task<bool> UpdateUserTokenAsync(UserToken userToken);
        Task<bool> DeleteUserTokenAsync(int userTokenId);
    }
}
