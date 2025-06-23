using B2P_API.DTOs.FacilityDTO;
using B2P_API.Models;
using B2P_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilitiesController : ControllerBase
    {
        private readonly FacilityService _facilityService;

        public FacilitiesController(FacilityService facilityService)
        {
            _facilityService = facilityService;
        }
        [HttpPost("search")]
        public async Task<IActionResult> SearchFacilities([FromBody] SearchFormRequest request, int pageNumber = 1, int pageSize = 10)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _facilityService.SearchFacilities(request, pageNumber, pageSize);
            if (!response.Success)
            {
                return StatusCode(response.Status, response.Message);
            }
            return Ok(response);

        }
    }
}
