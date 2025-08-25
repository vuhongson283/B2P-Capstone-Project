using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.ReportService_UnitTest
{
    public class GetReportTest
    {
        private readonly Mock<IReportRepository> _reportRepoMock;
        private readonly Mock<IExcelExportService> _excelExportMock;
        private readonly ReportService _service;
        private readonly int _testUserId = 1;
        private readonly int _testFacilityId = 1;
        private readonly DateTime _testStartDate = DateTime.Now.AddDays(-7);
        private readonly DateTime _testEndDate = DateTime.Now;

        public GetReportTest()
        {
            _reportRepoMock = new Mock<IReportRepository>();
            _excelExportMock = new Mock<IExcelExportService>();
            _service = new ReportService(_reportRepoMock.Object, _excelExportMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should return complete report data successfully")]
        public async Task UTCID01_ValidRequest_ReturnsCompleteData()
        {
            // Arrange
            var mockReport = new PagedResponse<ReportDTO>
            {
                CurrentPage = 1,
                ItemsPerPage = 10,
                TotalItems = 2,
                TotalPages = 1,
                Items = new List<ReportDTO>
                {
                    new ReportDTO
                    {
                        CourtCount = 2,
                        CourtCategories = "Sân 5, Sân 7",
                        BookingId = 1001,
                        CustomerName = "Nguyễn Văn A",
                        CustomerEmail = "a.nguyen@example.com",
                        CustomerPhone = "0909123456",
                        TimeSlotCount = 3,
                        CheckInDate = "2023-12-01",
                        TotalPrice = 1500000,
                        BookingTime = DateTime.Parse("2023-11-30 10:00"),
                        BookingStatus = "Confirmed"
                    },
                    new ReportDTO
                    {
                        CourtCount = 1,
                        CourtCategories = "Sân 3",
                        BookingId = 1002,
                        CustomerName = "Trần Thị B",
                        CustomerPhone = "0909876543",
                        TimeSlotCount = 2,
                        CheckInDate = "2023-12-02",
                        TotalPrice = 1000000,
                        BookingTime = DateTime.Parse("2023-12-01 09:00"),
                        BookingStatus = "Completed"
                    }
                }
            };

            _reportRepoMock.Setup(x => x.HasAnyBookings(_testUserId, _testFacilityId))
                .ReturnsAsync(true);

            _reportRepoMock.Setup(x => x.GetReport(1, 10, _testUserId, _testStartDate, _testEndDate, _testFacilityId))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetReport(_testUserId, _testStartDate, _testEndDate, _testFacilityId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy dữ liệu báo cáo thành công!", result.Message);

            // Verify data structure
            var reportData = result.Data;
            Assert.NotNull(reportData);
            Assert.Equal(2, reportData.TotalItems);
            Assert.Equal(2, reportData.Items.Count());

            // Verify first item details
            var firstItem = reportData.Items.ToList()[0];
            Assert.Equal(1001, firstItem.BookingId);
            Assert.Equal("Nguyễn Văn A", firstItem.CustomerName);
            Assert.Equal(1500000, firstItem.TotalPrice);
            Assert.Equal("Confirmed", firstItem.BookingStatus);

            // Verify second item details
            var secondItem = reportData.Items.ToList()[1];
            Assert.Equal(1002, secondItem.BookingId);
            Assert.Equal("Trần Thị B", secondItem.CustomerName);
            Assert.Null(secondItem.CustomerEmail); // Kiểm tra field optional
            Assert.Equal(1000000, secondItem.TotalPrice);
        }

        [Fact(DisplayName = "UTCID02 - Should return no bookings message")]
        public async Task UTCID02_NoBookings_ReturnsMessage()
        {
            // Arrange
            _reportRepoMock.Setup(x => x.HasAnyBookings(_testUserId, _testFacilityId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.GetReport(_testUserId, _testStartDate, _testEndDate, _testFacilityId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cơ sở của bạn chưa có booking nào", result.Message);
            Assert.Null(result.Data);

            _reportRepoMock.Verify(x => x.GetReport(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID03 - Should return no data message")]
        public async Task UTCID03_NoDataInDateRange_ReturnsMessage()
        {
            // Arrange
            _reportRepoMock.Setup(x => x.HasAnyBookings(_testUserId, _testFacilityId))
                .ReturnsAsync(true);

            _reportRepoMock.Setup(x => x.GetReport(1, 10, _testUserId, _testStartDate, _testEndDate, _testFacilityId))
                .ReturnsAsync((PagedResponse<ReportDTO>)null);

            // Act
            var result = await _service.GetReport(_testUserId, _testStartDate, _testEndDate, _testFacilityId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Không có dữ liệu trong khoảng thời gian đã chọn", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Should handle null dates")]
        public async Task UTCID05_NullDates_ReturnsSuccess()
        {
            // Arrange
            var mockReport = new PagedResponse<ReportDTO>
            {
                CurrentPage = 1,
                ItemsPerPage = 10,
                TotalItems = 3,
                TotalPages = 1,
                Items = new List<ReportDTO>()
            };

            _reportRepoMock.Setup(x => x.HasAnyBookings(_testUserId, _testFacilityId))
                .ReturnsAsync(true);

            _reportRepoMock.Setup(x => x.GetReport(1, 10, _testUserId, null, null, _testFacilityId))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetReport(_testUserId, null, null, _testFacilityId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }



    }
}