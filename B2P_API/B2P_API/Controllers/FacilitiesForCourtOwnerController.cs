using B2P_API.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilitiesForCourtOwnerController : ControllerBase
    {
        private readonly IFacilityService _facilityService;

        public FacilitiesForCourtOwnerController(IFacilityService facilityService)
        {
            _facilityService = facilityService;
        }

        [HttpGet("listCourt/{userId}")]
        public async Task<IActionResult> GetFacilitiesByUser(
        int userId,
        [FromQuery] string? facilityName = null,
        [FromQuery] int? statusId = null)
        {
            try
            {
                var facilities = await _facilityService.GetFacilitiesByUserAsync(userId, facilityName, statusId);
                return Ok(facilities);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
    }
}
