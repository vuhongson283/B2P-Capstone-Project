using B2P_API.DTOs.MerchantPaymentDTO;
using B2P_API.Services;
using Microsoft.AspNetCore.Mvc;
using B2P_API.Response;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MerchantPaymentController : ControllerBase
    {
        private readonly MerchantPaymentService _service;

        public MerchantPaymentController(MerchantPaymentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<MerchantPaymentResponseDto>>>> GetAll()
        {
            var response = await _service.GetAllAsync();
            return StatusCode(response.Status, response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<MerchantPaymentResponseDto?>>> GetById(int id)
        {
            var response = await _service.GetByIdAsync(id);
            return StatusCode(response.Status, response);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MerchantPaymentResponseDto>>>> GetByUserId(int userId)
        {
            var response = await _service.GetByUserIdAsync(userId);
            return StatusCode(response.Status, response);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<MerchantPaymentResponseDto>>> Create(MerchantPaymentCreateDto dto)
        {
            var response = await _service.CreateAsync(dto);
            return StatusCode(response.Status, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, MerchantPaymentUpdateDto dto)
        {
            var response = await _service.UpdateAsync(id, dto);
            return StatusCode(response.Status, response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var response = await _service.DeleteAsync(id);
            return StatusCode(response.Status, response);
        }
    }
}
