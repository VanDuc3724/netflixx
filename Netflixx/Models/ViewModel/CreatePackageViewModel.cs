using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class CreatePackageViewModel
    {
        public PackagesModel Package { get; set; }

        [Display(Name = "Films")]
        public List<int> SelectedFilmIds { get; set; } = new List<int>();

        public IEnumerable<FilmsModel> Films { get; set; } = new List<FilmsModel>();
    }
}
