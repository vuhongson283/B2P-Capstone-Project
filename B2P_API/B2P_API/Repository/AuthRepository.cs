using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Utils;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace B2P_API.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly SportBookingDbContext _context;
        private readonly JWTHelper _jwtHelper;
        private static readonly Dictionary<string, DateTime> _tokenExpiry = new();
        private static readonly Dictionary<string, bool> _blacklistedTokens = new();

        public AuthRepository(SportBookingDbContext context, JWTHelper jWTHelper)
        {
            _context = context;
            _jwtHelper = jWTHelper;
        }

        public async Task<int> CreateUserAsync(User user)
        {
            user.CreateAt = DateTime.UtcNow;
            user.StatusId = 1; // Active
            user.RoleId = 1;   // User role

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user.UserId;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                 .Include(u => u.Role)
                 .Include(u => u.Status)
                 .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            return await _context.Users
                 .Include(u => u.Role)
                 .Include(u => u.Status)
                 .FirstOrDefaultAsync(u => u.Phone == phone);
        }

        public async Task<UserToken?> GetUserTokenByAccessTokenAsync(string accessToken)
        {
            var userToken = await _context.UserTokens
                .Include(ut => ut.User)
                .ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(ut => ut.AccessToken == accessToken);

            if (userToken != null)
            {
                // Check nếu access token hết hạn
                var expiryKey = $"access_{accessToken}";
                if (_tokenExpiry.ContainsKey(expiryKey))
                {
                    if (_tokenExpiry[expiryKey] < DateTime.UtcNow)
                    {
                        return null; // Token hết hạn
                    }
                }

                // Check nếu token bị blacklist
                if (_blacklistedTokens.ContainsKey(accessToken) && _blacklistedTokens[accessToken])
                {
                    return null;
                }
            }

            return userToken;
        }

        public async Task<UserToken?> GetUserTokenByRefreshTokenAsync(string refreshToken)
        {
            var userToken = await _context.UserTokens
                .Include(ut => ut.User)
                .ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(ut => ut.RefreshToken == refreshToken);

            if (userToken != null)
            {
                // Check nếu refresh token hết hạn
                var expiryKey = $"refresh_{refreshToken}";
                if (_tokenExpiry.ContainsKey(expiryKey))
                {
                    if (_tokenExpiry[expiryKey] < DateTime.UtcNow)
                    {
                        return null; // Token hết hạn
                    }
                }

                // Check nếu token bị blacklist
                if (_blacklistedTokens.ContainsKey(refreshToken) && _blacklistedTokens[refreshToken])
                {
                    return null;
                }
            }

            return userToken;
        }

        public async Task<bool> IsTokenValidAsync(string accessToken)
        {
            // Check blacklist
            if (_blacklistedTokens.ContainsKey(accessToken) && _blacklistedTokens[accessToken])
            {
                return false;
            }

            // Check database
            var exists = await _context.UserTokens
                .AnyAsync(ut => ut.AccessToken == accessToken);

            if (!exists)
            {
                return false;
            }

            // Check expiry
            var expiryKey = $"access_{accessToken}";
            if (_tokenExpiry.ContainsKey(expiryKey))
            {
                return _tokenExpiry[expiryKey] > DateTime.UtcNow;
            }

            return true;
        }

        public async Task<UserToken?> RefreshTokenAsync(string refreshToken)
        {
            var userToken = await GetUserTokenByRefreshTokenAsync(refreshToken);
            if (userToken?.User == null)
            {
                return null;
            }

            // ⭐ Blacklist token cũ TRƯỚC khi xóa
            if (userToken.AccessToken != null)
                _blacklistedTokens[userToken.AccessToken] = true;
            _blacklistedTokens[refreshToken] = true;

            _context.UserTokens.Remove(userToken);

            // Dùng JWTHelper để tạo token mới
            var newTokens = _jwtHelper.GenerateTokens(userToken.User);

            var newUserToken = new UserToken
            {
                UserId = userToken.UserId,
                AccessToken = newTokens.AccessToken,
                RefreshToken = newTokens.RefreshToken
            };

            _context.UserTokens.Add(newUserToken);
            await _context.SaveChangesAsync();

            // Cập nhật expiry
            _tokenExpiry[$"access_{newTokens.AccessToken}"] = newTokens.ExpiresAt;
            _tokenExpiry[$"refresh_{newTokens.RefreshToken}"] = DateTime.UtcNow.AddDays(7);

            return newUserToken;
        }

        public async Task RevokeAllUserTokensAsync(int userId)
        {
            var userTokens = await _context.UserTokens
                .Where(ut => ut.UserId == userId)
                .ToListAsync();

            foreach (var token in userTokens)
            {
                // Thêm vào blacklist
                if (token.AccessToken != null)
                    _blacklistedTokens[token.AccessToken] = true;

                if (token.RefreshToken != null)
                    _blacklistedTokens[token.RefreshToken] = true;

                // Xóa expiry
                _tokenExpiry.Remove($"access_{token.AccessToken}");
                _tokenExpiry.Remove($"refresh_{token.RefreshToken}");
            }

            // Xóa khỏi database
            _context.UserTokens.RemoveRange(userTokens);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeUserTokenAsync(string accessToken)
        {
            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(ut => ut.AccessToken == accessToken);

            if (userToken != null)
            {
                // Xóa khỏi database
                _context.UserTokens.Remove(userToken);
                await _context.SaveChangesAsync();

                // Thêm vào blacklist
                if (userToken.AccessToken != null)
                    _blacklistedTokens[userToken.AccessToken] = true;

                if (userToken.RefreshToken != null)
                    _blacklistedTokens[userToken.RefreshToken] = true;

                // Xóa expiry
                _tokenExpiry.Remove($"access_{userToken.AccessToken}");
                _tokenExpiry.Remove($"refresh_{userToken.RefreshToken}");
            }
        }

        public async Task SaveUserTokenAsync(UserToken userToken)
        {
            var oldTokens = await _context.UserTokens
                .Where(ut => ut.UserId == userToken.UserId)
                .ToListAsync();

            if (oldTokens.Any())
            {
                _context.UserTokens.RemoveRange(oldTokens);
            }

            _context.UserTokens.Add(userToken);
            await _context.SaveChangesAsync();

            // Lấy expiry từ JWT token thực tế
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15); // Default fallback
            try
            {
                var principal = _jwtHelper.ValidateAccessToken(userToken.AccessToken!);
                var expClaim = principal?.Claims.FirstOrDefault(c => c.Type == "exp");
                if (expClaim != null && long.TryParse(expClaim.Value, out long exp))
                {
                    accessTokenExpiry = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                }
            }
            catch { /* Use default */ }

            if (userToken.AccessToken != null)
                _tokenExpiry[$"access_{userToken.AccessToken}"] = accessTokenExpiry;

            if (userToken.RefreshToken != null)
                _tokenExpiry[$"refresh_{userToken.RefreshToken}"] = DateTime.UtcNow.AddDays(7);
        }

        public async Task UpdateUserVerificationAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.StatusId = 1; // Verified/Active
                await _context.SaveChangesAsync();
            }
        }

       

        // Method để clean up expired tokens (có thể gọi định kỳ)
        public static void CleanupExpiredTokens()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _tokenExpiry
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _tokenExpiry.Remove(key);
            }

            // Clean up blacklisted tokens that are expired
            // (Trong thực tế bạn có thể cần lưu expiry date cho blacklisted tokens)
        }

        public async Task<bool> UpdateUserTokenAsync(UserToken userToken)
        {
            try
            {
                _context.UserTokens.Update(userToken);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserTokenAsync(int userTokenId)
        {
            try
            {
                var userToken = await _context.UserTokens.FindAsync(userTokenId);
                if (userToken != null)
                {
                    // Thêm vào blacklist trước khi xóa
                    if (userToken.AccessToken != null)
                        _blacklistedTokens[userToken.AccessToken] = true;

                    if (userToken.RefreshToken != null)
                        _blacklistedTokens[userToken.RefreshToken] = true;

                    // Xóa expiry
                    _tokenExpiry.Remove($"access_{userToken.AccessToken}");
                    _tokenExpiry.Remove($"refresh_{userToken.RefreshToken}");

                    // Xóa khỏi database
                    _context.UserTokens.Remove(userToken);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<User> GetUserByEmailOrPhoneAsync(string emailOrPhone)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.Phone == emailOrPhone);

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting user by email/phone: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VerifyUserPasswordAsync(int userId, string password)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.Password))
                {
                    return false;
                }

                // VERIFY PASSWORD HASH
                return BCrypt.Net.BCrypt.Verify(password, user.Password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error verifying password: {ex.Message}");
                return false;
            }
        }
    }
}