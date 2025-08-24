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


    }
}