using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.CourtService_UnitTest
{
    public class UpdateCourtTest
    {
        private readonly Mock<ICourtRepository> _courtRepoMock;
        private readonly CourtServices _service;
        private readonly int _testUserId = 1;
        private readonly int _testCourtId = 1;

        public UpdateCourtTest()
        {
            _courtRepoMock = new Mock<ICourtRepository>();
            _service = new CourtServices(_courtRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should update court successfully with valid data")]
        public async Task UTCID01_ValidData_ReturnsSuccess()
        {
            // Arrange
            var request = new UpdateCourtRequest
            {
                CourtId = _testCourtId,
                CourtName = "Sân bóng mới",
                PricePerHour = 250000m
            };

            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.UpdateCourt(request))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateCourt(request, _testUserId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật sân thành công.", result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Should return error when user is not court owner")]
        public async Task UTCID02_NotOwner_ReturnsError()
        {
            // Arrange
            var request = new UpdateCourtRequest { CourtId = _testCourtId };

            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(false);

            // Act
            var result = await _service.UpdateCourt(request, _testUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Tài khoản không thể cập nhật sân này vì không phải tài khoản chủ sân.", result.Message);
            _courtRepoMock.Verify(x => x.UpdateCourt(It.IsAny<UpdateCourtRequest>()), Times.Never());
        }

        [Theory(DisplayName = "UTCID03 - Should validate court name properly")]
        [InlineData(" ")]
        [InlineData("")]
        public async Task UTCID03_InvalidCourtName_ReturnsError(string courtName)
        {
            // Arrange
            var request = new UpdateCourtRequest
            {
                CourtId = _testCourtId,
                CourtName = courtName
            };

            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            // Act
            var result = await _service.UpdateCourt(request, _testUserId);

            // Assert
            if (string.IsNullOrWhiteSpace(courtName))
            {
                Assert.False(result.Success);
                Assert.Equal(400, result.Status);
                Assert.Equal("CourtName không thể chỉ là khoảng trắng.", result.Message);
            }
        }

        [Theory(DisplayName = "UTCID04 - Should validate price properly")]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task UTCID04_InvalidPrice_ReturnsError(decimal price)
        {
            // Arrange
            var request = new UpdateCourtRequest
            {
                CourtId = _testCourtId,
                PricePerHour = price
            };

            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            // Act
            var result = await _service.UpdateCourt(request, _testUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("PricePerHour phải lớn hơn 0.", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Should return not found when court doesn't exist")]
        public async Task UTCID05_CourtNotFound_ReturnsError()
        {
            // Arrange
            var request = new UpdateCourtRequest { CourtId = _testCourtId };

            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.UpdateCourt(request))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateCourt(request, _testUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy sân.", result.Message);
        }

        [Fact(DisplayName = "UTCID07 - Should handle exceptions properly")]
        public async Task UTCID07_Exception_ReturnsError()
        {
            // Arrange
            var request = new UpdateCourtRequest { CourtId = _testCourtId };

            _courtRepoMock.Setup(x => x.CheckCourtOwner(_testUserId, _testCourtId))
                .Returns(true);

            _courtRepoMock.Setup(x => x.UpdateCourt(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.UpdateCourt(request, _testUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Đã xảy ra lỗi trong quá trình cập nhật: Database error", result.Message);
        }
    }
}