using B2P_API.DTOs.AuthDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.AuthService_UnitTest
{
    public class LogoutAsyncTest
    {
        private readonly Mock<IAuthRepository> _authRepoMock = new();
        private readonly Mock<IImageRepository> _imageRepoMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<ISMSService> _smsServiceMock = new();
        private readonly Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache> _cacheMock = new();

        private JWTHelper CreateRealJwtHelper()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"JWT:AccessSecret", "test-key-test-key-test-key-test-key"},
                {"JWT:Issuer", "test-issuer"},
                {"JWT:Audience", "test-audience"},
                {"JWT:DurationInMinutes", "60"},
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            return new JWTHelper(configuration);
        }

        private string GenerateValidAccessToken(int userId)
        {
            var jwtHelper = CreateRealJwtHelper();
            var user = new User
            {
                UserId = userId,
                Phone = "0123456789",
                Email = "test@email.com",
                FullName = "Test User",
                RoleId = 1
            };
            return jwtHelper.GenerateTokens(user).AccessToken;
        }

        // Helper for refresh token (needed for UTCID09)
        private string GenerateValidRefreshToken(int userId)
        {
            var jwtHelper = CreateRealJwtHelper();
            var user = new User
            {
                UserId = userId,
                Phone = "0123456789",
                Email = "test@email.com",
                FullName = "Test User",
                RoleId = 1
            };
            return jwtHelper.GenerateTokens(user).RefreshToken;
        }

        [Fact(DisplayName = "UTCID01 - Missing access token returns 400")]
        public async Task UTCID01_MissingAccessToken_Returns400()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var req = new LogoutRequestDto { AccessToken = "" };

            var result = await service.LogoutAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Access token is required", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Invalid access token returns 401")]
        public async Task UTCID02_InvalidAccessToken_Returns401()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var req = new LogoutRequestDto { AccessToken = "invalid" };

            var result = await service.LogoutAsync(req);

            Assert.False(result.Success);
            Assert.Equal(401, result.Status);
            Assert.Equal("Invalid access token", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Invalid token claims returns 401")]
        public async Task UTCID03_InvalidTokenClaims_Returns401()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );

            // Tạo access token không có claim userId
            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(new JwtSecurityToken(
                issuer: "test-issuer",
                audience: "test-audience",
                claims: new List<Claim> { }, // Không có userId
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-test-key-test-key-test-key")),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256
                )
            ));

            var req = new LogoutRequestDto { AccessToken = token };
            var result = await service.LogoutAsync(req);

            Assert.False(result.Success);
            Assert.Equal(401, result.Status);
            Assert.Equal("Invalid token claims", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Token not found returns 401")]
        public async Task UTCID04_TokenNotFound_Returns401()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var accessToken = GenerateValidAccessToken(1);

            _authRepoMock.Setup(x => x.GetUserTokenByAccessTokenAsync(accessToken)).ReturnsAsync((UserToken)null);

            var req = new LogoutRequestDto { AccessToken = accessToken };
            var result = await service.LogoutAsync(req);

            Assert.False(result.Success);
            Assert.Equal(401, result.Status);
            Assert.Equal("Token not found or already revoked", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Single logout success")]
        public async Task UTCID05_SingleLogout_Success()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var accessToken = GenerateValidAccessToken(2);

            var user = new User
            {
                UserId = 2,
                Email = "test@email.com",
                Phone = "0123456789"
            };
            var userToken = new UserToken
            {
                UserTokenId = 10,
                AccessToken = accessToken,
                User = user
            };

            _authRepoMock.Setup(x => x.GetUserTokenByAccessTokenAsync(accessToken)).ReturnsAsync(userToken);
            _authRepoMock.Setup(x => x.DeleteUserTokenAsync(userToken.UserTokenId)).ReturnsAsync(true).Verifiable();

            var req = new LogoutRequestDto { AccessToken = accessToken, LogoutType = "single" };
            var result = await service.LogoutAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Logged out successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("single", result.Data.GetType().GetProperty("LogoutType")?.GetValue(result.Data));
            Assert.Equal(2, result.Data.GetType().GetProperty("UserId")?.GetValue(result.Data));
            _authRepoMock.Verify(x => x.DeleteUserTokenAsync(userToken.UserTokenId), Times.Once);
        }

        [Fact(DisplayName = "UTCID06 - All devices logout success")]
        public async Task UTCID06_AllDevicesLogout_Success()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var accessToken = GenerateValidAccessToken(3);

            var user = new User
            {
                UserId = 3,
                Email = "test@email.com",
                Phone = "0123456789"
            };
            var userToken = new UserToken
            {
                UserTokenId = 15,
                AccessToken = accessToken,
                User = user
            };

            _authRepoMock.Setup(x => x.GetUserTokenByAccessTokenAsync(accessToken)).ReturnsAsync(userToken);
            _authRepoMock.Setup(x => x.RevokeAllUserTokensAsync(3)).Returns(Task.CompletedTask).Verifiable();

            var req = new LogoutRequestDto { AccessToken = accessToken, LogoutType = "all" };
            var result = await service.LogoutAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Logged out from all devices successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("all", result.Data.GetType().GetProperty("LogoutType")?.GetValue(result.Data));
            Assert.Equal(3, result.Data.GetType().GetProperty("UserId")?.GetValue(result.Data));
            _authRepoMock.Verify(x => x.RevokeAllUserTokensAsync(3), Times.Once);
        }

        [Fact(DisplayName = "UTCID07 - Logout failed returns 500")]
        public async Task UTCID07_LogoutFailed_Returns500()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var accessToken = GenerateValidAccessToken(4);

            var user = new User
            {
                UserId = 4,
                Email = "test@email.com",
                Phone = "0123456789"
            };
            var userToken = new UserToken
            {
                UserTokenId = 22,
                AccessToken = accessToken,
                User = user
            };

            _authRepoMock.Setup(x => x.GetUserTokenByAccessTokenAsync(accessToken)).ReturnsAsync(userToken);
            _authRepoMock.Setup(x => x.DeleteUserTokenAsync(userToken.UserTokenId)).ReturnsAsync(false).Verifiable();

            var req = new LogoutRequestDto { AccessToken = accessToken, LogoutType = "single" };
            var result = await service.LogoutAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Logout failed", result.Message);
            Assert.Null(result.Data);
            _authRepoMock.Verify(x => x.DeleteUserTokenAsync(userToken.UserTokenId), Times.Once);
        }

        [Fact(DisplayName = "UTCID08 - Exception returns 500")]
        public async Task UTCID08_Exception_Returns500()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var accessToken = GenerateValidAccessToken(5);

            _authRepoMock.Setup(x => x.GetUserTokenByAccessTokenAsync(accessToken))
                .Throws(new Exception("error"));

            var req = new LogoutRequestDto { AccessToken = accessToken };
            var result = await service.LogoutAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.StartsWith("Logout failed: error", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID09 - Unknown logout_type defaults to single logout")]
        public async Task UTCID09_UnknownLogoutType_DefaultsToSingleLogout()
        {
            var jwtHelper = CreateRealJwtHelper();
            var service = new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
            var accessToken = GenerateValidAccessToken(7);

            var user = new User
            {
                UserId = 7,
                Email = "test@email.com",
                Phone = "0123456789"
            };
            var userToken = new UserToken
            {
                UserTokenId = 77,
                AccessToken = accessToken,
                User = user
            };

            _authRepoMock.Setup(x => x.GetUserTokenByAccessTokenAsync(accessToken)).ReturnsAsync(userToken);
            _authRepoMock.Setup(x => x.DeleteUserTokenAsync(userToken.UserTokenId)).ReturnsAsync(true).Verifiable();

            // LogoutType là giá trị lạ
            var req = new LogoutRequestDto { AccessToken = accessToken, LogoutType = "unexpected" };
            var result = await service.LogoutAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Logged out successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("unexpected", result.Data.GetType().GetProperty("LogoutType")?.GetValue(result.Data));
            Assert.Equal(7, result.Data.GetType().GetProperty("UserId")?.GetValue(result.Data));
            _authRepoMock.Verify(x => x.DeleteUserTokenAsync(userToken.UserTokenId), Times.Once);
        }
    }
}