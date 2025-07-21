// Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Netflixx.Hubs;
using Netflixx.Services;
using System.Security.Claims;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationsController(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }

    [HttpPost("mark-as-read/{id}")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        await _notificationService.CreateNotificationAsync(
            request.UserId,
            request.Title,
            request.Message,
            request.Type
        );

        await _hubContext.Clients.User(request.UserId).SendAsync(
            "ReceiveNotification",
            new { Title = request.Title, Message = request.Message }
        );

        return Ok();
    }
}

public class NotificationRequest
{
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public int Type { get; set; }
}