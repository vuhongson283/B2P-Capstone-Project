using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Interface;
using B2P_API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilitiesManageController : ControllerBase
    {
        private readonly IFacilityService _facilityService;

        public FacilitiesManageController(IFacilityService facilityService)
        {
            _facilityService = facilityService;
        }

        [HttpGet("listCourt/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetFacilitiesByUser(
    int userId,
    [FromQuery] string? facilityName = null,
    [FromQuery] int? statusId = null,
    [FromQuery] int currentPage = 1,
    [FromQuery] int itemsPerPage = 3)
        {
            try
            {
                var response = await _facilityService.GetFacilitiesByUserAsync(
                    userId,
                    facilityName,
                    statusId,
                    currentPage,
                    itemsPerPage
                );

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Status = 400,
                    Message = ex.Message,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Status = 500,
                    Message = $"Lỗi server: {ex.Message}",
                    Data = null
                });
            }
        }
        [HttpGet("getFacilityById/{facilityId}")]
        public async Task<IActionResult> GetFacilityById(int facilityId)
        {
            try
            {
                var response = await _facilityService.GetFacilityById(facilityId);
                if (response == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Status = 404,
                        Message = "Không tìm thấy cơ sở",
                        Data = null
                    });
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Status = 500,
                    Message = $"Lỗi server: {ex.Message}",
                    Data = null
                });
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
