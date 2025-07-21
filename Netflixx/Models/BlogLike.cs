using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class BlogLike
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public AppUserModel User { get; set; }

        [Required]
        public int BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; }

        public bool IsLike { get; set; } // true = like, false = dislike
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}