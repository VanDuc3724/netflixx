using Netflixx.Models;
using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class HomeViewModel
    {
        // the signed‐in user (for avatar URL, etc.)
        public AppUserModel CurrentUser { get; set; }

        // featured carousel
        public List<FilmsModel> CarouselFilms { get; set; }

        // based on watched
        public List<FilmsModel> RecommendedFilms { get; set; }

        // top rated
        public List<FilmsModel> TopRatedFilms { get; set; }

        public FilmsModel CheapestFilm { get; set; }
    }
}
