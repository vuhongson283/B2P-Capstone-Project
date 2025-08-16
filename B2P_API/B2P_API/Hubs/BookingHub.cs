using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace B2P_API.Hubs
{
	public class BookingHub : Hub
	{
		// ✅ EXISTING: Facility group methods
		public async Task JoinFacilityGroup(int facilityId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"facility_{facilityId}");
			Console.WriteLine($"Client {Context.ConnectionId} joined facility group: {facilityId}");
		}

		public async Task LeaveFacilityGroup(int facilityId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"facility_{facilityId}");
			Console.WriteLine($"Client {Context.ConnectionId} left facility group: {facilityId}");
		}

		public async Task SendBookingUpdate(object notification)
		{
			await Clients.All.SendAsync("ReceiveBookingUpdate", notification);
			Console.WriteLine($"Booking update sent: {notification}");
		}

		// ✅ NEW: User group methods for comment notifications
		public async Task JoinUserGroup(string userId)
		{
			var groupName = $"user_{userId}";
			await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
			Console.WriteLine($"👤 Client {Context.ConnectionId} joined user group: {userId} (group: {groupName})");
		}

		public async Task LeaveUserGroup(string userId)
		{
			var groupName = $"user_{userId}";
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
			Console.WriteLine($"👤 Client {Context.ConnectionId} left user group: {userId} (group: {groupName})");
		}

		// ✅ NEW: Send comment notification method
		public async Task SendCommentNotification(object notification)
		{
			try
			{
				// Log để debug
				Console.WriteLine($"📤 Received comment notification request: {notification}");

				// Parse notification để lấy thông tin target user
				var notificationJson = notification.ToString();

				// Có thể parse JSON để lấy blogAuthorId, hoặc gửi tất cả để client filter
				await Clients.All.SendAsync("CommentNotification", notification);

				Console.WriteLine($"💬 Comment notification broadcasted successfully");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error sending comment notification: {ex.Message}");
			}
		}

		// ✅ EXISTING: Connection events
		public override async Task OnConnectedAsync()
		{
			Console.WriteLine($"Client connected: {Context.ConnectionId}");
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
			if (exception != null)
			{
				Console.WriteLine($"Disconnect reason: {exception.Message}");
			}
			await base.OnDisconnectedAsync(exception);
		}
	}
}