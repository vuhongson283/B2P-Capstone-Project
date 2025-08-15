using B2P_API.DTOs.TimeslotDTO;
using B2P_API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2P_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeslotManagementController : ControllerBase
    {
        private readonly ITimeSlotManagementService _service;

        public TimeslotManagementController(ITimeSlotManagementService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Create([FromBody] CreateTimeslotRequestDTO request)
        {
            var result = await _service.CreateNewTimeSlot(request);
            return StatusCode(result.Status, result);
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CreateTimeslotRequestDTO request)
        {
            var result = await _service.UpdateTimeSlot(request, id);
            return StatusCode(result.Status, result);
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var result = await _service.DeleteTimeSlot(id);
            return StatusCode(result.Status, result);
        }

        [HttpGet("facility/{facilityId}")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> GetByFacilityId(
            [FromRoute] int facilityId,
            [FromQuery] int? statusId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetTimeslotByFacilityIdAsync(facilityId, statusId, pageNumber, pageSize);
            return StatusCode(result.Status, result);
        }
    }
}
