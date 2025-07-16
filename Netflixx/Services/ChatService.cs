// Netflixx.Services/ChatService.cs
using Netflixx.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netflixx.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _repo;

        // Dependency Injection của IChatRepository
        public ChatService(IChatRepository repo)
        {
            _repo = repo;
        }

        // --- Triển khai các phương thức từ IChatService mới ---

        public Task<UserViewModel?> GetUserByIdAsync(string userId)
        {
            return _repo.FindUserByIdAsync(userId);
        }

        public Task<List<UserViewModel>> GetUsersByRoleAsync(string role)
        {
            return _repo.FindUsersByRoleAsync(role);
        }

        public Task<MessageModel> SaveMessageAsync(MessageModel message)
        {
            // Trong tương lai, bạn có thể thêm logic nghiệp vụ ở đây
            // Ví dụ: kiểm tra nội dung tin nhắn, thêm filter,...
            // Hiện tại, chúng ta chỉ gọi thẳng xuống repository.
            return _repo.AddMessageAsync(message);
        }

        public Task<List<MessageModel>> GetConversationHistoryAsync(string conversationId)
        {
            return _repo.GetConversationHistoryAsync(conversationId);
        }

        public Task<List<UserConversationInfo>> GetAllConversationsForManagerAsync()
        {
            return _repo.GetAllActiveConversationsForManagerAsync();
        }

        public Task MarkConversationAsReadByUserAsync(string conversationId)
        {
            return _repo.MarkMessagesAsReadByUserAsync(conversationId);
        }
        public Task HandleUserLogoutAsync(string userId)
        {
            // userId trong trường hợp này chính là conversationId
            return _repo.DeleteConversationAsync(userId);
        }
    }
}