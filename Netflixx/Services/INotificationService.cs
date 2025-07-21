public interface INotificationService
{
    Task CreateNotificationAsync(string userId, string title, string message, int type);
    Task<List<Notification>> GetUserNotificationsAsync(string userId);
    Task MarkAsReadAsync(int notificationId);
}