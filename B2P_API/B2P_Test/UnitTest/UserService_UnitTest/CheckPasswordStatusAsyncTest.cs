using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class CheckPasswordStatusAsyncTest
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<ISMSService> _smsServiceMock;
        private readonly Mock<IImageRepository> _imageRepositoryMock;
        private readonly UserService _service;

        public CheckPasswordStatusAsyncTest()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _cacheMock = new Mock<IMemoryCache>();
            _smsServiceMock = new Mock<ISMSService>();
            _imageRepositoryMock = new Mock<IImageRepository>();

            _service = new UserService(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _cacheMock.Object,
                _imageRepositoryMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Should handle main user scenarios")]
        [InlineData(0, null, 0, null, 400, MessagesCodes.MSG_90)]              // Invalid userId
        [InlineData(1, null, 0, null, 404, MessagesCodes.MSG_65)]              // User not found
        [InlineData(2, "John Doe", 2, null, 400, MessagesCodes.MSG_09)]        // User inactive
        [InlineData(3, "John Doe", 1, "password123", 200, MessagesCodes.MSG_89)] // Active with password
        [InlineData(4, "Jane Doe", 1, null, 200, MessagesCodes.MSG_88)]        // Active without password
        public async Task UTCID01_CheckPasswordStatus_MainScenarios_ReturnsCorrectResponse(
            int userId, string? fullName, int statusId, string? password, int expectedStatus, string expectedMessage)
        {
            // Setup
            User? mockUser = null;
            if (userId > 0 && fullName != null)
            {
                mockUser = new User
                {
                    UserId = userId,
                    FullName = fullName,
                    Email = $"user{userId}@example.com",
                    Phone = $"123456789{userId}",
                    StatusId = statusId,
                    Password = password
                };
            }

            if (userId > 0)
            {
                _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId))
                    .ReturnsAsync(mockUser);
            }

            // Act
            var result = await _service.CheckPasswordStatusAsync(userId);

            // Assert
            Assert.Equal(expectedStatus == 200, result.Success);
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(expectedMessage, result.Message);

            if (expectedStatus == 200 && mockUser != null)
            {
                Assert.NotNull(result.Data);
                VerifySuccessfulResponseData(result.Data, userId, password, mockUser);
            }
            else
            {
                Assert.Null(result.Data);
            }

            // Verify repository calls
            if (userId > 0)
            {
                _userRepositoryMock.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
            }
            else
            {
                _userRepositoryMock.Verify(x => x.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
            }
        }

        [Fact(DisplayName = "UTCID02 - Should return 500 when repository throws exception")]
        public async Task UTCID03_RepositoryException_Returns500()
        {
            // Setup
            var userId = 1;
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.CheckPasswordStatusAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);

            _userRepositoryMock.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        }

        #region Helper Methods

        private static void VerifySuccessfulResponseData(object data, int expectedUserId, string? password, User mockUser)
        {
            var dataType = data.GetType();
            var userIdProp = dataType.GetProperty("UserId");
            var hasPasswordProp = dataType.GetProperty("HasPassword");
            var requireOldPasswordProp = dataType.GetProperty("RequireOldPassword");
            var passwordStatusProp = dataType.GetProperty("PasswordStatus");
            var fullNameProp = dataType.GetProperty("FullName");
            var emailProp = dataType.GetProperty("Email");
            var phoneProp = dataType.GetProperty("Phone");

            Assert.Equal(expectedUserId, userIdProp?.GetValue(data));

            var expectedHasPassword = !string.IsNullOrEmpty(password);
            Assert.Equal(expectedHasPassword, hasPasswordProp?.GetValue(data));
            Assert.Equal(expectedHasPassword, requireOldPasswordProp?.GetValue(data));

            var expectedPasswordStatus = expectedHasPassword ? "Đã thiết lập mật khẩu" : "Chưa thiết lập mật khẩu";
            Assert.Equal(expectedPasswordStatus, passwordStatusProp?.GetValue(data));

            Assert.Equal(mockUser.FullName, fullNameProp?.GetValue(data));
            Assert.Equal(mockUser.Email, emailProp?.GetValue(data));
            Assert.Equal(mockUser.Phone, phoneProp?.GetValue(data));
        }

        #endregion
    }
}