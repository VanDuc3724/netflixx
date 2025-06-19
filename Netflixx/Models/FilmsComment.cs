// Models/FilmComment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class FilmComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FilmId { get; set; }

        [Required]
        public string? AuthorName { get; set; }

        [Required]
        public string? Content { get; set; }

        /// <summary>
        /// 1 = top‑level comment, 2 = reply
        /// </summary>
        public int? Level { get; set; } = 1;

        /// <summary>
        /// If Level==2, points at parent comment
        /// </summary>
        public int? ParentCommentId { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("FilmId")]
        public FilmsModel Film { get; set; }
    }
}
