using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.CourtService_UnitTest
{
    public class CreateCourtAsyncTest
    {
        private readonly Mock<ICourtRepository> _courtRepoMock;
        private readonly CourtServices _service;

        public CreateCourtAsyncTest()
        {
            _courtRepoMock = new Mock<ICourtRepository>();
            _service = new CourtServices(_courtRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should create court successfully with valid data")]
        public async Task UTCID01_ValidData_ReturnsSuccess()
        {
            // Arrange
            var validRequest = new CreateCourt
            {
                FacilityId = 1,
                CourtName = "Sân bóng A",
                CategoryId = 2,
                PricePerHour = 200000
            };

            var mockCourt = new Court
            {
                CourtId = 1,
                FacilityId = validRequest.FacilityId,
                CourtName = validRequest.CourtName,
                CategoryId = validRequest.CategoryId,
                PricePerHour = validRequest.PricePerHour,
                StatusId = 1
            };

            _courtRepoMock.Setup(x => x.CreateCourt(validRequest))
                .ReturnsAsync(mockCourt);

            // Act
            var result = await _service.CreateCourtAsync(validRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Sân đã được thêm vào thành công!", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(mockCourt.CourtId, result.Data.CourtId);
            Assert.Equal(validRequest.FacilityId, result.Data.FacilityId);
            Assert.Equal(validRequest.CourtName, result.Data.CourtName);
            Assert.Equal(validRequest.CategoryId, result.Data.CategoryId);
            Assert.Equal(validRequest.PricePerHour, result.Data.PricePerHour);
            Assert.Equal(1, result.Data.StatusId);
        }

        [Theory(DisplayName = "UTCID02 - Should validate required fields")]
        [InlineData(null, "Sân bóng", 1, "200000", "FacilityId là bắt buộc.")]
        [InlineData(1, null, 1, "200000", "CourtName là bắt buộc và không được phép là khoảng trắng.")]
        [InlineData(1, "", 1, "200000", "CourtName là bắt buộc và không được phép là khoảng trắng.")]
        [InlineData(1, "   ", 1, "200000", "CourtName là bắt buộc và không được phép là khoảng trắng.")]
        [InlineData(1, "Sân bóng", null, "200000", "CategoryId là bắt buộc.")]
        [InlineData(1, "Sân bóng", 1, null, "PricePerHour phải lớn hơn 0.")]
        [InlineData(1, "Sân bóng", 1, "0", "PricePerHour phải lớn hơn 0.")]
        [InlineData(1, "Sân bóng", 1, "-100", "PricePerHour phải lớn hơn 0.")]
        public async Task UTCID02_InvalidData_ReturnsValidationErrors(
    int? facilityId, string courtName, int? categoryId, string pricePerHourStr, string expectedMessage)
        {
            // Arrange
            decimal? pricePerHour = string.IsNullOrEmpty(pricePerHourStr)
                ? null
                : decimal.Parse(pricePerHourStr);

            var invalidRequest = new CreateCourt
            {
                FacilityId = facilityId,
                CourtName = courtName,
                CategoryId = categoryId,
                PricePerHour = pricePerHour
            };

            // Act
            var result = await _service.CreateCourtAsync(invalidRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Null(result.Data);
            _courtRepoMock.Verify(x => x.CreateCourt(It.IsAny<CreateCourt>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID03 - Should handle database error")]
        public async Task UTCID03_DatabaseError_Returns500()
        {
            // Arrange
            var validRequest = new CreateCourt
            {
                FacilityId = 1,
                CourtName = "Sân bóng A",
                CategoryId = 2,
                PricePerHour = 200000
            };

            _courtRepoMock.Setup(x => x.CreateCourt(validRequest))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.CreateCourtAsync(validRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Đã xảy ra lỗi: Database connection failed", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Should use returned court from repository")]
        public async Task UTCID04_UsesRepositoryReturnedCourt()
        {
            // Arrange
            var request = new CreateCourt
            {
                FacilityId = 1,
                CourtName = "Sân bóng mới",
                CategoryId = 3,
                PricePerHour = 150000
            };

            var returnedCourt = new Court
            {
                CourtId = 10, // ID được repository trả về
                FacilityId = request.FacilityId,
                CourtName = request.CourtName,
                CategoryId = request.CategoryId,
                PricePerHour = request.PricePerHour,
                StatusId = 1
            };

            _courtRepoMock.Setup(x => x.CreateCourt(request))
                .ReturnsAsync(returnedCourt);

            // Act
            var result = await _service.CreateCourtAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(returnedCourt.CourtId, result.Data.CourtId); // Kiểm tra sử dụng court từ repository
        }
    }
}