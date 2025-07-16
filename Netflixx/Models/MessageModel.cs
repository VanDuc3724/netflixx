// Netflixx.Models/MessageModel.cs
using System;

namespace Netflixx.Models
{
    public class MessageModel
    {
        public int Id { get; set; }

        /// <summary>
        /// ID của người đã gửi tin nhắn (có thể là User hoặc Manager).
        /// Thuộc tính này sẽ KHÔNG BAO GIỜ null.
        /// </summary>
        public string SenderId { get; set; } = null!;

        /// <summary>
        /// ID của luồng hội thoại.
        /// Đây chính là ID của người dùng (User) đã bắt đầu cuộc chat.
        /// Dùng để nhóm tất cả tin nhắn liên quan đến một người dùng lại với nhau.
        /// Thuộc tính này sẽ KHÔNG BAO GIỜ null.
        /// </summary>
        public string ConversationId { get; set; } = null!;

        public string Content { get; set; } = null!;
        public string MessageType { get; set; } = "text";

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Cờ này chỉ cho biết người dùng (User) đã đọc tin nhắn của manager hay chưa.
        /// Khi User mở cửa sổ chat, các tin nhắn do Manager gửi sẽ được đánh dấu là đã đọc.
        /// </summary>
        public bool IsRead { get; set; }
    }
}