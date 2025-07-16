// Models/ViewModel/FilmDetailViewModel.cs
using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class FilmDetailViewModel
    {
        // The film itself
        public FilmsModel Film { get; set; }

        public int FilmId { get; set; }

        // Comments & replies
        public IEnumerable<FilmComment> Comments { get; set; }

        // Recent films list
        public IEnumerable<FilmsModel> RecentFilms { get; set; }

        // (Optional) Which tab is active: "info" or "trailer"
        public string ActiveTab { get; set; } = "info";

        // Rating data
        /// <summary>Average rating on a 0–5 scale (e.g. 4.3)</summary>
        public double AverageRating { get; set; }

        /// <summary>Total number of ratings submitted</summary>
        public int RatingCount { get; set; }

        /// <summary>The current user’s own rating (1–10), or 0 if none yet</summary>
        public int UserRating { get; set; }

        // For posting a new comment

        public string NewCommentAuthor { get; set; }
        public string NewCommentContent { get; set; }

        // When replying to a comment, this holds the parent comment’s ID
        public int? ReplyToCommentId { get; set; }
    }
}