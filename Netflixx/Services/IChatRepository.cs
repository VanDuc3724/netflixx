// Netflixx.Services/IChatRepository.cs
using Netflixx.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netflixx.Services
{
    /// <summary>
    /// Interface định nghĩa các hoạt động kho lưu trữ cho tính năng chat.
    /// Logic được xây dựng xoay quanh khái niệm "Conversation" (luồng hội thoại),
    /// được định danh bằng ID của người dùng (User) đã khởi tạo nó.
    /// </summary>
    public interface IChatRepository
    {
        // === CÁC PHƯƠNG THỨC LIÊN QUAN ĐẾN USER (GIỮ NGUYÊN) ===

        /// <summary>
        /// Tìm thông tin của một người dùng (User hoặc Manager) bằng ID.
        /// </summary>
        Task<UserViewModel?> FindUserByIdAsync(string userId);

        /// <summary>
        /// Tìm tất cả người dùng có một vai trò (role) cụ thể.
        /// </summary>
        Task<List<UserViewModel>> FindUsersByRoleAsync(string role);


        // === CÁC PHƯƠNG THỨC LIÊN QUAN ĐẾN HỘI THOẠI VÀ TIN NHẮN (SỬA ĐỔI) ===

        /// <summary>
        /// Thêm một tin nhắn mới vào kho lưu trữ.
        /// </summary>
        /// <param name="message">Model tin nhắn đã có SenderId và ConversationId.</param>
        Task<MessageModel> AddMessageAsync(MessageModel message);

        /// <summary>
        /// Lấy toàn bộ lịch sử tin nhắn của một luồng hội thoại.
        /// </summary>
        /// <param name="conversationId">ID của luồng hội thoại (chính là ID của User).</param>
        Task<List<MessageModel>> GetConversationHistoryAsync(string conversationId);

        /// <summary>
        /// Lấy danh sách tóm tắt tất cả các luồng hội thoại đang hoạt động để hiển thị cho Manager.
        /// </summary>
        Task<List<UserConversationInfo>> GetAllActiveConversationsForManagerAsync();

        /// <summary>
        /// Đánh dấu tất cả các tin nhắn do Manager gửi trong một luồng hội thoại là đã được User đọc.
        /// </summary>
        /// <param name="conversationId">ID của luồng hội thoại (chính là ID của User đang đọc).</param>
        /// 

        Task DeleteConversationAsync(string conversationId);

        Task MarkMessagesAsReadByUserAsync(string conversationId);

        // Các phương thức sau đã bị loại bỏ vì không còn phù hợp với mô hình mới:
        // - GetManagerConversationsAsync
        // - GetUnassignedConversationsAsync
        // - AssignConversationToManagerAsync
        // - CloseConversationAsync
        // - MarkMessagesAsReadAsync (đã được thay bằng phiên bản mới rõ ràng hơn)
    }
}