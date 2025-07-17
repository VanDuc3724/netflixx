using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class CreatePackageViewModel
    {
        public PackagesModel Package { get; set; }

        [Display(Name = "Film")]
        public int? SelectedFilmId { get; set; }

        public IEnumerable<FilmsModel> Films { get; set; } = new List<FilmsModel>();
    }
}
