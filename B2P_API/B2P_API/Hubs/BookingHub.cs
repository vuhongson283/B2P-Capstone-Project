using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace B2P_API.Hubs
{
	public class BookingHub : Hub
	{
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

		public override async Task OnConnectedAsync()
		{
			Console.WriteLine($"Client connected: {Context.ConnectionId}");
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
			await base.OnDisconnectedAsync(exception);
		}
	}
}
