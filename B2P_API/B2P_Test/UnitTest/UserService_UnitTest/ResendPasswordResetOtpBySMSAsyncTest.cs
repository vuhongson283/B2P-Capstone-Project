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
    public class ResendPasswordResetOtpBySMSAsyncTest
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

        [Fact(DisplayName = "UTCID01 - Request null returns 400")]
        public async Task UTCID01_RequestIsNull_Returns400()
        {
            var userService = CreateUserService();
            var result = await userService.ResendPasswordResetOtpBySMSAsync(null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Dữ liệu không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Phone is null or invalid returns 400")]
        public async Task UTCID02_PhoneIsNullOrInvalid_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(false);

            var request = new ResendOtpBySmsDto { PhoneNumber = "" };
            var result = await userServiceMock.Object.ResendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - User not found returns 404")]
        public async Task UTCID03_UserNotFound_Returns404()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            var request = new ResendOtpBySmsDto { PhoneNumber = "0123456789" };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync((User?)null);

            var result = await userServiceMock.Object.ResendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Số điện thoại không tồn tại trong hệ thống", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - User is locked returns 400")]
        public async Task UTCID04_UserIsLocked_Returns400()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            var request = new ResendOtpBySmsDto { PhoneNumber = "0123456789" };
            var user = new User { UserId = 1, Phone = request.PhoneNumber, StatusId = 2 };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync(user);

            var result = await userServiceMock.Object.ResendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_09, result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Rate limit hit returns 500")]
        public async Task UTCID05_RateLimit_Returns500()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            var request = new ResendOtpBySmsDto { PhoneNumber = "0123456789" };
            var user = new User { UserId = 1, Phone = request.PhoneNumber, StatusId = 1 };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync(user);

            object? dummy = null;
            var rateLimitKey = $"rate_limit_otp_{request.PhoneNumber}";
            _cacheMock.Setup(x => x.TryGetValue(rateLimitKey, out dummy!)).Returns(true);

            var result = await userServiceMock.Object.ResendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Vui lòng đợi 1 phút trước khi gửi lại OTP", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Success returns 200")]
        public async Task UTCID06_Success_Returns200()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            var request = new ResendOtpBySmsDto { PhoneNumber = "0123456789" };
            var user = new User { UserId = 1, Phone = request.PhoneNumber, StatusId = 1 };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync(user);

            object dummy = null!;
            var rateLimitKey = $"rate_limit_otp_{request.PhoneNumber}";
            _cacheMock.Setup(x => x.TryGetValue(rateLimitKey, out dummy)).Returns(false);

            // Mock CreateEntry thay cho Set
            var cacheEntryMock = new Mock<ICacheEntry>();
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

            // **Sửa tại đây: Setup trực tiếp method public**
            userServiceMock
                .Setup(x => x.SendPasswordResetOtpBySMSAsync(It.IsAny<ForgotPasswordRequestBySmsDto>()))
                .ReturnsAsync(new ApiResponse<object>
                {
                    Success = true,
                    Status = 200,
                    Message = "OTP sent"
                });

            var result = await userServiceMock.Object.ResendPasswordResetOtpBySMSAsync(request);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("OTP sent", result.Message);

            _cacheMock.Verify(x => x.CreateEntry(rateLimitKey), Times.Once);
        }

        [Fact(DisplayName = "UTCID07 - Exception returns 500")]
        public async Task UTCID07_Exception_Returns500()
        {
            var userServiceMock = new Mock<UserService>(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Throws(new Exception("some error"));

            var request = new ResendOtpBySmsDto { PhoneNumber = "0123456789" };

            var result = await userServiceMock.Object.ResendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("some error", result.Message);
        }
    }
}