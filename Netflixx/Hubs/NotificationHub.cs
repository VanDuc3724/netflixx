// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;

namespace Netflixx.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinNotificationGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendNotificationToUser(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        public async Task SendNotificationToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveNotification", message);
        }
    }
}