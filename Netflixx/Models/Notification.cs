public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; }  // Liên kết với User
    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int NotificationType { get; set; } // 1: System, 2: Message, 3: Film Update...
}