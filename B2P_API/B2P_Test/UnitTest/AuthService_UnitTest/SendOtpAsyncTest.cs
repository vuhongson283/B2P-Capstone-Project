using B2P_API.DTOs.AuthDTOs;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
namespace B2P_Test.UnitTest.AuthService_UnitTest
{
    public class SendOtpAsyncTest
    {
        private readonly Mock<IAuthRepository> _authRepoMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<ISMSService> _smsServiceMock = new();
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IImageRepository> _imageRepoMock = new();

        private JWTHelper CreateJwtHelper()
        {
            return new JWTHelper(_configMock.Object);
        }

        private AuthService CreateService()
        {
            return new AuthService(
                _imageRepoMock.Object,
                _authRepoMock.Object,
                CreateJwtHelper(),
                _cacheMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object
            );
        }

        [Fact(DisplayName = "UTCID01 - PhoneOrEmail is empty returns 400")]
        public async Task UTCID01_PhoneOrEmailEmpty_Returns400()
        {
            var service = CreateService();
            var req = new SendOtpRequestDto { PhoneOrEmail = "" };
            var result = await service.SendOtpAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Phone number or email is required", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Invalid phone/email format returns 400")]
        public async Task UTCID02_InvalidFormat_Returns400()
        {
            var service = CreateService();
            var req = new SendOtpRequestDto { PhoneOrEmail = "invalid@@" };
            var result = await service.SendOtpAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Invalid phone number or email format", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Rate limited returns 429")]
        public async Task UTCID03_RateLimited_Returns429()
        {
            var service = CreateService();
            var req = new SendOtpRequestDto { PhoneOrEmail = "user@email.com" };
            object dummy;
            var cacheKey = $"otp_rate_limit_{req.PhoneOrEmail}";
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out dummy)).Returns(true);

            var result = await service.SendOtpAsync(req);

            Assert.False(result.Success);
            Assert.Equal(429, result.Status);
            Assert.Equal("Please wait before requesting another OTP", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Success send OTP to email")]
        public async Task UTCID04_SuccessSendOtpToEmail()
        {
            var service = CreateService();
            var req = new SendOtpRequestDto { PhoneOrEmail = "user@email.com" };

            object dummy;
            var cacheKey = $"otp_rate_limit_{req.PhoneOrEmail}";
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out dummy)).Returns(false);

            // Mock CreateEntry để tránh lỗi khi gọi .Set (không cần Setup cho Set)
            var entryMock = new Mock<ICacheEntry>();
            _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(entryMock.Object);

            _emailServiceMock.Setup(x => x.SendOtpEmailForLoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await service.SendOtpAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("OTP sent successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrEmpty(result.Data.SessionToken));
            Assert.Contains("@", result.Data.MaskedContact);
            Assert.True((result.Data.ExpiresAt - DateTime.UtcNow).TotalMinutes <= 5.1);
        }

        [Fact(DisplayName = "UTCID05 - Success send OTP to phone")]
        public async Task UTCID05_SuccessSendOtpToPhone()
        {
            var service = CreateService();
            var req = new SendOtpRequestDto { PhoneOrEmail = "0912345678" };

            object dummy;
            var cacheKey = $"otp_rate_limit_{req.PhoneOrEmail}";
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out dummy)).Returns(false);

            // Thêm mock này để tránh lỗi khi code gọi .Set
            var entryMock = new Mock<ICacheEntry>();
            _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(entryMock.Object);

            _smsServiceMock.Setup(x => x.SendOTPAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<object> { Success = true });

            // Act
            var result = await service.SendOtpAsync(req);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("OTP sent successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrEmpty(result.Data.SessionToken));
            Assert.True(result.Data.MaskedContact.Contains("*") || result.Data.MaskedContact.Contains("****"));
            Assert.True((result.Data.ExpiresAt - DateTime.UtcNow).TotalMinutes <= 5.1);
        }

        [Fact(DisplayName = "UTCID06 - Send SMS failed returns 500")]
        public async Task UTCID06_SendSmsFailed_Returns500()
        {
            var service = CreateService();
            var req = new SendOtpRequestDto { PhoneOrEmail = "0912345678" };

            object dummy;
            var cacheKey = $"otp_rate_limit_{req.PhoneOrEmail}";
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out dummy)).Returns(false);

            // Thêm dòng này:
            var entryMock = new Mock<ICacheEntry>();
            _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(entryMock.Object);

            _smsServiceMock.Setup(x => x.SendOTPAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<object> { Success = false, Message = "provider error" });

            _cacheMock.Setup(x => x.Remove(It.IsAny<object>()));

            // Act
            var result = await service.SendOtpAsync(req);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.StartsWith("Failed to send SMS:", result.Message);
            _cacheMock.Verify(x => x.Remove(It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact(DisplayName = "UTCID07 - Exception returns 500")]
        public async Task UTCID07_Exception_Returns500()
        {
            var service = CreateService();
            var req = new SendOtpRequestDto { PhoneOrEmail = "0912345678" };

            object dummy;
            var cacheKey = $"otp_rate_limit_{req.PhoneOrEmail}";
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out dummy)).Throws(new Exception("cache fail"));

            var result = await service.SendOtpAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Failed to send OTP: cache fail", result.Message);
            Assert.Null(result.Data);
        }
    }
}