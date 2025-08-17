using Microsoft.AspNetCore.SignalR;
using B2P_API.Hubs;
using System.Text.Json;

namespace B2P_API.Services
{
	public interface IBookingNotificationService
	{
		// ✅ EXISTING: Booking methods
		Task NotifyBookingCreated(int facilityId, object bookingData);
		Task NotifyBookingUpdated(int facilityId, object bookingData);
		Task NotifyBookingCompleted(int facilityId, object bookingData);

		// ✅ NEW: Comment notification methods
		Task NotifyCommentCreated(int blogAuthorId, object commentData);
		Task NotifyCommentReply(int blogAuthorId, object replyData);
		Task SendCommentNotification(object notificationData);
	}

	public class BookingNotificationService : IBookingNotificationService
	{
		private readonly IHubContext<BookingHub> _hubContext;

		public BookingNotificationService(IHubContext<BookingHub> hubContext)
		{
			_hubContext = hubContext;
		}

		// ✅ EXISTING: Booking notification methods
		public async Task NotifyBookingCreated(int facilityId, object bookingData)
		{
			Console.WriteLine($"🔔 Sending BookingCreated notification to facility {facilityId}");
			await _hubContext.Clients.Group($"facility_{facilityId}")
				.SendAsync("BookingCreated", bookingData);
		}

		public async Task NotifyBookingUpdated(int facilityId, object bookingData)
		{
			Console.WriteLine($"🔔 Sending BookingUpdated notification to facility {facilityId}");
			await _hubContext.Clients.Group($"facility_{facilityId}")
				.SendAsync("BookingUpdated", bookingData);
		}

		public async Task NotifyBookingCompleted(int facilityId, object bookingData)
		{
			Console.WriteLine($"🔔 Sending BookingCompleted notification to facility {facilityId}");
			await _hubContext.Clients.Group($"facility_{facilityId}")
				.SendAsync("BookingCompleted", bookingData);
		}

		// ✅ NEW: Comment notification methods
		public async Task NotifyCommentCreated(int blogAuthorId, object commentData)
		{
			try
			{
				Console.WriteLine($"💬 Sending CommentCreated notification to user {blogAuthorId}");

				// Gửi đến user group cụ thể
				await _hubContext.Clients.Group($"user_{blogAuthorId}")
					.SendAsync("NewComment", commentData);

				Console.WriteLine($"✅ Comment notification sent to user_{blogAuthorId}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error sending comment notification: {ex.Message}");
			}
		}

		public async Task NotifyCommentReply(int blogAuthorId, object replyData)
		{
			try
			{
				Console.WriteLine($"↩️ Sending CommentReply notification to user {blogAuthorId}");

				// Gửi đến user group cụ thể
				await _hubContext.Clients.Group($"user_{blogAuthorId}")
					.SendAsync("CommentReply", replyData);

				Console.WriteLine($"✅ Reply notification sent to user_{blogAuthorId}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error sending reply notification: {ex.Message}");
			}
		}

		public async Task SendCommentNotification(object notificationData)
		{
			try
			{
				Console.WriteLine($"🔔 Broadcasting comment notification: {JsonSerializer.Serialize(notificationData)}");

				// Broadcast đến tất cả clients, client sẽ filter
				await _hubContext.Clients.All.SendAsync("CommentNotification", notificationData);

				Console.WriteLine($"✅ Comment notification broadcasted successfully");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error broadcasting comment notification: {ex.Message}");
			}
		}
	}
}