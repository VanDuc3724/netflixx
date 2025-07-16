// Controllers/ChatApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Services;
using System.Security.Claims;
using System.Collections.Generic; // Thêm using
using System.Threading.Tasks;    // Thêm using

[ApiController]
[Route("api/chat")]
[Authorize] // Bảo vệ toàn bộ API
public class ChatApiController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatApiController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("manager/conversations")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetManagerConversations()
    {
        var conversations = await _chatService.GetAllConversationsForManagerAsync();
        return Ok(conversations);
    }

    [HttpGet("history/{conversationId}")]
    public async Task<IActionResult> GetConversationHistory(string conversationId)
    {
        // Kiểm tra quyền (giữ nguyên)
        if (!User.IsInRole("Manager") && User.FindFirstValue(ClaimTypes.NameIdentifier) != conversationId)
        {
            return Forbid();
        }

        var history = await _chatService.GetConversationHistoryAsync(conversationId);

        // ===== BẮT ĐẦU SỬA ĐỔI =====
        // Chuyển đổi List<MessageModel> thành một danh sách các đối tượng DTO
        // chứa đầy đủ thông tin người gửi để nhất quán với dữ liệu từ SignalR.
        var messageDtos = new List<object>();

        foreach (var message in history)
        {
            var sender = await _chatService.GetUserByIdAsync(message.SenderId);
            // Bỏ qua nếu không tìm thấy người gửi để tránh lỗi
            if (sender == null) continue;

            messageDtos.Add(new
            {
                // Thuộc tính của tin nhắn
                Id = message.Id,
                SenderId = message.SenderId,
                ConversationId = message.ConversationId,
                Content = message.Content,
                Timestamp = message.Timestamp,
                IsRead = message.IsRead,
                MessageType = message.MessageType,

                // Thông tin bổ sung của người gửi
                SenderName = sender.Name,
                SenderAvatarUrl = sender.AvatarUrl,
                SenderRole = sender.Role, // <-- Đây là thông tin quan trọng cần thêm
            });
        }

        return Ok(messageDtos); // Trả về danh sách đã được làm giàu thông tin
        // ===== KẾT THÚC SỬA ĐỔI =====
    }
}