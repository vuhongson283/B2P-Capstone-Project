using B2P_API.DTOs.FacilityDTOs;
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

        [HttpPost("createFacility")]
        public async Task<IActionResult> CreateCourt([FromBody] CreateFacilityRequest request)
        {
            
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _facilityService.CreateFacility(request);
                return Ok(response);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPut("updateFacility/{facilityId}")]
        public async Task<IActionResult> UpdateFacility([FromRoute] int facilityId, [FromBody] UpdateFacilityRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _facilityService.UpdateFacility(request, facilityId);
                if (!response.Success)
                {
                    return StatusCode(response.Status, response.Message);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi máy chủ: " + ex.Message);
            }
        }

        [HttpDelete("{facilityId}")]
        public async Task<IActionResult> DeleteFacility(int facilityId)
        {
            var response = await _facilityService.DeleteFacility(facilityId);
            return StatusCode(response.Status, response);
        }

    }
}
