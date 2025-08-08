using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.CourtService_UnitTest
{
    public class GetCourtDetailTest
    {
        private readonly Mock<ICourtRepository> _courtRepoMock;
        private readonly CourtServices _service;

        public GetCourtDetailTest()
        {
            _courtRepoMock = new Mock<ICourtRepository>();
            _service = new CourtServices(_courtRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should return full court details successfully")]
        public async Task UTCID01_ValidCourtId_ReturnsFullCourtDetail()
        {
            // Arrange
            var mockCourtDetail = new CourtDetailDTO
            {
                CourtId = 1,
                CourtName = "Sân bóng A",
                PricePerHour = 200000,
                StatusId = 1,
                StatusName = "Hoạt động",
                StatusDescription = "Sân đang hoạt động bình thường",
                CategoryId = 2,
                CategoryName = "Bóng đá",
                FacilityId = 3,
                FacilityName = "Trung tâm thể thao X",
                Location = "123 Đường ABC, Quận 1",
                Contact = "0909123456"
            };

            _courtRepoMock.Setup(x => x.GetCourtDetail(1))
                .ReturnsAsync(mockCourtDetail);

            // Act
            var result = await _service.GetCourtDetail(1);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy thông tin chi tiết sân thành công", result.Message);

            var court = result.Data;
            Assert.NotNull(result.Data);
            Assert.Equal(1, court.CourtId);
            Assert.Equal("Sân bóng A", court.CourtName);
            Assert.Equal(200000, court.PricePerHour);
            Assert.Equal(1, court.StatusId);
            Assert.Equal("Hoạt động", court.StatusName);
            Assert.Equal("Sân đang hoạt động bình thường", court.StatusDescription);
            Assert.Equal(2, court.CategoryId);
            Assert.Equal("Bóng đá", court.CategoryName);
            Assert.Equal(3, court.FacilityId);
            Assert.Equal("Trung tâm thể thao X", court.FacilityName);
            Assert.Equal("123 Đường ABC, Quận 1", court.Location);
            Assert.Equal("0909123456", court.Contact);
        }

        [Fact(DisplayName = "UTCID02 - Should handle partial court details")]
        public async Task UTCID02_PartialCourtDetails_ReturnsSuccess()
        {
            // Arrange
            var mockCourtDetail = new CourtDetailDTO
            {
                CourtId = 2,
                CourtName = "Sân bóng B",
                PricePerHour = 150000,
                // Các trường khác null
                StatusId = null,
                StatusName = null,
                CategoryId = null,
                FacilityId = null
            };

            _courtRepoMock.Setup(x => x.GetCourtDetail(2))
                .ReturnsAsync(mockCourtDetail);

            // Act
            var result = await _service.GetCourtDetail(2);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);

            var court = result.Data;
            Assert.NotNull(court);
            Assert.Equal(2, court.CourtId);
            Assert.Equal("Sân bóng B", court.CourtName);
            Assert.Equal(150000, court.PricePerHour);
            Assert.Null(court.StatusId);
            Assert.Null(court.StatusName);
            Assert.Null(court.CategoryId);
            Assert.Null(court.FacilityId);
        }

        [Fact(DisplayName = "UTCID03 - Should return 404 when court not found")]
        public async Task UTCID03_CourtNotFound_Returns404()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.GetCourtDetail(It.IsAny<int>()))
                .ReturnsAsync((CourtDetailDTO)null);

            // Act
            var result = await _service.GetCourtDetail(999);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy thông tin sân", result.Message);
            Assert.Null(result.Data);
        }

        [Theory(DisplayName = "UTCID04 - Should validate invalid court IDs")]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task UTCID04_InvalidCourtId_Returns400(int invalidCourtId)
        {
            // Act
            var result = await _service.GetCourtDetail(invalidCourtId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("ID sân không hợp lệ", result.Message);
            Assert.Null(result.Data);
            _courtRepoMock.Verify(x => x.GetCourtDetail(It.IsAny<int>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID05 - Should handle database error")]
        public async Task UTCID05_DatabaseError_Returns500()
        {
            // Arrange
            _courtRepoMock.Setup(x => x.GetCourtDetail(1))
                .ThrowsAsync(new Exception("Database timeout"));

            // Act
            var result = await _service.GetCourtDetail(1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Lỗi hệ thống", result.Message);
            Assert.Contains("Database timeout", result.Message);
            Assert.Null(result.Data);
        }
    }
}