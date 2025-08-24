using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.ReportService_UnitTest
{
    public class GetTotalReportTest
    {
        private readonly Mock<IReportRepository> _reportRepoMock;
        private readonly ReportService _service;
        private readonly int _testUserId = 1;
        private readonly DateTime _testStartDate = DateTime.Now.AddDays(-7);
        private readonly DateTime _testEndDate = DateTime.Now;

        public GetTotalReportTest()
        {
            _reportRepoMock = new Mock<IReportRepository>();
            _service = new ReportService(_reportRepoMock.Object, null); // ExcelExportService không cần trong test này
        }

        [Fact(DisplayName = "UTCID01 - Should return total report successfully")]
        public async Task UTCID01_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var mockReport = new TotalReportDTO
            {
                TotalFacility = 3,
                TotalBooking = 15,
                TotalCourt = 10,
                TotalCost = 25000000m
            };

            _reportRepoMock.Setup(x => x.GetTotalReport(_testUserId, _testStartDate, _testEndDate))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetTotalReport(_testUserId, _testStartDate, _testEndDate);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy dữ liệu báo cáo thành công!", result.Message);
            Assert.NotNull(result.Data);

            // Verify all fields
            Assert.Equal(3, result.Data.TotalFacility);
            Assert.Equal(15, result.Data.TotalBooking);
            Assert.Equal(10, result.Data.TotalCourt);
            Assert.Equal(25000000m, result.Data.TotalCost);
        }

        [Fact(DisplayName = "UTCID03 - Should handle null dates")]
        public async Task UTCID03_NullDates_ReturnsSuccess()
        {
            // Arrange
            var mockReport = new TotalReportDTO
            {
                TotalFacility = 1,
                TotalBooking = 5,
                TotalCourt = 3,
                TotalCost = 5000000m
            };

            _reportRepoMock.Setup(x => x.GetTotalReport(_testUserId, null, null))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetTotalReport(_testUserId, null, null);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.TotalBooking);
        }

        [Fact(DisplayName = "UTCID04 - Should handle zero values")]
        public async Task UTCID04_ZeroValues_ReturnsSuccess()
        {
            // Arrange
            var mockReport = new TotalReportDTO
            {
                TotalFacility = 0,
                TotalBooking = 0,
                TotalCourt = 0,
                TotalCost = 0m
            };

            _reportRepoMock.Setup(x => x.GetTotalReport(_testUserId, _testStartDate, _testEndDate))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetTotalReport(_testUserId, _testStartDate, _testEndDate);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.Data.TotalBooking);
            Assert.Equal(0m, result.Data.TotalCost);
        }

    }
}