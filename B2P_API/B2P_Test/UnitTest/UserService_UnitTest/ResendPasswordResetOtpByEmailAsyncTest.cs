using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class ResendPasswordResetOtpByEmailAsyncTest
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly Mock<ISMSService> _smsServiceMock = new();
        private readonly Mock<IImageRepository> _imageRepositoryMock = new();

        private UserService CreateUserService()
        {
            return new UserService(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            );
        }

        [Fact(DisplayName = "UTCID01 - Request is null returns 400")]
        public async Task UTCID01_RequestIsNull_Returns400()
        {
            var userService = CreateUserService();
            var result = await userService.ResendPasswordResetOtpByEmailAsync(null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Dữ liệu không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Email is null or empty returns 400")]
        public async Task UTCID02_EmailIsNullOrEmpty_Returns400()
        {
            var userService = CreateUserService();
            var request = new ResendOtpDtoByEmail { Email = "" };
            var result = await userService.ResendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Email is not real returns 400")]
        public async Task UTCID03_EmailIsNotReal_Returns400()
        {
            var request = new ResendOtpDtoByEmail { Email = "fake@email.com" };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(false);

            var result = await userServiceMock.Object.ResendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_68, result.Message);
        }

        [Fact(DisplayName = "UTCID04 - User not found returns 404")]
        public async Task UTCID04_UserNotFound_Returns404()
        {
            var request = new ResendOtpDtoByEmail { Email = "notfound@email.com" };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync((User?)null);

            var result = await userServiceMock.Object.ResendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Email không tồn tại trong hệ thống", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - User is locked returns 400")]
        public async Task UTCID05_UserIsLocked_Returns400()
        {
            var request = new ResendOtpDtoByEmail { Email = "locked@email.com" };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync(
                new User { UserId = 2, Email = request.Email, StatusId = 2 }
            );

            var result = await userServiceMock.Object.ResendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_09, result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Rate limit returns 500")]
        public async Task UTCID06_RateLimit_Returns500()
        {
            var request = new ResendOtpDtoByEmail { Email = "user@email.com" };
            var user = new User { UserId = 1, Email = request.Email, StatusId = 1 };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ReturnsAsync(true);
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);

            // Mock rate limit đã tồn tại
            object dummy = true;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out dummy))
                .Callback(new TryGetValueCallback((object key, out object? value) => { value = true; }))
                .Returns(true);

            var result = await userServiceMock.Object.ResendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Vui lòng đợi 1 phút trước khi gửi lại OTP", result.Message);
        }

        [Fact(DisplayName = "UTCID07 - Resend OTP successfully returns 200")]
        public async Task UTCID07_ResendOtpSuccessfully_Returns200()
        {
            var request = new ResendOtpDtoByEmail { Email = "user@email.com" };
            var user = new User { UserId = 1, Email = request.Email, StatusId = 1 };

            // Use a test double to override SendPasswordResetOtpByEmailAsync
            var userServiceMock = new UserServiceTestDouble(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            );

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);

            // Mock rate limit chưa tồn tại
            object dummy;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out dummy))
                .Callback(new TryGetValueCallback((object key, out object? value) => { value = null; }))
                .Returns(false);
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

            userServiceMock.SendPasswordResetOtpByEmailAsyncHandler = _ =>
                Task.FromResult(new ApiResponse<object>
                {
                    Data = default!,
                    Message = MessagesCodes.MSG_91,
                    Success = true,
                    Status = 200
                });

            var result = await userServiceMock.ResendPasswordResetOtpByEmailAsync(request);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(MessagesCodes.MSG_91, result.Message);
        }

        // Add this test double class inside your test class or as a nested class
        private class UserServiceTestDouble : UserService
        {
            public Func<ForgotPasswordRequestByEmailDto, Task<ApiResponse<object>>>? SendPasswordResetOtpByEmailAsyncHandler { get; set; }

            public UserServiceTestDouble(
                IUserRepository userRepository,
                IEmailService emailService,
                ISMSService smsService,
                IMemoryCache cache,
                IImageRepository imageRepository
            ) : base(userRepository, emailService, smsService, cache, imageRepository) { }

            protected override Task<bool> IsRealEmailAsync(string email) => Task.FromResult(true);

            public override Task<ApiResponse<object>> SendPasswordResetOtpByEmailAsync(ForgotPasswordRequestByEmailDto? request)
            {
                if (SendPasswordResetOtpByEmailAsyncHandler != null && request != null)
                    return SendPasswordResetOtpByEmailAsyncHandler(request);
                return base.SendPasswordResetOtpByEmailAsync(request);
            }
        }

        [Fact(DisplayName = "UTCID08 - Exception returns 500")]
        public async Task UTCID08_Exception_Returns500()
        {
            var request = new ResendOtpDtoByEmail { Email = "user@email.com" };

            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            // Giả lập lỗi bất kỳ (ví dụ lỗi repo)
            userServiceMock.Protected()
                .Setup<Task<bool>>("IsRealEmailAsync", ItExpr.IsAny<string>())
                .ThrowsAsync(new Exception("Some repo error"));

            var result = await userServiceMock.Object.ResendPasswordResetOtpByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Some repo error", result.Message);
        }

        private delegate void TryGetValueCallback(object key, out object? value);
    }
}