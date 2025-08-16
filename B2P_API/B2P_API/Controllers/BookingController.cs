using Microsoft.AspNetCore.Mvc;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Response;
using B2P_API.Services;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

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
		[HttpPost("create-simple")]
		public async Task<IActionResult> CreateSimpleBooking([FromBody] SimpleBookingDto request)
		{
			var result = await _bookingService.CreateSimpleBookingAsync(request);
			return StatusCode(result.Status, result);
		}
		[HttpPost]
		public async Task<IActionResult> CreateBooking([FromBody] BookingRequestDto request)
		{
			try
			{
				var result = await _bookingService.CreateBookingAsync(request);

				if (result.Success)
				{
					var data = JsonConvert.DeserializeObject<CreateBookingResultDto>(
						JsonConvert.SerializeObject(result.Data));

					if (data?.Slots != null && data.Slots.Count > 0)
					{
						// ✅ FIX: Lấy booking details để có thông tin Status
						var bookingDetailsResponse = await _bookingService.GetByIdAsync(data.BookingId);

						string Fmt(string t) => string.IsNullOrWhiteSpace(t) ? "" : t.Substring(0, 5);

						foreach (var slot in data.Slots)
						{
							var start = slot.StartTime;
							var end = slot.EndTime;

							// ✅ FIX: Xử lý response để lấy Status
							string status = "Unpaid";
							int statusId = 8;
							string statusDescription = "Chưa thanh toán";
							decimal totalAmount = 20;

							if (bookingDetailsResponse.Success && bookingDetailsResponse.Data != null)
							{
								var bookingData = JsonConvert.DeserializeObject<dynamic>(
									JsonConvert.SerializeObject(bookingDetailsResponse.Data));

								// Lấy thông tin từ booking details
								status = bookingData?.status ?? "Unpaid";
								statusId = bookingData?.statusId ?? 8;
								statusDescription = bookingData?.statusDescription ?? "Chưa thanh toán";
								totalAmount = bookingData?.totalPrice ?? 0;
							}

							await _notificationService.NotifyBookingCreated(request.FacilityId, new
							{
								bookingId = data.BookingId,
								facilityId = request.FacilityId,
								courtId = slot.CourtId,
								courtName = slot.CourtName,
								customerName = data.User?.Email?.Split('@')[0] ?? "Khách",
								customerEmail = data.User?.Email,
								customerPhone = data.User?.Phone,
								date = data.CheckInDate.ToString("dd/MM/yyyy"),
								checkInTime = Fmt(start),
								timeSlot = $"{Fmt(start)} - {Fmt(end)}",
								// ✅ FIX: Sử dụng Status từ database
								status = status,
								statusId = statusId,
								statusDescription = statusDescription,
								action = "created",
								totalAmount = totalAmount,
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

		[HttpPost("mark-smart-slot")]
		public async Task<IActionResult> MarkSmartSlot([FromBody] BookingRequestDto request)
		{
			try
			{
				var result = await _bookingService.MarkSmartSlot(request);

				if (result.Success)
				{
					var data = JsonConvert.DeserializeObject<dynamic>(
						JsonConvert.SerializeObject(result.Data));

					if (data?.slots != null)
					{
						string Fmt(string t) => string.IsNullOrWhiteSpace(t) ? "" : t.Substring(0, 5);

						foreach (var slot in data.slots)
						{
							var start = slot.startTime?.ToString();
							var end = slot.endTime?.ToString();

							// ✅ ENSURE: This notification is sent
							await _notificationService.NotifyBookingCreated(request.FacilityId, new
							{
								bookingId = data.bookingId,
								facilityId = request.FacilityId,
								courtId = slot.courtId,
								courtName = slot.courtName?.ToString(),
								customerName = data.user?.email?.ToString()?.Split('@')[0] ?? "Admin",
								customerEmail = data.user?.email?.ToString(),
								customerPhone = data.user?.phone?.ToString(),
								date = data.checkInDate?.ToString("dd/MM/yyyy"),
								checkInTime = Fmt(start),
								timeSlot = $"{Fmt(start)} - {Fmt(end)}",
								status = "Paid",
								statusId = 7,
								statusDescription = "Đã Cọc",
								action = "created",
								totalAmount = 0,
								timestamp = DateTime.UtcNow.ToString("o")
							});
						}
					}
				}

				return StatusCode(result.Status, result);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error in MarkSmartSlot: {ex.Message}");
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
					// ✅ FIX: Lấy thông tin booking để gửi notification đúng
					var bookingDetailsResponse = await _bookingService.GetByIdAsync(id);

					int facilityId = 0;
					if (bookingDetailsResponse.Success && bookingDetailsResponse.Data != null)
					{
						var bookingData = JsonConvert.DeserializeObject<dynamic>(
							JsonConvert.SerializeObject(bookingDetailsResponse.Data));
						facilityId = bookingData?.facilityId ?? 0;
					}

					await _notificationService.NotifyBookingCompleted(facilityId, new
					{
						bookingId = id,
						facilityId = facilityId,
						status = "Completed", // ✅ Status sau khi complete
						statusId = 10,        // ✅ StatusId cho Completed
						action = "completed",
						message = "Đơn đặt sân đã được hoàn thành",
						timestamp = DateTime.UtcNow.ToString("o")
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

		// ✅ DTO Classes - Không thay đổi vì không có TotalAmount field
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