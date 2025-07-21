using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class Feedback
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Phản hồi")]
        public string? Response { get; set; }
    }
}