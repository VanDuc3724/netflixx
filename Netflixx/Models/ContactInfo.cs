using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class ContactInfo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Giờ làm việc là bắt buộc")]
        public string BusinessHours { get; set; }

        [Required(ErrorMessage = "Đường dẫn bản đồ là bắt buộc")]
        [Url(ErrorMessage = "Đường dẫn không hợp lệ")]
        public string MapEmbedUrl { get; set; }
    }
}