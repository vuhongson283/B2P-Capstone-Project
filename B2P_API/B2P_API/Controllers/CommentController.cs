using B2P_API.DTOs;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly CommentService _service;

        public CommentController(CommentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Status = 400,
                    Data = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                });
            }

            var result = await _service.CreateAsync(dto);
            return StatusCode(result.Status, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CommentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Status = 400,
                    Data = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                });
            }

            var result = await _service.UpdateAsync(id, dto);
            return StatusCode(result.Status, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int userId, [FromQuery] int roleId)
        {
            var result = await _service.DeleteAsync(id, userId, roleId);
            return StatusCode(result.Status, result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(
    int userId,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string sortBy = "PostAt",
    [FromQuery] string sortDir = "desc")
        {
            var result = await _service.GetByUserIdAsync(userId, page, size, sortBy, sortDir);
            return StatusCode(result.Status, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string sortBy = "postAt",
    [FromQuery] string sortDir = "desc",
    [FromQuery] int? userId = null,
    [FromQuery] int? blogId = null,
    [FromQuery] bool? hasParent = null)
        {
            var result = await _service.GetAllAsync(page, size, sortBy, sortDir, userId, blogId, hasParent);
            return StatusCode(result.Status, result);
        }


    }


}
