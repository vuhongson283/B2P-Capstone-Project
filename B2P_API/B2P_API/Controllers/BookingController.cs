using Microsoft.AspNetCore.Mvc;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Response;
using B2P_API.Services;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly BookingService _bookingService;

        public BookingController(BookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequestDto request)
        {
            var result = await _bookingService.CreateBookingAsync(request);
            return StatusCode(result.Status, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookings([FromQuery] int? userId, [FromQuery] BookingQueryParameters query)
        {
            var result = await _bookingService.GetByUserIdAsync(userId, query);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var response = await _bookingService.GetByIdAsync(bookingId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> MarkComplete(int id)
        {
            var result = await _bookingService.MarkBookingCompleteAsync(id);
            return StatusCode(result.Status, result);
        }

        [HttpGet("available-slots")]
        public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int facilityId,
        [FromQuery] int categoryId,
        [FromQuery] DateTime checkInDate)
        {
            var response = await _bookingService.GetTimeSlotAvailabilityAsync(
                facilityId, categoryId, checkInDate);

            return Ok(response);
        }

    }
}
