using Netflixx.Repositories;
using Microsoft.EntityFrameworkCore;
public class NotificationService : INotificationService
{
    private readonly DBContext _context;
    public NotificationService(DBContext context)
    {
        _context = context;
    }

    public async Task CreateNotificationAsync(string userId, string title, string message, int type)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            NotificationType = type
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}