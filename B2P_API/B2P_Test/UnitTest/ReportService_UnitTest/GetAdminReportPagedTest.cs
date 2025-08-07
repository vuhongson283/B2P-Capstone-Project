using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.ReportService_UnitTest
{
    public class GetAdminReportPagedTest
    {
        private readonly Mock<IReportRepository> _reportRepoMock;
        private readonly ReportService _service;
        private readonly int _testYear = 2023;
        private readonly int _testMonth = 6;

        public GetAdminReportPagedTest()
        {
            _reportRepoMock = new Mock<IReportRepository>();
            _service = new ReportService(_reportRepoMock.Object, null);
        }

        [Fact(DisplayName = "UTCID01 - Trả về báo cáo thành công khi có dữ liệu")]
        public async Task UTCID01_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var mockReport = new AdminReportDTO
            {
                Year = _testYear,
                Month = _testMonth,
                StartDate = new DateTime(_testYear, _testMonth, 1),
                EndDate = new DateTime(_testYear, _testMonth, DateTime.DaysInMonth(_testYear, _testMonth)),
                TotalStats = new TotalStatsDTO
                {
                    TotalFacilities = 25,
                    TotalCourts = 150,
                    ActiveUsers = 500
                },
                MonthlyStats = new MonthlyStatsDTO
                {
                    TotalBooking = 100,
                    TotalRevenue = 50000000,
                    AverageRevenuePerBooking = 500000,
                    CompletedBookings = 85,
                    CancelledBookings = 15
                },
                TopFacilities = new List<FacilityStatDTO>
                {
                    new FacilityStatDTO { FacilityId = 1, FacilityName = "Facility A", TotalBooking = 30, TotalRevenue = 15000000 },
                    new FacilityStatDTO { FacilityId = 2, FacilityName = "Facility B", TotalBooking = 25, TotalRevenue = 12500000 }
                },
                PopularCourtCategories = new List<CourtCategoryStatDTO>
                {
                    new CourtCategoryStatDTO { CategoryName = "Tennis", TotalBooking = 40 },
                    new CourtCategoryStatDTO { CategoryName = "Badminton", TotalBooking = 35 }
                }
            };

            _reportRepoMock.Setup(x => x.GetAdminReport(_testYear, _testMonth))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetAdminReportPaged(_testYear, _testMonth);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy dữ liệu báo cáo thành công!", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(_testYear, result.Data.Year);
            Assert.Equal(_testMonth, result.Data.Month);
            Assert.Equal(50000000, result.Data.MonthlyStats.TotalRevenue);
            Assert.Equal(100, result.Data.MonthlyStats.TotalBooking);
            Assert.Equal(25, result.Data.TotalStats.TotalFacilities);
            Assert.Equal(2, result.Data.TopFacilities.Count);
            Assert.Equal(2, result.Data.PopularCourtCategories.Count);
        }

        [Fact(DisplayName = "UTCID02 - Xử lý khi không có dữ liệu")]
        public async Task UTCID02_NoData_ReturnsMessage()
        {
            // Arrange
            _reportRepoMock.Setup(x => x.GetAdminReport(_testYear, _testMonth))
                .ReturnsAsync((AdminReportDTO)null);

            // Act
            var result = await _service.GetAdminReportPaged(_testYear, _testMonth);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Không có dữ liệu trong khoảng thời gian đã chọn", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Chỉ lọc theo năm")]
        public async Task UTCID03_YearOnlyFilter_ReturnsSuccess()
        {
            // Arrange
            var mockReport = new AdminReportDTO
            {
                Year = _testYear,
                Month = 0, // Không có tháng cụ thể
                StartDate = new DateTime(_testYear, 1, 1),
                EndDate = new DateTime(_testYear, 12, 31),
                TotalStats = new TotalStatsDTO
                {
                    TotalFacilities = 30,
                    TotalCourts = 200,
                    ActiveUsers = 800
                },
                MonthlyStats = new MonthlyStatsDTO
                {
                    TotalBooking = 1200,
                    TotalRevenue = 30000000,
                    AverageRevenuePerBooking = 25000,
                    CompletedBookings = 1000,
                    CancelledBookings = 200
                },
                TopFacilities = new List<FacilityStatDTO>(),
                PopularCourtCategories = new List<CourtCategoryStatDTO>()
            };

            _reportRepoMock.Setup(x => x.GetAdminReport(_testYear, null))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetAdminReportPaged(_testYear, null);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(_testYear, result.Data.Year);
            Assert.Equal(30000000, result.Data.MonthlyStats.TotalRevenue);
            Assert.Equal(1200, result.Data.MonthlyStats.TotalBooking);
        }

        [Fact(DisplayName = "UTCID04 - Chỉ lọc theo tháng")]
        public async Task UTCID04_MonthOnlyFilter_ReturnsSuccess()
        {
            // Arrange
            var mockReport = new AdminReportDTO
            {
                Year = 0, // Không có năm cụ thể
                Month = _testMonth,
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MaxValue,
                TotalStats = new TotalStatsDTO
                {
                    TotalFacilities = 20,
                    TotalCourts = 100,
                    ActiveUsers = 300
                },
                MonthlyStats = new MonthlyStatsDTO
                {
                    TotalBooking = 50,
                    TotalRevenue = 10000000,
                    AverageRevenuePerBooking = 200000,
                    CompletedBookings = 40,
                    CancelledBookings = 10
                },
                TopFacilities = new List<FacilityStatDTO>(),
                PopularCourtCategories = new List<CourtCategoryStatDTO>()
            };

            _reportRepoMock.Setup(x => x.GetAdminReport(null, _testMonth))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetAdminReportPaged(null, _testMonth);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(_testMonth, result.Data.Month);
            Assert.Equal(50, result.Data.MonthlyStats.TotalBooking);
            Assert.Equal(10000000, result.Data.MonthlyStats.TotalRevenue);
        }

        [Fact(DisplayName = "UTCID05 - Xử lý lỗi database")]
        public async Task UTCID05_DatabaseError_Returns500()
        {
            // Arrange
            var errorMessage = "Lỗi kết nối database";
            _reportRepoMock.Setup(x => x.GetAdminReport(_testYear, _testMonth))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await _service.GetAdminReportPaged(_testYear, _testMonth);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Đã xảy ra lỗi: " + errorMessage, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Không có bộ lọc")]
        public async Task UTCID06_NoFilter_ReturnsAllData()
        {
            // Arrange
            var mockReport = new AdminReportDTO
            {
                Year = 0,
                Month = 0,
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MaxValue,
                TotalStats = new TotalStatsDTO
                {
                    TotalFacilities = 50,
                    TotalCourts = 300,
                    ActiveUsers = 1000
                },
                MonthlyStats = new MonthlyStatsDTO
                {
                    TotalBooking = 2000,
                    TotalRevenue = 100000000,
                    AverageRevenuePerBooking = 50000,
                    CompletedBookings = 1800,
                    CancelledBookings = 200
                },
                TopFacilities = new List<FacilityStatDTO>
                {
                    new FacilityStatDTO { FacilityId = 1, FacilityName = "Top Facility", TotalBooking = 100, TotalRevenue = 50000000 }
                },
                PopularCourtCategories = new List<CourtCategoryStatDTO>
                {
                    new CourtCategoryStatDTO { CategoryName = "Popular Sport", TotalBooking = 500 }
                }
            };

            _reportRepoMock.Setup(x => x.GetAdminReport(null, null))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetAdminReportPaged();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(100000000, result.Data.MonthlyStats.TotalRevenue);
            Assert.Equal(2000, result.Data.MonthlyStats.TotalBooking);
            Assert.Equal(1000, result.Data.TotalStats.ActiveUsers);
            Assert.Single(result.Data.TopFacilities);
            Assert.Single(result.Data.PopularCourtCategories);
        }

        [Fact(DisplayName = "UTCID07 - Kiểm tra dữ liệu TopFacilities")]
        public async Task UTCID07_ValidRequest_ReturnsTopFacilities()
        {
            // Arrange
            var mockReport = new AdminReportDTO
            {
                Year = _testYear,
                Month = _testMonth,
                StartDate = new DateTime(_testYear, _testMonth, 1),
                EndDate = new DateTime(_testYear, _testMonth, DateTime.DaysInMonth(_testYear, _testMonth)),
                TotalStats = new TotalStatsDTO(),
                MonthlyStats = new MonthlyStatsDTO(),
                TopFacilities = new List<FacilityStatDTO>
                {
                    new FacilityStatDTO { FacilityId = 1, FacilityName = "Sân A", TotalBooking = 50, TotalRevenue = 20000000 },
                    new FacilityStatDTO { FacilityId = 2, FacilityName = "Sân B", TotalBooking = 40, TotalRevenue = 15000000 },
                    new FacilityStatDTO { FacilityId = 3, FacilityName = "Sân C", TotalBooking = 30, TotalRevenue = 10000000 }
                },
                PopularCourtCategories = new List<CourtCategoryStatDTO>()
            };

            _reportRepoMock.Setup(x => x.GetAdminReport(_testYear, _testMonth))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetAdminReportPaged(_testYear, _testMonth);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.Data.TopFacilities.Count);
            Assert.Equal("Sân A", result.Data.TopFacilities[0].FacilityName);
            Assert.Equal(50, result.Data.TopFacilities[0].TotalBooking);
            Assert.Equal(20000000, result.Data.TopFacilities[0].TotalRevenue);
        }

        [Fact(DisplayName = "UTCID08 - Kiểm tra dữ liệu PopularCourtCategories")]
        public async Task UTCID08_ValidRequest_ReturnsPopularCourtCategories()
        {
            // Arrange
            var mockReport = new AdminReportDTO
            {
                Year = _testYear,
                Month = _testMonth,
                StartDate = new DateTime(_testYear, _testMonth, 1),
                EndDate = new DateTime(_testYear, _testMonth, DateTime.DaysInMonth(_testYear, _testMonth)),
                TotalStats = new TotalStatsDTO(),
                MonthlyStats = new MonthlyStatsDTO(),
                TopFacilities = new List<FacilityStatDTO>(),
                PopularCourtCategories = new List<CourtCategoryStatDTO>
                {
                    new CourtCategoryStatDTO { CategoryName = "Tennis", TotalBooking = 80 },
                    new CourtCategoryStatDTO { CategoryName = "Badminton", TotalBooking = 60 },
                    new CourtCategoryStatDTO { CategoryName = "Basketball", TotalBooking = 40 }
                }
            };

            _reportRepoMock.Setup(x => x.GetAdminReport(_testYear, _testMonth))
                .ReturnsAsync(mockReport);

            // Act
            var result = await _service.GetAdminReportPaged(_testYear, _testMonth);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.Data.PopularCourtCategories.Count);
            Assert.Equal("Tennis", result.Data.PopularCourtCategories[0].CategoryName);
            Assert.Equal(80, result.Data.PopularCourtCategories[0].TotalBooking);
            Assert.Equal("Badminton", result.Data.PopularCourtCategories[1].CategoryName);
            Assert.Equal(60, result.Data.PopularCourtCategories[1].TotalBooking);
        }
    }
}