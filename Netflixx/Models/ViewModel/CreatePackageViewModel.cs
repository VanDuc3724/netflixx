using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class CreatePackageViewModel
    {
        public PackagesModel Package { get; set; }

        [Display(Name = "Film")]
        public ICollection<int>? SelectedFilmIds { get; set; }

        public IEnumerable<FilmsModel> Films { get; set; } = new List<FilmsModel>();
    }
}
