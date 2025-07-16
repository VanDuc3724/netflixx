// Models/UserConversationInfo.cs
using System;
using System.Collections.Generic;
using Netflixx.Models; // ĐẢM BẢO DÒNG NÀY ĐỂ CÓ THỂ SỬ DỤNG UserViewModel VÀ MessageModel

namespace Netflixx.Models // Đảm bảo namespace này khớp với các models khác của bạn
{
    public class UserConversationInfo
    {
        public string UserId { get; set; } = null!;         // Luôn là user cuối
        public string UserName { get; set; } = null!;
        public string UserAvatarUrl { get; set; } = null!;
        public bool IsAssigned { get; set; }
        public string? AssignedToManagerId { get; set; }
        public string? AssignedToManagerName { get; set; }
        public bool HasUnreadMessages { get; set; }
        public string LastMessageContent { get; set; } = null!;
        public DateTime LastMessageTimestamp { get; set; }
        public List<MessageModel> Messages { get; set; } = new();
        public bool IsClosed { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}