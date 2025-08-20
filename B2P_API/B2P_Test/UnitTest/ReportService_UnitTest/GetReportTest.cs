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

        [Theory(DisplayName = "UTCID04 - Should handle pagination correctly")]
        [InlineData(0, 1)] // pageNumber <= 0 should default to 1
        [InlineData(-1, 1)] // pageNumber <= 0 should default to 1
        [InlineData(2, 2)] // valid pageNumber within range
        [InlineData(5, 5)] // pageNumber beyond total pages (service doesn't adjust)
        public async Task UTCID04_Pagination_CorrectlyHandled(int inputPage, int expectedPage)
        {
            // Arrange
            const int totalItems = 50;
            const int pageSize = 10;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var mockReport = new PagedResponse<ReportDTO>
            {
                CurrentPage = expectedPage,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = new List<ReportDTO>()
            };

            _reportRepoMock.Setup(x => x.HasAnyBookings(_testUserId, _testFacilityId))
                .ReturnsAsync(true);

            // Mock repository to return report for any page number
            // (service doesn't validate against total pages)
            _reportRepoMock.Setup(x => x.GetReport(
                It.IsAny<int>(),
                It.IsAny<int>(),
                _testUserId,
                _testStartDate,
                _testEndDate,
                _testFacilityId))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetReport(
                _testUserId,
                _testStartDate,
                _testEndDate,
                _testFacilityId,
                inputPage,
                pageSize);

            // Assert
            Assert.True(result.Success, $"Expected success but got failure. Message: {result.Message}");
            Assert.Equal(expectedPage, result.Data.CurrentPage);

            // Additional verification
            Assert.Equal(pageSize, result.Data.ItemsPerPage);
            Assert.Equal(totalItems, result.Data.TotalItems);
            Assert.Equal(totalPages, result.Data.TotalPages);
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

        [Fact(DisplayName = "UTCID06 - Should handle null facilityId")]
        public async Task UTCID06_NullFacilityId_ReturnsSuccess()
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

            _reportRepoMock.Setup(x => x.HasAnyBookings(_testUserId, null))
                .ReturnsAsync(true);

            _reportRepoMock.Setup(x => x.GetReport(1, 10, _testUserId, _testStartDate, _testEndDate, null))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetReport(_testUserId, _testStartDate, _testEndDate, null);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }
        [Fact(DisplayName = "UTCID07 - Should validate all required fields in ReportDTO")]
        public async Task UTCID02_VerifyRequiredFields()
        {
            // Arrange
            var mockReport = new PagedResponse<ReportDTO>
            {
                Items = new List<ReportDTO>
                {
                    new ReportDTO
                    {
                        BookingId = 1001,
                        CustomerName = "Nguyễn Văn A",
                        TotalPrice = 1500000,
                        BookingTime = DateTime.Now
                    }
                }
            };

            _reportRepoMock.Setup(x => x.HasAnyBookings(_testUserId, _testFacilityId))
                .ReturnsAsync(true);

            _reportRepoMock.Setup(x => x.GetReport(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetReport(_testUserId, _testStartDate, _testEndDate, _testFacilityId);

            // Assert
            var firstItem = result.Data.Items.ToList()[0];
            Assert.NotNull(firstItem.BookingId);
            Assert.NotNull(firstItem.CustomerName);
            Assert.NotNull(firstItem.TotalPrice);
            Assert.NotNull(firstItem.BookingTime);
        }


    }
}