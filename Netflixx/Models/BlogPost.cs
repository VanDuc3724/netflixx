using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models

{

    public class BlogPost

    {

        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastUpdated { get; set; }

        public string? AuthorId { get; set; } // If we want to link to the user

        // Optionally, we can add an Author navigation property

        public AppUserModel? Author { get; set; }

        // Add this foreign key relationship
        public int? FilmId { get; set; }

        // Add this navigation property
        [ForeignKey("FilmId")]
        public virtual FilmsModel? Film { get; set; }
    }

}