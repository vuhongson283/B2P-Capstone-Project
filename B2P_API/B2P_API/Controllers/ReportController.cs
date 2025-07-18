using B2P_API.DTOs.Account;
using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;
        private readonly IExcelExportService _excelExportService;

        public ReportController(ReportService reportService, IExcelExportService excelExportService )
        {
            _reportService = reportService;
            _excelExportService = excelExportService;
        }

        [HttpGet("ReportList")]
        public async Task<IActionResult> Get( 
            [FromQuery, BindRequired] int userId,
            DateTime? startDate, DateTime? endDate, int? facilityId, int pageNumber =1, int pageSize =10)
        {
            var response = await _reportService.GetReport(userId, startDate, endDate, facilityId, pageNumber, pageSize);
            return StatusCode(response.Status, response);
        }

        [HttpGet("export-report-to-excel")]
        public async Task<IActionResult> ExportReportToExcel(
            [FromQuery, BindRequired] int userId,
            DateTime? startDate, DateTime? endDate, int? facilityId, int pageNumber = 1, int pageSize = 10)
        {
            var reportResponse = await _reportService.GetReport(userId, startDate, endDate, facilityId, pageNumber, int.MaxValue);
            if (!reportResponse.Success)
            {
                return StatusCode(reportResponse.Status, reportResponse.Message);
            }

            var report = reportResponse.Data;
            report.ItemsPerPage = report.Items.Count();
            var excelResponse = await _excelExportService.ExportToExcelAsync<ReportDTO>(
                report,
                "Report");

            if (!excelResponse.Success)
            {
                return StatusCode(excelResponse.Status, excelResponse.Message);
            }

            var fileName = "Report";

            if (startDate.HasValue && endDate.HasValue)
            {
                fileName += $"_From_{startDate.Value:yyyy-MM-dd}_To_{endDate.Value:yyyy-MM-dd}";
            }
            else if (startDate.HasValue)
            {
                fileName += $"_From_{startDate.Value:yyyy-MM-dd}";
            }
            else if (endDate.HasValue)
            {
                fileName += $"_To_{endDate.Value:yyyy-MM-dd}";
            }
            else
            {
                fileName += $"_{DateTime.Now:yyyy-MM-dd}";
            }

            fileName += ".xlsx";


            return File(excelResponse.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet("TotalReport")]
        public async Task<IActionResult> GetTotal([FromQuery, BindRequired] int userId,
            DateTime? startDate, DateTime? endDate)
        {
            var response = await _reportService.GetTotalReport(userId, startDate, endDate);
            return StatusCode(response.Status, response);
        }
    }
}
