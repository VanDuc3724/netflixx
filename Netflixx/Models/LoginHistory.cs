using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class LoginHistory
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }   // liên kết với bảng AppUserModel

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        public DateTime logoutTime { get; set; } = DateTime.UtcNow;
        public string IPAddress { get; set; }

        public string UserAgent { get; set; }


        public virtual AppUserModel User { get; set; }
    }
}
