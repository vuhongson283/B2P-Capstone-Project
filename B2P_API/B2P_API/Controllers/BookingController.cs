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
    }
}
