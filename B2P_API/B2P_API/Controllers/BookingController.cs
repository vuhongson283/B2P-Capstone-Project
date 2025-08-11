using Microsoft.AspNetCore.Mvc;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Response;
using B2P_API.Services;
using Newtonsoft.Json;

namespace B2P_API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class BookingController : ControllerBase
	{
		private readonly BookingService _bookingService;
		private readonly IBookingNotificationService _notificationService;

		public BookingController(
			BookingService bookingService,
			IBookingNotificationService notificationService)
		{
			_bookingService = bookingService;
			_notificationService = notificationService;
		}

		[HttpPost]
		public async Task<IActionResult> CreateBooking([FromBody] BookingRequestDto request)
		{
			try
			{
				var result = await _bookingService.CreateBookingAsync(request);

				if (result.Success)
				{
					// Biến object -> DTO
					var data = JsonConvert.DeserializeObject<CreateBookingResultDto>(
						JsonConvert.SerializeObject(result.Data));

					if (data?.Slots != null && data.Slots.Count > 0)
					{
						string Fmt(string t) => string.IsNullOrWhiteSpace(t) ? "" : t.Substring(0, 5);

						foreach (var slot in data.Slots)
						{
							var start = slot.StartTime; // "10:00:00"
							var end = slot.EndTime;   // "12:00:00"

							await _notificationService.NotifyBookingCreated(request.FacilityId, new
							{
								bookingId = data.BookingId,              // 1131
								facilityId = request.FacilityId,          // 27
								courtId = slot.CourtId,                // 19
								courtName = slot.CourtName,              // "Sân 5 người - CMT8 - 2"
								customerName = data.User?.Email?.Split('@')[0] ?? "Khách",
								customerEmail = data.User?.Email,
								customerPhone = data.User?.Phone,
								date = data.CheckInDate.ToString("dd/MM/yyyy"),
								checkInTime = Fmt(start),                  // "10:00"
								timeSlot = $"{Fmt(start)} - {Fmt(end)}",// "10:00 - 12:00"
								status = "paid",
								action = "created",
								totalAmount = 0,                           // nếu có field thực thì thay vào
								timestamp = DateTime.UtcNow.ToString("o")
							});
						}
					}
				}

				return StatusCode(result.Status, result);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error in CreateBooking: {ex.Message}");
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetBookings([FromQuery] int? userId, [FromQuery] BookingQueryParameters query)
		{
			var result = await _bookingService.GetByUserIdAsync(userId, query);

			if (!result.Success)
				return BadRequest(result);

			return Ok(result);
		}

		[HttpGet("court-owner")]
		public async Task<IActionResult> GetBookingsForCourtOwner([FromQuery] BookingQueryParameters query)
		{
			var result = await _bookingService.GetByUserIdAsync(null, query);

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
			try
			{
				var result = await _bookingService.MarkBookingCompleteAsync(id);

				if (result.Success)
				{
					// 🔔 GỬI THÔNG BÁO SIGNALR đơn giản
					await _notificationService.NotifyBookingCompleted(0, new // facilityId = 0 tạm thời
					{
						bookingId = id,
						status = "completed",
						action = "completed",
						message = "Đơn đặt sân đã được hoàn thành",
						timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
					});

					Console.WriteLine($"✅ Booking {id} completed and SignalR notification sent");
				}

				return StatusCode(result.Status, result);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error in MarkComplete: {ex.Message}");
				return StatusCode(500, new { message = ex.Message });
			}
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



		public class CreateBookingResultDto
		{
			public int BookingId { get; set; }
			public DateTime CheckInDate { get; set; }   // "2025-08-11T00:00:00Z"
			public CreateBookingUserDto User { get; set; }
			public List<CreateBookingSlotDto> Slots { get; set; }
		}

		public class CreateBookingUserDto
		{
			public int UserId { get; set; }
			public string Email { get; set; }
			public string Phone { get; set; }
		}

		public class CreateBookingSlotDto
		{
			public int TimeSlotId { get; set; }
			public string StartTime { get; set; } // "10:00:00"
			public string EndTime { get; set; }   // "12:00:00"
			public int CourtId { get; set; }
			public string CourtName { get; set; }
		}
	}
}