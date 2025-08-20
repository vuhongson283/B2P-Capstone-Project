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
    //public class RefreshTokenAsyncTest
    //{
    //    private readonly Mock<IAuthRepository> _authRepoMock = new();
    //    private JWTHelper CreateRealJwtHelper()
    //    {
    //        var inMemorySettings = new Dictionary<string, string?>
    //        {
    //            {"JWT:AccessSecret", "test-key-test-key-test-key-test-key"},
    //            {"JWT:Issuer", "test-issuer"},
    //            {"JWT:Audience", "test-audience"},
    //            {"JWT:DurationInMinutes", "60"},
    //        };
    //        var configuration = new ConfigurationBuilder()
    //            .AddInMemoryCollection(inMemorySettings)
    //            .Build();

    //        return new JWTHelper(configuration);
    //    }

    //    private string GenerateValidRefreshToken(int userId)
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var user = new User
    //        {
    //            UserId = userId,
    //            Phone = "0123456789",
    //            Email = "test@email.com",
    //            FullName = "Test User",
    //            RoleId = 1
    //        };
    //        return jwtHelper.GenerateTokens(user).RefreshToken;
    //    }

    //    [Fact(DisplayName = "UTCID01 - Missing refresh token returns 400")]
    //    public async Task UTCID01_MissingToken_Returns400()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var req = new RefreshTokenRequestDto { RefreshToken = "" };

    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.False(result.Success);
    //        Assert.Equal(400, result.Status);
    //        Assert.Equal("Refresh token is required", result.Message);
    //        Assert.Null(result.Data);
    //    }

    //    [Fact(DisplayName = "UTCID02 - Invalid token format returns 401")]
    //    public async Task UTCID02_InvalidTokenFormat_Returns401()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );

    //        var req = new RefreshTokenRequestDto { RefreshToken = "invalid" };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.False(result.Success);
    //        Assert.Equal(401, result.Status);
    //        Assert.Equal("Invalid refresh token", result.Message);
    //        Assert.Null(result.Data);
    //    }

    //    [Fact(DisplayName = "UTCID03 - Invalid token claims returns 401")]
    //    public async Task UTCID03_InvalidTokenClaims_Returns401()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );

    //        // Tạo refresh token không có claim userId
    //        var handler = new JwtSecurityTokenHandler();
    //        var token = handler.WriteToken(new JwtSecurityToken(
    //            issuer: "test-issuer",
    //            audience: "test-audience",
    //            claims: new[] { new Claim("tokenType", "refresh") },
    //            expires: DateTime.UtcNow.AddMinutes(5),
    //            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
    //                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-test-key-test-key-test-key")),
    //                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256
    //            )
    //        ));

    //        var req = new RefreshTokenRequestDto { RefreshToken = token };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.False(result.Success);
    //        Assert.Equal(401, result.Status);
    //        Assert.Equal("Invalid token claims", result.Message);
    //        Assert.Null(result.Data);
    //    }

    //    [Fact(DisplayName = "UTCID04 - Token not found returns 401")]
    //    public async Task UTCID04_TokenNotFound_Returns401()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var refreshToken = GenerateValidRefreshToken(1);

    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(refreshToken)).ReturnsAsync((UserToken)null);

    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.False(result.Success);
    //        Assert.Equal(401, result.Status);
    //        Assert.Equal("Refresh token not found or revoked", result.Message);
    //        Assert.Null(result.Data);
    //    }

    //    [Fact(DisplayName = "UTCID05 - User not found returns 401 and deletes token")]
    //    public async Task UTCID05_UserNotFound_Returns401AndDeletesToken()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var refreshToken = GenerateValidRefreshToken(2);

    //        var userToken = new UserToken { UserTokenId = 10, RefreshToken = refreshToken };
    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(refreshToken)).ReturnsAsync(userToken);
    //        _authRepoMock.Setup(x => x.GetUserByIdAsync(2)).ReturnsAsync((User)null);
    //        _authRepoMock.Setup(x => x.DeleteUserTokenAsync(userToken.UserTokenId)).ReturnsAsync(true).Verifiable();

    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.False(result.Success);
    //        Assert.Equal(401, result.Status);
    //        Assert.Equal("User not found or account deactivated", result.Message);
    //        Assert.Null(result.Data);
    //        _authRepoMock.Verify(x => x.DeleteUserTokenAsync(userToken.UserTokenId), Times.Once);
    //    }

    //    [Fact(DisplayName = "UTCID06 - User deactivated returns 401 and deletes token")]
    //    public async Task UTCID06_UserDeactivated_Returns401AndDeletesToken()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var refreshToken = GenerateValidRefreshToken(3);

    //        var userToken = new UserToken { UserTokenId = 15, RefreshToken = refreshToken };
    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(refreshToken)).ReturnsAsync(userToken);

    //        var user = new User { UserId = 3, StatusId = 0 };
    //        _authRepoMock.Setup(x => x.GetUserByIdAsync(3)).ReturnsAsync(user);

    //        _authRepoMock.Setup(x => x.DeleteUserTokenAsync(userToken.UserTokenId)).ReturnsAsync(true).Verifiable();

    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.False(result.Success);
    //        Assert.Equal(401, result.Status);
    //        Assert.Equal("User not found or account deactivated", result.Message);
    //        Assert.Null(result.Data);
    //        _authRepoMock.Verify(x => x.DeleteUserTokenAsync(userToken.UserTokenId), Times.Once);
    //    }

    //    [Fact(DisplayName = "UTCID07 - Success")]
    //    public async Task UTCID07_Success_Returns200()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var refreshToken = GenerateValidRefreshToken(4);

    //        var userToken = new UserToken { UserTokenId = 22, RefreshToken = refreshToken };
    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(refreshToken)).ReturnsAsync(userToken);

    //        var user = new User
    //        {
    //            UserId = 4,
    //            StatusId = 1,
    //            Phone = "123456789",
    //            FullName = "Test User",
    //            Email = "test@user.com",
    //            IsMale = true,
    //            Dob = new DateOnly(1990, 1, 1),
    //            Address = "123 Main St",
    //            Role = new Role { RoleName = "Admin" },
    //            CreateAt = new DateTime(2020, 1, 1)
    //        };
    //        _authRepoMock.Setup(x => x.GetUserByIdAsync(4)).ReturnsAsync(user);

    //        _authRepoMock.Setup(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken))).ReturnsAsync(true).Verifiable();

    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.True(result.Success);
    //        Assert.Equal(200, result.Status);
    //        Assert.Equal("Token refreshed successfully", result.Message);
    //        Assert.NotNull(result.Data);
    //        Assert.False(string.IsNullOrEmpty(result.Data.AccessToken));
    //        Assert.False(string.IsNullOrEmpty(result.Data.RefreshToken));
    //        Assert.StartsWith("eyJ", result.Data.AccessToken);
    //        Assert.StartsWith("eyJ", result.Data.RefreshToken);
    //        Assert.Equal(user.Email, result.Data.User.Email);
    //        Assert.False(result.Data.IsNewUser);
    //        Assert.Equal("Admin", result.Data.User.RoleName);
    //        _authRepoMock.Verify(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken)), Times.Once);
    //    }

    //    [Fact(DisplayName = "UTCID08 - Exception returns 500")]
    //    public async Task UTCID08_Exception_Returns500()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );

    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(It.IsAny<string>()))
    //            .Throws(new Exception("error"));

    //        var refreshToken = GenerateValidRefreshToken(5);
    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.False(result.Success);
    //        Assert.Equal(500, result.Status);
    //        Assert.StartsWith("Token refresh failed: error", result.Message);
    //        Assert.Null(result.Data);
    //    }

    //    [Fact(DisplayName = "UTCID09 - Success with user has no Role")]
    //    public async Task UTCID09_Success_UserNoRole_ReturnsUserRoleName()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var refreshToken = GenerateValidRefreshToken(6);

    //        var userToken = new UserToken { UserTokenId = 33, RefreshToken = refreshToken };
    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(refreshToken)).ReturnsAsync(userToken);

    //        var user = new User
    //        {
    //            UserId = 6,
    //            StatusId = 1,
    //            Phone = "1122334455",
    //            FullName = "No Role User",
    //            Email = "norole@user.com",
    //            IsMale = false,
    //            Dob = new DateOnly(1988, 8, 8),
    //            Address = "No Role Address",
    //            Role = null, // Quan trọng: Không có role
    //            CreateAt = new DateTime(2019, 8, 8)
    //        };
    //        _authRepoMock.Setup(x => x.GetUserByIdAsync(6)).ReturnsAsync(user);

    //        _authRepoMock.Setup(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken))).ReturnsAsync(true).Verifiable();

    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.True(result.Success);
    //        Assert.Equal(200, result.Status);
    //        Assert.Equal("Token refreshed successfully", result.Message);
    //        Assert.NotNull(result.Data);
    //        Assert.False(string.IsNullOrEmpty(result.Data.AccessToken));
    //        Assert.False(string.IsNullOrEmpty(result.Data.RefreshToken));
    //        Assert.StartsWith("eyJ", result.Data.AccessToken);
    //        Assert.StartsWith("eyJ", result.Data.RefreshToken);
    //        Assert.Equal(user.Email, result.Data.User.Email);
    //        Assert.False(result.Data.IsNewUser);
    //        // RoleName phải là "User" khi user không có Role
    //        Assert.Equal("User", result.Data.User.RoleName);
    //        _authRepoMock.Verify(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken)), Times.Once);
    //    }

    //    [Fact(DisplayName = "UTCID10 - Success with user RoleName is null returns default 'User'")]
    //    public async Task UTCID10_Success_UserRoleNameNull_ReturnsDefaultUser()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var refreshToken = GenerateValidRefreshToken(7);

    //        var userToken = new UserToken { UserTokenId = 44, RefreshToken = refreshToken };
    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(refreshToken)).ReturnsAsync(userToken);

    //        var user = new User
    //        {
    //            UserId = 7,
    //            StatusId = 1,
    //            Phone = "88887777",
    //            FullName = "Null RoleName User",
    //            Email = "nullrolename@user.com",
    //            IsMale = true,
    //            Dob = new DateOnly(1995, 7, 7),
    //            Address = "Null RoleName Address",
    //            Role = new Role { RoleName = null }, // Role đã có, nhưng RoleName null
    //            CreateAt = new DateTime(2022, 7, 7)
    //        };
    //        _authRepoMock.Setup(x => x.GetUserByIdAsync(7)).ReturnsAsync(user);

    //        _authRepoMock.Setup(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken))).ReturnsAsync(true).Verifiable();

    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.True(result.Success);
    //        Assert.Equal(200, result.Status);
    //        Assert.Equal("Token refreshed successfully", result.Message);
    //        Assert.NotNull(result.Data);
    //        Assert.False(string.IsNullOrEmpty(result.Data.AccessToken));
    //        Assert.False(string.IsNullOrEmpty(result.Data.RefreshToken));
    //        Assert.StartsWith("eyJ", result.Data.AccessToken);
    //        Assert.StartsWith("eyJ", result.Data.RefreshToken);
    //        Assert.Equal(user.Email, result.Data.User.Email);
    //        Assert.False(result.Data.IsNewUser);
    //        // RoleName phải là "User" khi user.Role.RoleName là null
    //        Assert.Equal("User", result.Data.User.RoleName);
    //        _authRepoMock.Verify(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken)), Times.Once);
    //    }

    //    [Fact(DisplayName = "UTCID11 - Success with user fields Phone, FullName, Email null and Role null")]
    //    public async Task UTCID11_Success_UserPhoneFullNameEmailNull_AndRoleNull()
    //    {
    //        var jwtHelper = CreateRealJwtHelper();
    //        var service = new AuthService(
    //            _authRepoMock.Object,
    //            jwtHelper,
    //            null, null, null
    //        );
    //        var refreshToken = GenerateValidRefreshToken(8);

    //        var userToken = new UserToken { UserTokenId = 55, RefreshToken = refreshToken };
    //        _authRepoMock.Setup(x => x.GetUserTokenByRefreshTokenAsync(refreshToken)).ReturnsAsync(userToken);

    //        var user = new User
    //        {
    //            UserId = 8,
    //            StatusId = 1,
    //            Phone = null,          // Test branch Phone ?? ""
    //            FullName = null,       // Test branch FullName ?? ""
    //            Email = null,          // Test branch Email ?? ""
    //            IsMale = false,
    //            Dob = new DateOnly(2000, 1, 1),
    //            Address = "Test address",
    //            Role = null,           // Test branch RoleName ?? "User"
    //            CreateAt = new DateTime(2020, 8, 8)
    //        };
    //        _authRepoMock.Setup(x => x.GetUserByIdAsync(8)).ReturnsAsync(user);

    //        _authRepoMock.Setup(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken))).ReturnsAsync(true).Verifiable();

    //        var req = new RefreshTokenRequestDto { RefreshToken = refreshToken };
    //        var result = await service.RefreshTokenAsync(req);

    //        Assert.True(result.Success);
    //        Assert.Equal(200, result.Status);
    //        Assert.Equal("Token refreshed successfully", result.Message);
    //        Assert.NotNull(result.Data);
    //        Assert.False(string.IsNullOrEmpty(result.Data.AccessToken));
    //        Assert.False(string.IsNullOrEmpty(result.Data.RefreshToken));
    //        // Các trường null phải trả về ""
    //        Assert.Equal("", result.Data.User.Phone);
    //        Assert.Equal("", result.Data.User.FullName);
    //        Assert.Equal("", result.Data.User.Email);
    //        Assert.Equal("User", result.Data.User.RoleName);
    //        Assert.Equal(user.UserId, result.Data.User.UserId);
    //        Assert.Equal(user.Address, result.Data.User.Address);
    //        Assert.Equal(user.IsMale, result.Data.User.IsMale);
    //        Assert.Equal(user.Dob, result.Data.User.Dob);
    //        Assert.Equal(user.CreateAt, result.Data.User.CreateAt);
    //        _authRepoMock.Verify(x => x.UpdateUserTokenAsync(It.Is<UserToken>(ut => ut == userToken)), Times.Once);
    //    }
    //}
}