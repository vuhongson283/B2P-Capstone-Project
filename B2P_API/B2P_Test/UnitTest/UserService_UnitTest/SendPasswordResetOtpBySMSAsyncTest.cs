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
    public class SendPasswordResetOtpBySMSAsyncTest
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
            var result = await userService.SendPasswordResetOtpBySMSAsync(null);

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
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            // Mock IsValidPhoneNumber trả về false
            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(false);

            var request = new ForgotPasswordRequestBySmsDto { PhoneNumber = "" };

            var result = await userServiceMock.Object.SendPasswordResetOtpBySMSAsync(request);

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
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object
            )
            { CallBase = true };

            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            var request = new ForgotPasswordRequestBySmsDto { PhoneNumber = "0123456789" };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync((User?)null);

            var result = await userServiceMock.Object.SendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Số điện thoại không tồn tại trong hệ thống", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - User is locked returns 404")]
        public async Task UTCID04_UserIsLocked_Returns404()
        {
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
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            var request = new ForgotPasswordRequestBySmsDto { PhoneNumber = "0123456789" };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync(
                new User { UserId = 2, Phone = request.PhoneNumber, StatusId = 2 }
            );

            var result = await userServiceMock.Object.SendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_09, result.Message);
        }

        [Fact(DisplayName = "UTCID05 - SMS send failed returns 500")]
        public async Task UTCID05_SendOtpSmsFailed_Returns500()
        {
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
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            userServiceMock.Protected()
                .Setup<string>("GenerateSecureOtp")
                .Returns("123456");

            var request = new ForgotPasswordRequestBySmsDto { PhoneNumber = "0123456789" };
            var user = new User { UserId = 1, Phone = request.PhoneNumber, StatusId = 1 };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync(user);

            // Mock cache
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(new Mock<ICacheEntry>().Object);

            _smsServiceMock.Setup(x => x.SendOTPAsync(request.PhoneNumber, "123456"))
                .ReturnsAsync(new ApiResponse<object>
                {
                    Success = false,
                    Message = "SMS service error",
                });

            var result = await userServiceMock.Object.SendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Không thể gửi OTP: SMS service error", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Send OTP successfully returns 200")]
        public async Task UTCID06_SendOtpSuccessfully_Returns200()
        {
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
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(true);

            userServiceMock.Protected()
                .Setup<string>("GenerateSecureOtp")
                .Returns("123456");

            var request = new ForgotPasswordRequestBySmsDto { PhoneNumber = "0123456789" };
            var user = new User { UserId = 1, Phone = request.PhoneNumber, StatusId = 1 };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync(user);

            // Mock cache
            _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(new Mock<ICacheEntry>().Object);

            _smsServiceMock.Setup(x => x.SendOTPAsync(request.PhoneNumber, "123456"))
                .ReturnsAsync(new ApiResponse<object>
                {
                    Success = true,
                    Message = "OK",
                });

            var result = await userServiceMock.Object.SendPasswordResetOtpBySMSAsync(request);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Mã OTP đã được gửi đến tin nhắn của bạn.", result.Message);
            _smsServiceMock.Verify(x => x.SendOTPAsync(request.PhoneNumber, "123456"), Times.Once);
        }

        [Fact(DisplayName = "UTCID07 - Exception returns 500")]
        public async Task UTCID07_Exception_Returns500()
        {
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
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Throws(new Exception("some error"));

            var request = new ForgotPasswordRequestBySmsDto { PhoneNumber = "0123456789" };

            var result = await userServiceMock.Object.SendPasswordResetOtpBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("some error", result.Message);
        }
    }
}