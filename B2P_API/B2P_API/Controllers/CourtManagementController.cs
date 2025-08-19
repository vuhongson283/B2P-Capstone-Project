using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Models;
using B2P_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourtManagementController : ControllerBase
    {
        private readonly CourtServices _courseService;

        public CourtManagementController(CourtServices courseService)
        {
            _courseService = courseService;
        }

        [HttpGet("CourtList")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Get(int pageNumber, int pageSize,
            [FromQuery, BindRequired] int facilityId,
            string? search, int? status, int? categoryId)
        {
            CourtRequestDTO req = new CourtRequestDTO
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                FacilityId = facilityId,
                Search = search,
                Status = status,
                CategoryId = categoryId
            };
            var response = await _courseService.GetAllCourts(req);
            return StatusCode(response.Status, response);
        }
        
        [HttpGet("CourtDetail")]
        public async Task<IActionResult> Get(int courtId)
        {
            var response = await _courseService.GetCourtDetail(courtId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("CreateCourt")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Create([FromBody] CreateCourtRequest request)
        {
            var response = await _courseService.CreateCourtAsync(request);
            return StatusCode(response.Status, response);
        }

        [HttpPut("UpdateCourt")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Update([FromBody] UpdateCourtRequest request, [FromQuery, BindRequired] int userId)
        {
            var response = await _courseService.UpdateCourt(request, userId);
            return StatusCode(response.Status, response);
        }

        [HttpDelete("DeleteCourt")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Delete(int courtId, [FromQuery, BindRequired] int userId)
        {
            var response = await _courseService.DeleteCourt(userId, courtId);
            return StatusCode(response.Status, response);
        }

        [HttpPut("LockCourt")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Lock(int courtId, int statusId, [FromQuery, BindRequired] int userId)
        {
            var response = await _courseService.LockCourt(userId, courtId, statusId);
            return StatusCode(response.Status, response);
        }
    }
}
