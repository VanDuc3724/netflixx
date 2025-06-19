using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class FavoriteFilmsModel
    {
        [Key]
        public int Id { get; set; }

        [Required, ForeignKey(nameof(User))]
        public string UserID { get; set; }
        public AppUserModel User { get; set; }

        [Required, ForeignKey(nameof(Film))]
        public int FilmID { get; set; }
        public FilmsModel Film { get; set; }

        [Required]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
