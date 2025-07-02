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

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("ReportList")]
        public async Task<IActionResult> Get([FromQuery, BindRequired] int userId,
            DateTime? startDate, DateTime? endDate, int? facilityId)
        {
            var response = await _reportService.GetReport(userId, startDate, endDate, facilityId);
            return StatusCode(response.Status, response);
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
