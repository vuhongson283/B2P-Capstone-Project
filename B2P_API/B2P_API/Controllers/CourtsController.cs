using B2P_API.DTOs;
using B2P_API.Models;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourtsController : ControllerBase
    {
        private readonly CourtServices _courseService;

        public CourtsController(CourtServices courseService)
        {
            _courseService = courseService;
        }

        [HttpGet("CourtList")]
        public async Task<IActionResult> Get(int pageNumber, int pageSize,
            string? search, int? status, int? categoryId)
        {
            var response = await _courseService.GetAllCourts(pageNumber, pageSize,
            search,  status, categoryId);
            return StatusCode(response.Status, response);
        }
        
        [HttpGet("CourtDetail")]
        public async Task<IActionResult> Get(int courtId)
        {
            var response = await _courseService.GetCourtDetail(courtId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("CreateCourt")]
        public async Task<IActionResult> Create([FromBody] CreateCourt request)
        {
            var response = await _courseService.CreateCourtAsync(request);
            return StatusCode(response.Status, response);
        }

        [HttpPut("UpdateCourt")]
        public async Task<IActionResult> Update([FromBody] UpdateCourtRequest request)
        {
            var response = await _courseService.UpdateCourt(request);
            return StatusCode(response.Status, response);
        }

        [HttpDelete("DeleteCourt")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _courseService.DeleteCourt(id);
            return StatusCode(response.Status, response);
        }

        [HttpPut("LockCourt")]
        public async Task<IActionResult> Lock(int courtId, int statusId)
        {
            var response = await _courseService.LockCourt(courtId, statusId);
            return StatusCode(response.Status, response);
        }
    }
}
