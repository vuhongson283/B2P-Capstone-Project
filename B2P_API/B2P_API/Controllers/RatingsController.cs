using B2P_API.DTOs.RatingDTO;
using B2P_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingsController : ControllerBase
    {
        private readonly RatingService _service;

        public RatingsController(RatingService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode(result.Status, result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRatingDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return StatusCode(result.Status, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CreateRatingDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return StatusCode(result.Status, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode(result.Status, result);
        }
    }
}
