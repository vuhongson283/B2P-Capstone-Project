using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using System;
using System.Threading.Tasks;
using Xunit;
using B2P_API.Models;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class SendPasswordResetOtpByEmailAsyncTest
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly Mock<ISMSService> _smsServiceMock = new();
        private readonly Mock<IBankAccountRepository> _bankAccountRepositoryMock = new();
        private readonly Mock<IImageRepository> _imageRepositoryMock = new();

        private UserService CreateUserService()
        {
            return new UserService(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            );
        }

        [Fact(DisplayName = "UTCID01 - Request is null returns 400")]
        public async Task UTCID01_RequestIsNull_Returns400()
        {
            var userService = CreateUserService();
            var result = await userService.SendPasswordResetOtpByEmailAsync(null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_80, result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Email is null or empty returns 400")]
        public async Task UTCID02_EmailIsNullOrEmpty_Returns400()
        {
            var userService = CreateUserService();
            var request = new ForgotPasswordRequestByEmailDto { Email = "" };
            var result = await userService.SendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Email is not real returns 400")]
        public async Task UTCID03_EmailIsNotReal_Returns400()
        {
            var request = new ForgotPasswordRequestByEmailDto { Email = "notreal@email.com" };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(false);

            var result = await userServiceMock.Object.SendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_68, result.Message);
        }

        [Fact(DisplayName = "UTCID04 - User not found returns 404")]
        public async Task UTCID04_UserNotFound_Returns404()
        {
            var request = new ForgotPasswordRequestByEmailDto { Email = "notfound@email.com" };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync((User?)null);

            var result = await userServiceMock.Object.SendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_11, result.Message);
        }

        [Fact(DisplayName = "UTCID05 - User is locked returns 404")]
        public async Task UTCID05_UserIsLocked_Returns404()
        {
            var request = new ForgotPasswordRequestByEmailDto { Email = "locked@email.com" };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync(
                new User { UserId = 2, Email = request.Email, StatusId = 2 }
            );

            var result = await userServiceMock.Object.SendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_09, result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Send OTP successfully returns 200")]
        public async Task UTCID06_SendOtpSuccessfully_Returns200()
        {
            var request = new ForgotPasswordRequestByEmailDto { Email = "user@email.com" };
            var user = new User { UserId = 1, Email = request.Email, StatusId = 1 };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
            userServiceMock.Protected()
                .Setup<string>("GenerateSecureOtp")
                .Returns("123456");
            _emailServiceMock.Setup(x => x.SendOtpEmailAsync(request.Email, "123456")).Returns(Task.CompletedTask);

            // Mock IMemoryCache.TryGetValue and CreateEntry instead of Set extension method
            object dummy;
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(new Mock<ICacheEntry>().Object);

            var result = await userServiceMock.Object.SendPasswordResetOtpByEmailAsync(request);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(MessagesCodes.MSG_91, result.Message);
            _emailServiceMock.Verify(x => x.SendOtpEmailAsync(request.Email, "123456"), Times.Once);
        }

        [Fact(DisplayName = "UTCID07 - Exception return 500")]
        public async Task UTCID07_EmailServiceThrows_Returns500()
        {
            var request = new ForgotPasswordRequestByEmailDto { Email = "user@email.com" };
            var user = new User { UserId = 1, Email = request.Email, StatusId = 1 };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
            userServiceMock.Protected()
                .Setup<string>("GenerateSecureOtp")
                .Returns("123456");
            // Gửi email bị lỗi
            _emailServiceMock.Setup(x => x.SendOtpEmailAsync(request.Email, "123456")).ThrowsAsync(new Exception("Email service failed"));
            // Mock cache
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(new Mock<ICacheEntry>().Object);

            var result = await userServiceMock.Object.SendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Email service failed", result.Message);
        }
    }
}