using B2P_API.DTOs.AuthDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.AuthService_UnitTest
{
    public class VerifyOtpAndLoginAsyncTest
    {
        private readonly Mock<IAuthRepository> _authRepoMock = new();
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly Mock<IConfiguration> _configMock = new();

        private JWTHelper CreateJwtHelper()
        {
            return new JWTHelper(_configMock.Object);
        }

        private AuthService CreateService()
        {
            return new AuthService(
                _authRepoMock.Object,
                CreateJwtHelper(),
                _cacheMock.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<ISMSService>()
            );
        }

        [Fact(DisplayName = "UTCID01 - OTP expired or invalid session")]
        public async Task UTCID01_OtpExpiredOrInvalidSession_Returns400()
        {
            // Arrange
            var service = CreateService();
            object? cacheObj = null;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(false);

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "user@email.com",
                SessionToken = "token",
                Otp = "123456"
            };

            // Act
            var result = await service.VerifyOtpAndLoginAsync(req);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("OTP expired or invalid session", result.Message);
            Assert.Null(result.Data);
        }


        [Fact(DisplayName = "UTCID02 - Invalid OTP code")]
        public async Task UTCID02_InvalidOtpCode_Returns400()
        {
            // Arrange
            var service = CreateService();
            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.Code = "654321";
            otpData.IsEmail = true;

            object? cacheObj = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(true);

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "user@email.com",
                SessionToken = "token",
                Otp = "123456"
            };

            // Act
            var result = await service.VerifyOtpAndLoginAsync(req);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Invalid OTP code", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Login for existing email user")]
        public async Task UTCID03_LoginExistingEmailUser_Returns200()
        {
            // Arrange
            // Cấu hình giả cho JWTHelper
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"JWT:AccessSecret", "test-key-test-key-test-key-test-key"}, // Đúng key cho JWTHelper
                {"JWT:Issuer", "test-issuer"},
                {"JWT:Audience", "test-audience"},
                {"JWT:DurationInMinutes", "60"},
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var jwtHelper = new JWTHelper(configuration);

            // Service dùng JWTHelper thật
            var service = new AuthService(
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<ISMSService>()
            );

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.Code = "123456";
            otpData.IsEmail = true;
            object? cacheObj = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(true);

            var user = new User
            {
                UserId = 1,
                Email = "user@email.com",
                FullName = "Test User",
                Phone = "",
                Role = new Role { RoleName = "User" },
                StatusId = 1,
                RoleId = 1,
                CreateAt = DateTime.UtcNow,
            };
            _authRepoMock.Setup(x => x.GetUserByEmailAsync("user@email.com")).ReturnsAsync(user);

            _authRepoMock.Setup(x => x.SaveUserTokenAsync(It.IsAny<UserToken>())).Returns(Task.CompletedTask);

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "user@email.com",
                SessionToken = "token",
                Otp = "123456"
            };

            // Act
            var result = await service.VerifyOtpAndLoginAsync(req);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Login successful", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(user.Email, result.Data.User.Email);
            Assert.False(result.Data.IsNewUser);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        }

        [Fact(DisplayName = "UTCID04 - Auto register new phone user")]
        public async Task UTCID04_AutoRegisterNewPhoneUser_Returns200()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
    {
        // Đảm bảo đủ mọi key/issuer cấu hình mà JWTHelper có thể lấy!
        {"JWT:AccessSecret", "test-key-test-key-test-key-test-key"},
        {"JWT:Issuer", "test-issuer"},
        {"JWT:Audience", "test-audience"},
        {"JWT:DurationInMinutes", "60"},
    };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var jwtHelper = new JWTHelper(configuration);

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.Code = "123456";
            otpData.IsEmail = false;
            object cacheObj = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(true);

            // User không tồn tại lần đầu, sau đó trả về user mới
            var newUser = new User
            {
                UserId = 2,
                Phone = "0912345678",
                FullName = "User 5678",
                Email = "0912345678@b2p.temp",
                StatusId = 1,
                RoleId = 1,
                CreateAt = DateTime.UtcNow,
                Role = new Role { RoleName = "Player" }
            };
            _authRepoMock.SetupSequence(x => x.GetUserByPhoneAsync("0912345678"))
                .ReturnsAsync((User?)null) // lần đầu: chưa có user
                .ReturnsAsync(newUser);    // lần sau: đã tạo user

            _authRepoMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(2);
            _authRepoMock.Setup(x => x.SaveUserTokenAsync(It.IsAny<UserToken>())).Returns(Task.CompletedTask);

            var service2 = new AuthService(
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<ISMSService>()
            );

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "0912345678",
                SessionToken = "token",
                Otp = "123456"
            };

            // Act
            var result = await service2.VerifyOtpAndLoginAsync(req);

            // Debug nếu fail
            if (!result.Success)
                Console.WriteLine($"Status: {result.Status}, Message: {result.Message}");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Account created and login successful", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(newUser.Phone, result.Data.User.Phone);
            Assert.True(result.Data.IsNewUser);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        }

        [Fact(DisplayName = "UTCID05 - Failed to create or retrieve user returns 500")]
        public async Task UTCID05_FailedToCreateOrRetrieveUser_Returns500()
        {
            // Arrange
            var service = CreateService();
            // Use ExpandoObject for dynamic to avoid runtime binder issues
            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.Code = "123456";
            otpData.IsEmail = false;
            object? cacheObj = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(true);

            _authRepoMock.Setup(x => x.GetUserByPhoneAsync("0912345678")).ReturnsAsync((User?)null);
            _authRepoMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(1);
            // After creation, still return null
            _authRepoMock.Setup(x => x.GetUserByPhoneAsync("0912345678")).ReturnsAsync((User?)null);

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "0912345678",
                SessionToken = "token",
                Otp = "123456"
            };

            var result = await service.VerifyOtpAndLoginAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Failed to create or retrieve user", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Exception returns 500")]
        public async Task UTCID06_Exception_Returns500()
        {
            // Arrange
            var service = CreateService();
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny)).Throws(new Exception("cache error"));

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "user@email.com",
                SessionToken = "token",
                Otp = "123456"
            };

            var result = await service.VerifyOtpAndLoginAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.StartsWith("Login failed: cache error", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID07 - Auto register new email user")]
        public async Task UTCID07_AutoRegisterNewEmailUser_Returns200()
        {
            // Arrange
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

            var jwtHelper = new JWTHelper(configuration);

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.Code = "123456";
            otpData.IsEmail = true;
            object cacheObj = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(true);

            // User không tồn tại lần đầu, sau đó trả về user mới
            var newUser = new User
            {
                UserId = 3,
                Email = "newuser@email.com",
                FullName = "User newuser",
                Phone = "",
                StatusId = 1,
                RoleId = 1,
                CreateAt = DateTime.UtcNow,
                Role = new Role { RoleName = "User" }
            };
            _authRepoMock.SetupSequence(x => x.GetUserByEmailAsync("newuser@email.com"))
                .ReturnsAsync((User?)null) // lần đầu: chưa có user
                .ReturnsAsync(newUser);    // lần sau: đã tạo user

            _authRepoMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(3);
            _authRepoMock.Setup(x => x.SaveUserTokenAsync(It.IsAny<UserToken>())).Returns(Task.CompletedTask);

            var service = new AuthService(
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<ISMSService>()
            );

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "newuser@email.com",
                SessionToken = "token",
                Otp = "123456"
            };

            // Act
            var result = await service.VerifyOtpAndLoginAsync(req);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Account created and login successful", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(newUser.Email, result.Data.User.Email);
            Assert.True(result.Data.IsNewUser);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        }

        [Fact(DisplayName = "UTCID08 - Login success with user has no Role returns RoleName User")]
        public async Task UTCID08_LoginSuccess_UserNoRole_ReturnsRoleNameUser()
        {
            // Arrange
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

            var jwtHelper = new JWTHelper(configuration);

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.Code = "123456";
            otpData.IsEmail = true;
            object cacheObj = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(true);

            var user = new User
            {
                UserId = 8,
                Email = "norole@email.com",
                FullName = "NoRole User",
                Phone = "",
                StatusId = 1,
                RoleId = 1,
                CreateAt = DateTime.UtcNow,
                Role = null // Quan trọng: Không có Role
            };
            _authRepoMock.Setup(x => x.GetUserByEmailAsync("norole@email.com")).ReturnsAsync(user);
            _authRepoMock.Setup(x => x.SaveUserTokenAsync(It.IsAny<UserToken>())).Returns(Task.CompletedTask);

            var service = new AuthService(
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<ISMSService>()
            );

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "norole@email.com",
                SessionToken = "token",
                Otp = "123456"
            };

            // Act
            var result = await service.VerifyOtpAndLoginAsync(req);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Login successful", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(user.Email, result.Data.User.Email);
            Assert.False(result.Data.IsNewUser);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
            Assert.Equal("User", result.Data.User.RoleName); // Quan trọng: RoleName trả về đúng "User"
        }

        [Fact(DisplayName = "UTCID09 - Login success with user fields Phone, FullName, Email null and RoleName null")]
        public async Task UTCID09_LoginSuccess_UserFieldsAndRoleNameNull_ReturnsDefaultValues()
        {
            // Arrange
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

            var jwtHelper = new JWTHelper(configuration);

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.Code = "123456";
            otpData.IsEmail = true;
            object cacheObj = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheObj)).Returns(true);

            var user = new User
            {
                UserId = 9,
                Email = null,                // Test nhánh Email ?? ""
                FullName = null,             // Test nhánh FullName ?? ""
                Phone = null,                // Test nhánh Phone ?? ""
                StatusId = 1,
                RoleId = 1,
                CreateAt = DateTime.UtcNow,
                Role = new Role { RoleName = null } // Test nhánh RoleName ?? "User"
            };
            _authRepoMock.Setup(x => x.GetUserByEmailAsync("fieldsnull@email.com")).ReturnsAsync(user);
            _authRepoMock.Setup(x => x.SaveUserTokenAsync(It.IsAny<UserToken>())).Returns(Task.CompletedTask);

            var service = new AuthService(
                _authRepoMock.Object,
                jwtHelper,
                _cacheMock.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<ISMSService>()
            );

            var req = new VerifyOtpRequestDto
            {
                PhoneOrEmail = "fieldsnull@email.com",
                SessionToken = "token",
                Otp = "123456"
            };

            // Act
            var result = await service.VerifyOtpAndLoginAsync(req);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Login successful", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("", result.Data.User.Phone);
            Assert.Equal("", result.Data.User.FullName);
            Assert.Equal("", result.Data.User.Email);
            Assert.Equal("User", result.Data.User.RoleName);
        }
    }
}