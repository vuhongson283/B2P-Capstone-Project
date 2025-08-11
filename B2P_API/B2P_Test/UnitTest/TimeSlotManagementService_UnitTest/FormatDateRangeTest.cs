using B2P_API.Interface;
using B2P_API.Services;
using Moq;
using System;
using Xunit;

namespace B2P_Test.UnitTest.ReportService_UnitTest
{
    public class FormatDateRangeTest
    {
        private readonly ReportService _service;

        public FormatDateRangeTest()
        {
            // Khởi tạo service với mock repository (không sử dụng trong hàm này)
            var reportRepoMock = new Mock<IReportRepository>();
            var excelExportMock = new Mock<IExcelExportService>();
            _service = new ReportService(reportRepoMock.Object, excelExportMock.Object);
        }

        [Theory(DisplayName = "UTCID08 - Định dạng khoảng ngày theo các trường hợp")]
        [InlineData("2023-01-01", "2023-01-31", "From_2023-01-01_To_2023-01-31")] // Có cả ngày bắt đầu và kết thúc
        [InlineData("2023-01-01", null, "From_2023-01-01")]                       // Chỉ có ngày bắt đầu
        [InlineData(null, "2023-01-31", "To_2023-01-31")]                        // Chỉ có ngày kết thúc
        [InlineData(null, null, null)]                                            // Không có ngày nào
        public void TestFormatDateRange(string startDateStr, string endDateStr, string expectedPattern)
        {
            // Arrange - Chuẩn bị dữ liệu
            DateTime? startDate = startDateStr != null ? DateTime.Parse(startDateStr) : null;
            DateTime? endDate = endDateStr != null ? DateTime.Parse(endDateStr) : null;

            // Xử lý trường hợp không có ngày (sử dụng ngày hiện tại)
            if (startDateStr == null && endDateStr == null)
            {
                expectedPattern = DateTime.Now.ToString("yyyy-MM-dd");
            }

            // Act - Gọi hàm cần test
            var result = _service.FormatDateRange(startDate, endDate);

            // Assert - Kiểm tra kết quả
            Assert.Equal(expectedPattern, result);
        }

        [Fact(DisplayName = "UTCID09 - Bỏ qua phần giờ phút giây")]
        public void TestIgnoreTimeComponent()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1, 14, 30, 0);
            var endDate = new DateTime(2023, 1, 31, 23, 59, 59);

            // Act
            var result = _service.FormatDateRange(startDate, endDate);

            // Assert
            Assert.Equal("From_2023-01-01_To_2023-01-31", result);
        }
    }
}