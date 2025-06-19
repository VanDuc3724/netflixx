// Models/FilmRating.cs
using Netflixx.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class FilmRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int FilmId { get; set; }

        [Range(1, 10)]
        public int RatingValue { get; set; }

        public DateTime RatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public AppUserModel User { get; set; }

        [ForeignKey("FilmId")]
        public FilmsModel Film { get; set; }
    }
}