using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Netflixx.Models;
using Netflixx.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Netflixx.Repositories
{
    public class ChatRepository : IChatRepository
    {
        // === IN-MEMORY STORAGE (Đã chuyển sang Dictionary + Queue) ===
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<MessageModel>> _conversations = new();
        private const int MaxMessagesPerConversation = 50; // Giới hạn số tin nhắn
        private static int _messageIdCounter = 0;

        // === DATABASE ACCESS ===
        private readonly IServiceScopeFactory _scopeFactory;
        public ChatRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // === USER METHODS (using DATABASE) - Không thay đổi ===
        public async Task<UserViewModel?> FindUserByIdAsync(string userId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUserModel>>();
                var appUser = await userManager.FindByIdAsync(userId);
                if (appUser == null) return null;
                var roles = await userManager.GetRolesAsync(appUser);
                return new UserViewModel
                {
                    Id = appUser.Id,
                    Name = appUser.UserName!,
                    AvatarUrl = appUser.AvatarUrl,
                    Role = roles.FirstOrDefault() ?? "User"
                };
            }
        }

        public async Task<List<UserViewModel>> FindUsersByRoleAsync(string role)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUserModel>>();
                var usersInRole = await userManager.GetUsersInRoleAsync(role);
                return usersInRole.Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Name = u.UserName!,
                    AvatarUrl = u.AvatarUrl,
                    Role = role
                }).ToList();
            }
        }

        // === MESSAGE & CONVERSATION METHODS - Đã được cập nhật và sửa lỗi ===

        /// <summary>
        /// Thêm một tin nhắn mới vào kho lưu trữ, đồng thời đảm bảo giới hạn số lượng tin nhắn.
        /// </summary>
        // Repositories/ChatRepository.cs
        public async Task<MessageModel> AddMessageAsync(MessageModel message)
        {
            message.Id = Interlocked.Increment(ref _messageIdCounter);
            message.Timestamp = DateTime.UtcNow;
            message.IsRead = false;

            var messageQueue = _conversations.GetOrAdd(message.ConversationId, _ => new ConcurrentQueue<MessageModel>());
            messageQueue.Enqueue(message);

            while (messageQueue.Count > MaxMessagesPerConversation && messageQueue.TryDequeue(out MessageModel oldestMessage))
            {
                if (oldestMessage.MessageType == "image" && !string.IsNullOrEmpty(oldestMessage.Content))
                {
                    // ***** SỬA LỖI Ở ĐÂY *****
                    await DeleteBlobAsync(oldestMessage.Content);
                }
            }
            return message;
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử của một luồng hội thoại.
        /// </summary>
        public Task<List<MessageModel>> GetConversationHistoryAsync(string conversationId)
        {
            if (_conversations.TryGetValue(conversationId, out var messageQueue))
            {
                // Trả về danh sách tin nhắn đã được sắp xếp theo thời gian
                return Task.FromResult(messageQueue.OrderBy(m => m.Timestamp).ToList());
            }
            // Trả về danh sách rỗng nếu không có cuộc hội thoại nào
            return Task.FromResult(new List<MessageModel>());
        }

        /// <summary>
        /// Lấy danh sách tóm tắt tất cả các luồng hội thoại đang hoạt động để hiển thị cho Manager.
        /// </summary>
        public async Task<List<UserConversationInfo>> GetAllActiveConversationsForManagerAsync()
        {
            var conversationInfos = new List<UserConversationInfo>();

            foreach (var kvp in _conversations)
            {
                var userId = kvp.Key;
                var messageQueue = kvp.Value;

                if (messageQueue.IsEmpty) continue;

                var user = await FindUserByIdAsync(userId);
                if (user == null || user.Role != "User") continue;

                var lastMessage = messageQueue.OrderByDescending(m => m.Timestamp).FirstOrDefault();

                if (lastMessage != null)
                {
                    var lastSender = await FindUserByIdAsync(lastMessage.SenderId);
                    var hasUnreadMessagesForManager = lastSender?.Role == "User";

                    conversationInfos.Add(new UserConversationInfo
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        UserAvatarUrl = user.AvatarUrl,
                        LastMessageContent = lastMessage.Content,
                        LastMessageTimestamp = lastMessage.Timestamp,
                        HasUnreadMessages = hasUnreadMessagesForManager
                    });
                }
            }

            return conversationInfos
                .OrderByDescending(c => c.HasUnreadMessages)
                .ThenByDescending(c => c.LastMessageTimestamp)
                .ToList();
        }

        /// <summary>
        /// Xóa toàn bộ cuộc hội thoại của một người dùng.
        /// </summary>
        public async Task DeleteConversationAsync(string conversationId)
        {
            if (_conversations.TryRemove(conversationId, out var messageQueue))
            {
                // Nếu xóa thành công, duyệt qua tất cả tin nhắn trong đó để xóa ảnh
                foreach (var message in messageQueue)
                {
                    if (message.MessageType == "image" && !string.IsNullOrEmpty(message.Content))
                    {
                        await DeleteBlobAsync(message.Content);
                    }
                }
            }
        }

        /// <summary>
        /// Đánh dấu các tin nhắn do Manager gửi trong một cuộc hội thoại là đã được User đọc.
        /// </summary>
        public Task MarkMessagesAsReadByUserAsync(string conversationId)
        {
            // Bước 1: Lấy đúng hàng đợi tin nhắn của cuộc hội thoại này
            if (_conversations.TryGetValue(conversationId, out var messageQueue))
            {
                // Bước 2: Duyệt qua các tin nhắn trong hàng đợi đó
                // Không cần ToList() vì ta đang duyệt trên một bản sao của các tham chiếu.
                // Việc thay đổi thuộc tính `IsRead` của message không làm thay đổi cấu trúc của `ConcurrentQueue`.
                foreach (var msg in messageQueue)
                {
                    // Đánh dấu đã đọc nếu tin nhắn chưa được đọc VÀ người gửi không phải là người dùng hiện tại (tức là do manager gửi)
                    if (!msg.IsRead && msg.SenderId != conversationId)
                    {
                        msg.IsRead = true;
                    }
                }
            }

            // Task.CompletedTask báo hiệu rằng công việc đã hoàn thành (không có gì để trả về).
            return Task.CompletedTask;
        }
        private async Task DeleteBlobAsync(string fileUrl)
        {
            try
            {
                // Tách tên file từ URL đầy đủ
                var uri = new Uri(fileUrl);
                var fileName = Path.GetFileName(uri.LocalPath);

                // Tạo một scope để lấy IFileStorageService
                using (var scope = _scopeFactory.CreateScope())
                {
                    var storageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                    await storageService.DeleteAsync(fileName);
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi, nhưng không để nó làm sập chương trình
                // _logger.LogError(ex, "Failed to delete blob at {Url}", fileUrl);
            }
        }
    }
}