using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class PackageCreateViewModel
    {
        [Required(ErrorMessage = "Tên gói không được để trống.")]
        public string Name { get; set; }
        public string? Description { get; set; }

        public List<int> SelectedFilmIds { get; set; } = new();
        public List<FilmsModel>? AvailableFilms { get; set; }
    }
}
