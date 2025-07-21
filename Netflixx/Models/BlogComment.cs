using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class BlogComment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(250, ErrorMessage = "Comment cannot exceed 250 characters.")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public string AuthorId { get; set; }
        public AppUserModel Author { get; set; }

        [Required]
        public int BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; }

        public int? ParentCommentId { get; set; }
        public BlogComment ParentComment { get; set; }
        public ICollection<BlogComment> Replies { get; set; } = new List<BlogComment>();
        public DateTime? LastUpdated { get; set; }
        [NotMapped]
        public bool IsEditing { get; set; }
    }
}