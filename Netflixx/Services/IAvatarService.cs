using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Netflixx.Services
{
    public interface IAvatarService
    {
        /// <summary>
        /// Lấy danh sách path ảnh mặc định
        /// </summary>
        List<string> GetDefaultAvatars();

        /// <summary>
        /// Lấy danh sách ảnh đã upload của user hiện tại
        /// </summary>
        Task<List<string>> GetUploadedAvatarsAsync(string userId);

        /// <summary>
        /// Xử lý tải file mới hoặc chọn avatar có sẵn
        /// </summary>
        Task<(bool Success, string Message, string RelativePath, string FileName)>
            UploadOrSelectAvatarAsync(IFormFile avatarFile, string selectedFileName, string userId);
    }

    public class AvatarService : IAvatarService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AvatarService> _logger;
        private const string AVATAR_FOLDER = "img/avatar";
        private const string DEFAULT_SUB = "default";
        private const string UPLOAD_SUB = "upload";

        public AvatarService(IWebHostEnvironment env, ILogger<AvatarService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public List<string> GetDefaultAvatars()
        {
            var physical = Path.Combine(_env.WebRootPath, AVATAR_FOLDER, DEFAULT_SUB);
            if (!Directory.Exists(physical)) return new List<string>();

            return Directory.GetFiles(physical)
                .Select(f => UrlContent(DEFAULT_SUB, Path.GetFileName(f)))
                .ToList();
        }

        public async Task<List<string>> GetUploadedAvatarsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUploadedAvatarsAsync called with null or empty userId.");
                return new List<string>();
            }

            var userUploadSubfolder = Path.Combine(UPLOAD_SUB, userId); // ví dụ: "upload/ADMIN_USER_ID"
            var userPhysicalUploadPath = Path.Combine(_env.WebRootPath, AVATAR_FOLDER, userUploadSubfolder);

            if (!Directory.Exists(userPhysicalUploadPath)) return new List<string>();

            return await Task.Run(() =>
                Directory.GetFiles(userPhysicalUploadPath)
                         .Select(f => UrlContent(userUploadSubfolder, Path.GetFileName(f))) // vd: /img/avatar/upload/ADMIN_USER_ID/file.png
                         .ToList()
            );
        }

        public async Task<(bool Success, string Message, string RelativePath, string FileName)>
    UploadOrSelectAvatarAsync(IFormFile avatarFile, string selectedFileName, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) // userId này là của Admin
                {
                    _logger.LogWarning("UploadOrSelectAvatarAsync called with null or empty userId (Admin).");
                    return (false, "Lỗi xác thực người quản trị.", null, null);
                }

                string finalFileName;
                string relativePathToServe;
                string subFolderUsed;

                // TH1: Admin tải file mới lên (sẽ lưu vào kho của Admin)
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    // ... (validation file size, type - giữ nguyên) ...
                    if (avatarFile.Length > 5 * 1024 * 1024) return (false, "File quá lớn (tối đa 5MB).", null, null);
                    var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!allowed.Contains(ext)) return (false, "Chỉ chấp nhận file ảnh JPG, PNG, GIF.", null, null);

                    subFolderUsed = Path.Combine(UPLOAD_SUB, userId); // Thư mục upload của Admin: "upload/ADMIN_USER_ID"
                    var adminPhysicalUploadDir = Path.Combine(_env.WebRootPath, AVATAR_FOLDER, subFolderUsed);
                    if (!Directory.Exists(adminPhysicalUploadDir)) Directory.CreateDirectory(adminPhysicalUploadDir);

                    finalFileName = $"{Guid.NewGuid()}{ext}";
                    var physicalPath = Path.Combine(adminPhysicalUploadDir, finalFileName);

                    using (var fs = new FileStream(physicalPath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(fs);
                    }
                    relativePathToServe = UrlContent(subFolderUsed, finalFileName);
                    _logger.LogInformation("Admin {AdminUserId} uploaded new file to their avatar storage: {File}, relative path {Path}", userId, finalFileName, relativePathToServe);
                }
                // TH2: Admin chọn file có sẵn (từ default hoặc từ kho của Admin)
                else if (!string.IsNullOrEmpty(selectedFileName))
                {
                    var sanitizedFileName = Path.GetFileName(selectedFileName);

                    // Kiểm tra trong thư mục default trước
                    var defaultDir = Path.Combine(_env.WebRootPath, AVATAR_FOLDER, DEFAULT_SUB);
                    var defaultFilePath = Path.Combine(defaultDir, sanitizedFileName);

                    if (System.IO.File.Exists(defaultFilePath) && Path.GetFullPath(defaultFilePath).StartsWith(Path.GetFullPath(defaultDir)))
                    {
                        finalFileName = sanitizedFileName;
                        subFolderUsed = DEFAULT_SUB; // Là ảnh default
                        relativePathToServe = UrlContent(subFolderUsed, finalFileName);
                        _logger.LogInformation("Admin {AdminUserId} selected a default avatar: {File}, relative path {Path}", userId, finalFileName, relativePathToServe);
                    }
                    else // Kiểm tra trong thư mục upload của Admin
                    {
                        subFolderUsed = Path.Combine(UPLOAD_SUB, userId); // Thư mục của Admin
                        var adminPhysicalUploadDir = Path.Combine(_env.WebRootPath, AVATAR_FOLDER, subFolderUsed);
                        var adminFilePath = Path.Combine(adminPhysicalUploadDir, sanitizedFileName);

                        if (System.IO.File.Exists(adminFilePath) && Path.GetFullPath(adminFilePath).StartsWith(Path.GetFullPath(adminPhysicalUploadDir)))
                        {
                            finalFileName = sanitizedFileName;
                            // subFolderUsed đã là "upload/ADMIN_USER_ID"
                            relativePathToServe = UrlContent(subFolderUsed, finalFileName);
                            _logger.LogInformation("Admin {AdminUserId} selected an existing avatar from their storage: {File}, relative path {Path}", userId, finalFileName, relativePathToServe);
                        }
                        else
                        {
                            _logger.LogWarning("Admin {AdminUserId} tried to select non-existent file: {File}", userId, sanitizedFileName);
                            return (false, "Ảnh được chọn không tồn tại trong kho ảnh mặc định hoặc kho của bạn.", null, null);
                        }
                    }
                }
                else
                {
                    return (false, "Không có ảnh nào được chọn hoặc tải lên.", null, null);
                }

                // Không cập nhật DB cho admin ở đây. Chỉ trả về thông tin file.
                return (true, "Ảnh đã được xử lý thành công!", relativePathToServe, finalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình xử lý avatar (context: Admin creating user, AdminID: {AdminId})", userId);
                return (false, "Lỗi hệ thống khi xử lý avatar.", null, null);
            }
        }
        // Helper tạo đường dẫn Url
        private string UrlContent(string subfolder, string fileName)
    => $"/{AVATAR_FOLDER}/{subfolder}/{fileName}";
    }
}

