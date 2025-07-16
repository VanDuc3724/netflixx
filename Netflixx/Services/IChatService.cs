// Netflixx.Services/IChatService.cs
using Netflixx.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netflixx.Services
{
    /// <summary>
    /// Interface định nghĩa các dịch vụ logic nghiệp vụ cho tính năng chat.
    /// Tầng này đóng vai trò trung gian giữa Hub/Controller và Repository.
    /// </summary>
    public interface IChatService
    {
        // === CÁC DỊCH VỤ LIÊN QUAN ĐẾN USER ===

        /// <summary>
        /// Lấy thông tin của một người dùng bằng ID.
        /// </summary>
        Task<UserViewModel?> GetUserByIdAsync(string userId);

        /// <summary>
        /// Lấy danh sách tất cả người dùng có một vai trò cụ thể.
        /// </summary>
        Task<List<UserViewModel>> GetUsersByRoleAsync(string role);

        // === CÁC DỊCH VỤ LIÊN QUAN ĐẾN HỘI THOẠI VÀ TIN NHẮN ===

        /// <summary>
        /// Xử lý và lưu một tin nhắn mới.
        /// </summary>
        /// <param name="message">Model tin nhắn.</param>
        Task<MessageModel> SaveMessageAsync(MessageModel message);

        /// <summary>
        /// Lấy toàn bộ lịch sử tin nhắn của một luồng hội thoại.
        /// </summary>
        /// <param name="conversationId">ID của luồng hội thoại (chính là ID của User).</param>
        Task<List<MessageModel>> GetConversationHistoryAsync(string conversationId);

        /// <summary>
        /// Lấy danh sách tóm tắt tất cả các luồng hội thoại đang hoạt động cho Manager.
        /// </summary>
        Task<List<UserConversationInfo>> GetAllConversationsForManagerAsync();

        /// <summary>
        /// Đánh dấu các tin nhắn trong một luồng hội thoại là đã được User đọc.
        /// </summary>
        /// <param name="conversationId">ID của luồng hội thoại (chính là ID của User đang đọc).</param>
        Task MarkConversationAsReadByUserAsync(string conversationId);

        // Các phương thức sau đã bị loại bỏ vì không còn phù hợp với mô hình mới:
        // - GetManagerConversationsAsync
        // - GetUnassignedConversationsAsync
        // - AssignConversationToManagerAsync
        // - CloseConversationAsync
        // - MarkMessagesAsReadAsync (đã được thay bằng phiên bản mới rõ ràng hơn)
        Task HandleUserLogoutAsync(string userId);

    }
}