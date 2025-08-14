using Microsoft.AspNetCore.SignalR;
using B2P_API.Hubs;

namespace B2P_API.Services
{
	public interface IBookingNotificationService
	{
		Task NotifyBookingCreated(int facilityId, object bookingData);
		Task NotifyBookingUpdated(int facilityId, object bookingData);
		Task NotifyBookingCompleted(int facilityId, object bookingData);
	}

	public class BookingNotificationService : IBookingNotificationService
	{
		private readonly IHubContext<BookingHub> _hubContext;

		public BookingNotificationService(IHubContext<BookingHub> hubContext)
		{
			_hubContext = hubContext;
		}

		public async Task NotifyBookingCreated(int facilityId, object bookingData)
		{
			Console.WriteLine($"🔔 Sending BookingCreated notification to facility {facilityId}");

			// FIX: Chỉ gửi đến facility group cụ thể thôi
			await _hubContext.Clients.Group($"facility_{facilityId}")
				.SendAsync("BookingCreated", bookingData);

			// BỎ DÒNG NÀY: await _hubContext.Clients.All.SendAsync("BookingCreated", bookingData);
		}

		public async Task NotifyBookingUpdated(int facilityId, object bookingData)
		{
			Console.WriteLine($"🔔 Sending BookingUpdated notification to facility {facilityId}");

			// FIX: Chỉ gửi đến facility group cụ thể thôi
			await _hubContext.Clients.Group($"facility_{facilityId}")
				.SendAsync("BookingUpdated", bookingData);

			// BỎ DÒNG NÀY: await _hubContext.Clients.All.SendAsync("BookingUpdated", bookingData);
		}

		public async Task NotifyBookingCompleted(int facilityId, object bookingData)
		{
			Console.WriteLine($"🔔 Sending BookingCompleted notification to facility {facilityId}");

			// FIX: Chỉ gửi đến facility group cụ thể thôi
			await _hubContext.Clients.Group($"facility_{facilityId}")
				.SendAsync("BookingCompleted", bookingData);

			// BỎ DÒNG NÀY: await _hubContext.Clients.All.SendAsync("BookingCompleted", bookingData);
		}
	}
}