using B2P_API.DTOs.ReportDTO;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
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

        public ReportController(ReportService reportService, IExcelExportService excelExportService)
        {
            _reportService = reportService;
            _excelExportService = excelExportService;
        }

        [HttpGet("ReportList")]
        public async Task<IActionResult> GetReportList(
            [FromQuery, BindRequired] int userId,
            DateTime? startDate, DateTime? endDate,
            int? facilityId, int pageNumber = 1, int pageSize = 10)
        {
            var response = await _reportService.GetReport(userId, startDate, endDate, facilityId, pageNumber, pageSize);
            return StatusCode(response.Status, response);
        }

        [HttpGet("Export-Report-CourtOwner")]
        public async Task<IActionResult> ExportReportToExcelForCourtOwner(
            [FromQuery, BindRequired] int userId,
            DateTime? startDate, DateTime? endDate,
            int? facilityId, int pageNumber = 1)
        {
            // Dùng method có sẵn trong service
            var excelResponse = await _reportService.ExportReportToExcel(
                userId, startDate, endDate, facilityId, pageNumber, int.MaxValue);

            if (!excelResponse.Success)
            {
                return StatusCode(excelResponse.Status, excelResponse.Message);
            }

            var fileName = $"Report_{_reportService.FormatDateRange(startDate, endDate)}.xlsx";

            return File(
                excelResponse.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet("TotalReport")]
        public async Task<IActionResult> GetTotalReport(
            [FromQuery, BindRequired] int userId,
            DateTime? startDate, DateTime? endDate)
        {
            var response = await _reportService.GetTotalReport(userId, startDate, endDate);
            return StatusCode(response.Status, response);
        }


        [HttpGet("AdminReport")]
        public async Task<IActionResult> GetAdminReport(int? year, int? month)
        {
            var response = await _reportService.GetAdminReportPaged(year, month);
            return StatusCode(response.Status, response);
        }


        [HttpGet("Export-Report-Admin")]
        public async Task<IActionResult> ExportReportToExcelForAdmin(int? year, int? month)
        {
            var reportResponse = await _reportService.ExportAdminReportToExcel(year, month);

            if (!reportResponse.Success)
            {
                return StatusCode(reportResponse.Status, reportResponse.Message);
            }

            var fileName = $"AdminReport_{_reportService.FormatDateRange(null, null)}.xlsx";

            return File(
                reportResponse.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
