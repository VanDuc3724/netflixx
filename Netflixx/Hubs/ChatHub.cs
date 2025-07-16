// Hubs/ChatHub.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Netflixx.Models;
using Netflixx.Services;
using System;
using System.Security.Claims; // Thêm using này nếu chưa có
using System.Threading.Tasks;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;
    private const string ManagerRole = "Manager";
    private const string ManagerGroup = "Managers";

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Được gọi khi một client kết nối tới Hub.
    /// Chúng ta sẽ thêm các Manager vào một group đặc biệt.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Connection {ConnectionId} attempted without a user identifier. Disconnecting.", Context.ConnectionId);
            Context.Abort();
            return;
        }

        if (Context.User.IsInRole(ManagerRole))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ManagerGroup);
            _logger.LogInformation("Manager {UserIdentifier} connected and added to group '{ManagerGroup}'. ConnectionId: {ConnectionId}",
                userId, ManagerGroup, Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("User {UserIdentifier} connected. ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Gửi một tin nhắn mới.
    /// Phương thức này được sử dụng bởi CẢ User và Manager.
    /// Nó hỗ trợ một tempId để client có thể theo dõi và cập nhật tin nhắn tạm thời (Optimistic UI).
    /// </summary>
    /// <param name="conversationId">ID của cuộc hội thoại (chính là ID của User).</param>
    /// <param name="content">Nội dung tin nhắn (text hoặc URL ảnh).</param>
    /// <param name="messageType">Loại tin nhắn ('text' hoặc 'image').</param>
    /// <param name="tempId">ID tạm thời được tạo ra bởi client, có thể null.</param>
    public async Task SendMessage(string conversationId, string content, string messageType = "text", string? tempId = null)
    {
        var senderId = Context.UserIdentifier;
        if (senderId == null) return; // Bảo vệ, mặc dù [Authorize] đã làm điều này.

        var sender = await _chatService.GetUserByIdAsync(senderId);
        if (sender == null)
        {
            _logger.LogWarning("Message not sent. Sender with ID {SenderId} not found.", senderId);
            return;
        }

        // Kiểm tra quyền
        if (sender.Role != ManagerRole && sender.Id != conversationId)
        {
            _logger.LogWarning("Security violation: User {SenderId} tried to send message to another user's conversation {ConversationId}",
                senderId, conversationId);
            return;
        }

        var message = new MessageModel
        {
            SenderId = senderId,
            ConversationId = conversationId,
            Content = content,
            MessageType = messageType,
        };

        var savedMessage = await _chatService.SaveMessageAsync(message);

        // Chuẩn bị DTO để gửi về client, bao gồm cả các thông tin bổ sung
        var messageDto = new
        {
            // Thuộc tính của tin nhắn đã lưu
            Id = savedMessage.Id,
            SenderId = savedMessage.SenderId,
            ConversationId = savedMessage.ConversationId,
            Content = savedMessage.Content, // Gửi về content đã lưu
            Timestamp = savedMessage.Timestamp,
            IsRead = savedMessage.IsRead,
            MessageType = savedMessage.MessageType,

            // Thông tin người gửi để client không phải gọi API thêm
            SenderName = sender.Name,
            SenderAvatarUrl = sender.AvatarUrl,
            SenderRole = sender.Role,

            // Quan trọng: Gửi lại tempId mà client đã cung cấp để client có thể cập nhật UI
            TempId = tempId
        };

        // Gửi tin nhắn đến người dùng cụ thể và tất cả manager
        await Clients.User(conversationId).SendAsync("ReceiveMessage", messageDto);
        await Clients.Group(ManagerGroup).SendAsync("ReceiveMessage", messageDto);

        _logger.LogInformation("Message with ID {MessageId} (TempId: {TempId}) sent by {SenderId}", savedMessage.Id, tempId ?? "N/A", senderId);
    }

    /// <summary>
    /// Phương thức được client (chỉ User) gọi khi họ mở cửa sổ chat,
    /// để đánh dấu tất cả tin nhắn trong đó là đã đọc.
    /// </summary>
    public async Task MarkConversationAsRead(string conversationId)
    {
        var userId = Context.UserIdentifier;

        // Chỉ user mới có thể đánh dấu cuộc hội thoại của chính mình là đã đọc.
        if (userId != conversationId)
        {
            _logger.LogWarning("Security violation: User {UserId} tried to mark conversation {ConversationId} as read.",
                userId, conversationId);
            return;
        }

        await _chatService.MarkConversationAsReadByUserAsync(conversationId);

        // Thông báo cho tất cả các manager rằng user đã đọc tin nhắn
        await Clients.Group(ManagerGroup).SendAsync("ConversationWasRead", conversationId);

        _logger.LogInformation("Conversation {ConversationId} marked as read by user.", conversationId);
    }


    /// <summary>
    /// Được gọi khi một client ngắt kết nối.
    /// Xóa Manager ra khỏi group.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;

        if (Context.User.IsInRole(ManagerRole))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ManagerGroup);
            _logger.LogInformation("Manager {UserIdentifier} disconnected and removed from group '{ManagerGroup}'. Reason: {Exception}",
               userId, ManagerGroup, exception?.Message ?? "Normal disconnect.");
        }
        else
        {
            _logger.LogInformation("User {UserIdentifier} disconnected. Reason: {Exception}",
               userId, exception?.Message ?? "Normal disconnect.");
        }

        await base.OnDisconnectedAsync(exception);
    }
}