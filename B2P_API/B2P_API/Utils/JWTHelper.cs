using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using B2P_API.Models;

namespace B2P_API.Utils
{
    public class JWTHelper
    {
        private readonly IConfiguration _configuration;

        public JWTHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Generate JWT tokens cho user
        public TokenResult GenerateTokens(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user); // ✅ Truyền user vào

            return new TokenResult
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken, // ✅ Giờ là JWT
                ExpiresAt = accessToken.ExpiresAt,
                TokenType = "Bearer"
            };
        }

        // Tạo Access Token (JWT)
        private (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
        {
            var secretKey = _configuration["JWT:AccessSecret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddMinutes(15); // 15 phút

            var claims = new List<Claim>
            {
                new Claim("userId", user.UserId.ToString()),
                new Claim("phone", user.Phone ?? ""),
                new Claim("email", user.Email ?? ""),
                new Claim("fullName", user.FullName ?? ""),
                new Claim("roleId", user.RoleId.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expiresAt);
        }

        // Tạo Refresh Token
        private string GenerateRefreshToken(User user)
        {
            var secretKey = _configuration["JWT:AccessSecret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddDays(7); // 7 ngày

            var claims = new List<Claim>
    {
        new Claim("userId", user.UserId.ToString()),
        new Claim("tokenType", "refresh"), // ✅ Đánh dấu là refresh token
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // Validate JWT token
        public ClaimsPrincipal? ValidateAccessToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JWT:AccessSecret"];
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JWT:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        // Generate OTP 6 số
        public static string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Generate session token cho OTP
        public static string GenerateSessionToken()
        {
            return Guid.NewGuid().ToString("N");
        }
        public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["JWT:AccessSecret"]; // ✅ Dùng như các method khác
                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JWT:Issuer"], // ✅ Dùng _configuration
                    ValidateAudience = true,
                    ValidAudience = _configuration["JWT:Audience"], // ✅ Dùng _configuration
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out _);

                // Kiểm tra token type
                var tokenType = principal.FindFirst("tokenType")?.Value;
                if (tokenType != "refresh")
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }

    // Class để return kết quả
    public class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}