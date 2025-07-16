using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public enum BlogPostStatus
    {
        Draft,      // Bản nháp
        Published   // Đã đăng
    }
    public class BlogPost
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastUpdated { get; set; }

        public string? AuthorId { get; set; }
        public AppUserModel? Author { get; set; }

        [NotMapped] // Không lưu vào database, chỉ để hiển thị
        public string? AuthorName => Author?.UserName;

        [Required]
        public int? FilmId { get; set; }

        [ForeignKey("FilmId")]
        public virtual FilmsModel? Film { get; set; }

        [Required]
        public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft; // Mặc định là Draft
    }
}