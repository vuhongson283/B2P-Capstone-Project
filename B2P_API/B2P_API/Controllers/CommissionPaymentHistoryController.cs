using Microsoft.AspNetCore.Mvc;
using B2P_API.DTOs;
using B2P_API.Services;
using B2P_API.Response;
using B2P_API.DTOs.CommissionPaymentHistoryDTOs;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommissionPaymentHistoryController : ControllerBase
    {
        private readonly CommissionPaymentHistoryService _service;

        public CommissionPaymentHistoryController(CommissionPaymentHistoryService service)
        {
            _service = service;
        }

        [HttpGet("getall")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CommissionPaymentHistoryDto>>>> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("getbyuserid/{userId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CommissionPaymentHistoryDto>>>> GetByUserId(int userId)
        {
            return Ok(await _service.GetByUserIdAsync(userId));
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<CommissionPaymentHistoryDto>>> Create([FromBody] CommissionPaymentHistoryCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return StatusCode(result.Status, result);
        }

        [HttpPut("update/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> Update(int id, [FromBody] CommissionPaymentHistoryUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return StatusCode(result.Status, result);
        }
    }
}
