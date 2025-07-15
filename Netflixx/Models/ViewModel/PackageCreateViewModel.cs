using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class PackageCreateViewModel
    {
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<int> SelectedFilmIds { get; set; } = new();
        public List<FilmOption> AvailableFilms { get; set; } = new();
    }

    public class FilmOption
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}
