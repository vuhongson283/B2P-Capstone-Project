using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Caching.Memory;
using B2P_API.DTOs.UserDTO;
using B2P_API.Models;
using B2P_API.Interface;
using B2P_API.Services;
using B2P_API.Utils;
using System;
using System.Threading.Tasks;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class VerifyOtpAndResetPasswordBySMSAsyncTest
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
            var result = await userService.VerifyOtpAndResetPasswordBySMSAsync(null);

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
            userServiceMock.Protected()
                .Setup<bool>("IsValidPhoneNumber", ItExpr.IsAny<string>())
                .Returns(false);

            var request = new VerifyOtpBySmsDto { PhoneNumber = "", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "Abc123" };
            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - OTP code is null or empty returns 400")]
        public async Task UTCID03_OtpCodeEmpty_Returns400()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "   ", NewPassword = "Abc123", ConfirmPassword = "Abc123" };
            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mã OTP không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - OTP code invalid format returns 400")]
        public async Task UTCID04_OtpCodeInvalidFormat_Returns400()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "abc123", NewPassword = "Abc123", ConfirmPassword = "Abc123" };
            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mã OTP phải gồm 6 chữ số", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - NewPassword invalid returns 400")]
        public async Task UTCID05_NewPasswordInvalid_Returns400()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "abc", ConfirmPassword = "abc" };
            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - ConfirmPassword empty returns 400")]
        public async Task UTCID06_ConfirmPasswordEmpty_Returns400()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "   " };
            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Xác nhận mật khẩu không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID07 - Passwords not match returns 400")]
        public async Task UTCID07_PasswordsNotMatch_Returns400()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "321Cba" };
            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_14, result.Message);
        }

        [Fact(DisplayName = "UTCID08 - OTP not found in cache returns 400")]
        public async Task UTCID08_OtpNotFoundInCache_Returns400()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "Abc123" };

            object dummy;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out dummy)).Returns(false);

            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_12, result.Message);
        }

        [Fact(DisplayName = "UTCID09 - OTP code not match returns 400")]
        public async Task UTCID09_OtpCodeNotMatch_Returns400()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "Abc123" };

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.OtpCode = "654321";
            object boxedOtpData = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out boxedOtpData)).Returns(true);

            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_12, result.Message);
        }

        [Fact(DisplayName = "UTCID10 - User not found returns 404")]
        public async Task UTCID10_UserNotFound_Returns404()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "Abc123" };

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.OtpCode = "123456";
            object boxedOtpData = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out boxedOtpData)).Returns(true);

            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync((User?)null);

            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_65, result.Message);
        }

        [Fact(DisplayName = "UTCID11 - Success returns 200")]
        public async Task UTCID11_Success_Returns200()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "Abc123" };

            dynamic otpData = new System.Dynamic.ExpandoObject();
            otpData.OtpCode = "123456";
            object boxedOtpData = otpData;
            _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out boxedOtpData)).Returns(true);

            var user = new User { UserId = 1, Phone = request.PhoneNumber, StatusId = 1 };
            _userRepositoryMock.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(MessagesCodes.MSG_10, result.Message);

            _userRepositoryMock.Verify(x => x.UpdateUserAsync(
                It.Is<User>(u => u.UserId == user.UserId && !string.IsNullOrEmpty(u.Password))
            ), Times.Once);
        }

        [Fact(DisplayName = "UTCID12 - Exception returns 500")]
        public async Task UTCID12_Exception_Returns500()
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

            var request = new VerifyOtpBySmsDto { PhoneNumber = "0123456789", OtpCode = "123456", NewPassword = "Abc123", ConfirmPassword = "Abc123" };

            var result = await userServiceMock.Object.VerifyOtpAndResetPasswordBySMSAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("some error", result.Message);
        }
    }
}