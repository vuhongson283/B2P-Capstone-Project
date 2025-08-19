using B2P_API.DTOs.UserDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace B2P_Test.UnitTest.UserService_UnitTest
{
    public class GetUserByIdAsyncTest
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<ISMSService> _smsServiceMock;
        private readonly Mock<IImageRepository> _imageRepositoryMock;
        private readonly UserService _service;

        public GetUserByIdAsyncTest()
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

        [Theory(DisplayName = "UTCID01 - Should return 404 for not found or 200 for success")]
        [InlineData(1, false, 404, MessagesCodes.MSG_65)]        // User not found
        [InlineData(2, true, 200, MessagesCodes.MSG_87)]  // User found
        public async Task UTCID01_GetUserScenarios_ReturnsCorrectResponse(
            int userId, bool userExists, int expectedStatus, string expectedMessage)
        {
            // Setup
            User? mockUser = null;
            if (userExists)
            {
                mockUser = CreateCompleteTestUser(userId);
            }

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(mockUser);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            Assert.Equal(userExists, result.Success);
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(expectedMessage, result.Message);

            if (userExists)
            {
                // Verify successful response with complete data mapping
                Assert.NotNull(result.Data);
                VerifyUserInfoMapping(mockUser!, result.Data);
            }
            else
            {
                // Verify not found response
                Assert.Null(result.Data);
            }

            // Verify repository was called
            _userRepositoryMock.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        }

        [Fact(DisplayName = "UTCID02 - Should handle user with null related data")]
        public async Task UTCID02_GetUserWithNullData_HandlesNullsCorrectly()
        {
            // Setup - User with all null related data
            var userId = 3;
            var mockUser = new User
            {
                UserId = userId,
                FullName = "Test User",
                Email = "test@example.com",
                Phone = "1234567890",
                IsMale = null,             // Nullable bool
                Dob = null,                // Nullable DateOnly
                Address = null,            // Nullable string
                CreateAt = null,           // Nullable DateTime
                Status = null,             // Null status
                Images = new List<Image>() // Empty images
            };

            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(mockUser);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(MessagesCodes.MSG_87, result.Message);
            Assert.NotNull(result.Data);

            // Verify basic user properties
            Assert.Equal("Test User", result.Data.FullName);
            Assert.Equal("test@example.com", result.Data.Email);
            Assert.Equal("1234567890", result.Data.Phone);
            Assert.Null(result.Data.IsMale);          // ✅ Correctly null
            Assert.Null(result.Data.Dob);             // ✅ Correctly null
            Assert.Null(result.Data.Address);         // ✅ Correctly null
            Assert.Null(result.Data.CreateAt);        // ✅ Correctly null

            // ✅ FIXED: Verify null handling for related entities (match your actual code)
            Assert.Null(result.Data.AccountHolder);                    // null ?? null = null
            Assert.Equal(string.Empty, result.Data.AccountNumber);     // null ?? string.Empty
            Assert.Equal(0, result.Data.BankTypeId);                   // null ?? 0
            Assert.Equal("Unknown", result.Data.BankName);             // null ?? "Unknown"
            Assert.Equal(string.Empty, result.Data.StatusDescription); // ✅ FIXED: null ?? string.Empty
            Assert.Equal(0, result.Data.ImageId);                      // FirstOrDefault() ?? 0
            Assert.Equal(string.Empty, result.Data.ImageUrl);          // FirstOrDefault() ?? string.Empty

            _userRepositoryMock.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        }

        [Fact(DisplayName = "UTCID03 - Should return 500 when repository throws exception")]
        public async Task UTCID03_RepositoryException_Returns500()
        {
            // Setup
            var userId = 1;
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);

            // Verify repository was called
            _userRepositoryMock.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        }

        #region Helper Methods

        private User CreateCompleteTestUser(int userId)
        {
            return new User
            {
                UserId = userId,
                FullName = "John Doe",
                Email = "john.doe@example.com",
                Phone = "0987654321",
                IsMale = true,
                Dob = new DateOnly(1985, 5, 15),          // DateOnly
                Address = "123 Test Street, Test City",
                CreateAt = new DateTime(2023, 1, 1),      // DateTime
                Status = new Status
                {
                    StatusId = 1,
                    StatusDescription = "Active"
                },
                Images = new List<Image>
                {
                    new Image
                    {
                        ImageId = 100,
                        ImageUrl = "https://example.com/image1.jpg"
                    },
                    new Image
                    {
                        ImageId = 101,
                        ImageUrl = "https://example.com/image2.jpg"
                    }
                }
            };
        }

        private void VerifyUserInfoMapping(User user, UserInfoResponse userInfo)
        {
            // Verify basic user properties
            Assert.Equal(user.FullName, userInfo.FullName);
            Assert.Equal(user.Email, userInfo.Email);
            Assert.Equal(user.Phone, userInfo.Phone);
            Assert.Equal(user.IsMale, userInfo.IsMale);           // bool? to bool?
            Assert.Equal(user.Dob, userInfo.Dob);                 // DateOnly? to DateOnly?
            Assert.Equal(user.Address, userInfo.Address);         // string? to string?
            Assert.Equal(user.CreateAt, userInfo.CreateAt);       // DateTime? to DateTime?


            // Verify status mapping
            Assert.Equal(user.Status?.StatusDescription, userInfo.StatusDescription); // Both nullable

            // Verify image mapping (first image)
            var firstImage = user.Images.FirstOrDefault();
            Assert.Equal(firstImage?.ImageId ?? 0, userInfo.ImageId);
            Assert.Equal(firstImage?.ImageUrl ?? string.Empty, userInfo.ImageUrl);
        }

        #endregion
    }
}