namespace Netflixx.Models
{
    public class AvatarViewModel
    {
        public string CurrentAvatar { get; set; }

        // Danh sách tên file của các ảnh đại diện mặc định (VD: "avatar1.png", "avatar2.png")
        public List<string> DefaultAvatars { get; set; } = new List<string>();

        // Danh sách tên file của các ảnh đã tải lên bởi người dùng (VD: "myphoto.jpg", "selfie.png")
        public List<string> UploadedAvatars { get; set; } = new List<string>();
    }
}
