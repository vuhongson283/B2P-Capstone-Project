using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class VerifyOtpAndResetPasswordByEmailAsyncTest
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<ISMSService> _smsServiceMock;
        private readonly Mock<IBankAccountRepository> _bankAccountRepositoryMock;
        private readonly Mock<IImageRepository> _imageRepositoryMock;

        public VerifyOtpAndResetPasswordByEmailAsyncTest()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _cacheMock = new Mock<IMemoryCache>();
            _smsServiceMock = new Mock<ISMSService>();
            _bankAccountRepositoryMock = new Mock<IBankAccountRepository>();
            _imageRepositoryMock = new Mock<IImageRepository>();
        }

        private UserService CreateService(bool emailValidationResult = true)
        {
            return new TestableUserServiceForOtpReset(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _bankAccountRepositoryMock.Object,
                _imageRepositoryMock.Object,
                emailValidationResult
            );
        }

        [Fact(DisplayName = "UTCID01 - Null request returns 400")]
        public async Task UTCID01_NullRequest_Returns400()
        {
            var service = CreateService();
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Dữ liệu không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Empty email returns 400")]
        public async Task UTCID02_EmptyEmail_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Invalid email returns 400")]
        public async Task UTCID03_InvalidEmail_Returns400()
        {
            var service = CreateService(emailValidationResult: false);
            var request = new VerifyOtpDtoByEmail
            {
                Email = "fake@email.com",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_68, result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Empty OTP returns 400")]
        public async Task UTCID04_EmptyOtp_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "user@email.com",
                OtpCode = "",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mã OTP không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - OTP format invalid returns 400")]
        public async Task UTCID05_InvalidOtpFormat_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "user@email.com",
                OtpCode = "1234",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mã OTP phải gồm 6 chữ số", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - New password not meet policy returns 400")]
        public async Task UTCID06_NewPasswordPolicyFail_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "user@email.com",
                OtpCode = "123456",
                NewPassword = "abc123",
                ConfirmPassword = "abc123"
            };
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự", result.Message);
        }

        [Fact(DisplayName = "UTCID07 - Empty confirm password returns 400")]
        public async Task UTCID07_EmptyConfirmPassword_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "user@email.com",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = ""
            };
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Xác nhận mật khẩu không được để trống", result.Message);
        }

        [Fact(DisplayName = "UTCID08 - New password does not match confirm returns 400")]
        public async Task UTCID08_PasswordMismatch_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "user@email.com",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = "Password456"
            };
            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_14, result.Message);
        }

        [Fact(DisplayName = "UTCID09 - OTP not in cache returns 400")]
        public async Task UTCID09_OtpNotInCache_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "missingcache@email.com",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var cacheKey = $"password_reset_otp_{request.Email}";
            object? cacheValue = null;
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cacheValue)).Returns(false);

            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_12, result.Message);
        }

        [Fact(DisplayName = "UTCID10 - Wrong OTP returns 400")]
        public async Task UTCID10_WrongOtp_Returns400()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "wrongotp@email.com",
                OtpCode = "654321",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var cacheKey = $"password_reset_otp_{request.Email}";
            dynamic cacheValue = new System.Dynamic.ExpandoObject();
            cacheValue.OtpCode = "123456";
            cacheValue.UserId = 1;
            cacheValue.Email = request.Email;

            _cacheMock.Setup(x =>
                x.TryGetValue(cacheKey, out It.Ref<object>.IsAny)
            ).Returns((string key, out object value) => { value = cacheValue; return true; });

            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(MessagesCodes.MSG_12, result.Message);
        }

        [Fact(DisplayName = "UTCID11 - User not found returns 404")]
        public async Task UTCID11_UserNotFound_Returns404()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "notfound@email.com",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var cacheKey = $"password_reset_otp_{request.Email}";
            dynamic cacheValue = new System.Dynamic.ExpandoObject();
            cacheValue.OtpCode = "123456";
            cacheValue.UserId = 1;
            cacheValue.Email = request.Email;

            _cacheMock.Setup(x =>
                x.TryGetValue(cacheKey, out It.Ref<object>.IsAny)
            ).Returns((string key, out object value) => { value = cacheValue; return true; });

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync((User?)null);

            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_65, result.Message);
        }

        [Fact(DisplayName = "UTCID12 - Success returns 200")]
        public async Task UTCID12_Success_Returns200()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "success@email.com",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var cacheKey = $"password_reset_otp_{request.Email}";
            dynamic cacheValue = new System.Dynamic.ExpandoObject();
            cacheValue.OtpCode = "123456";
            cacheValue.UserId = 1;
            cacheValue.Email = request.Email;

            _cacheMock.Setup(x =>
                x.TryGetValue(cacheKey, out It.Ref<object>.IsAny)
            ).Returns((string key, out object value) => { value = cacheValue; return true; });
            _cacheMock.Setup(x => x.Remove(cacheKey));

            var user = new User
            {
                Email = request.Email,
                Password = "oldpassword",
                UserId = 1,
                StatusId = 1,
                FullName = "User"
            };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(MessagesCodes.MSG_10, result.Message);

            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.Email == request.Email &&
                !string.IsNullOrEmpty(u.Password) &&
                u.Password != "oldpassword")), Times.Once);
            _cacheMock.Verify(x => x.Remove(cacheKey), Times.Once);
        }

        [Fact(DisplayName = "UTCID13 - Exception returns 500")]
        public async Task UTCID13_Exception_Returns500()
        {
            var service = CreateService();
            var request = new VerifyOtpDtoByEmail
            {
                Email = "error@email.com",
                OtpCode = "123456",
                NewPassword = "Password123",
                ConfirmPassword = "Password123"
            };
            var cacheKey = $"password_reset_otp_{request.Email}";
            dynamic cacheValue = new System.Dynamic.ExpandoObject();
            cacheValue.OtpCode = "123456";
            cacheValue.UserId = 1;
            cacheValue.Email = request.Email;
            _cacheMock.Setup(x =>
                x.TryGetValue(cacheKey, out It.Ref<object>.IsAny)
            ).Returns((string key, out object value) => { value = cacheValue; return true; });

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ThrowsAsync(new Exception("Database error"));

            var result = await service.VerifyOtpAndResetPasswordByEmailAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database error", result.Message);
        }
    }

    // Helper class to override IsRealEmailAsync for testing
    public class TestableUserServiceForOtpReset : UserService
    {
        private readonly bool _emailValidationResult;
        public TestableUserServiceForOtpReset(
            IUserRepository userRepository,
            IEmailService emailService,
            ISMSService smsService,
            IMemoryCache cache,
            IBankAccountRepository bankAccountRepository,
            IImageRepository imageRepository,
            bool emailValidationResult = true)
            : base(userRepository, emailService, smsService, cache, bankAccountRepository, imageRepository)
        {
            _emailValidationResult = emailValidationResult;
        }

        protected override Task<bool> IsRealEmailAsync(string email)
        {
            return Task.FromResult(_emailValidationResult);
        }
    }
}