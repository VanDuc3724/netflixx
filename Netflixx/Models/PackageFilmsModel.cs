using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PackageFilmsModel
    {
        [ForeignKey("Package")]
        public int PackageID { get; set; }
        public PackagesModel Package { get; set; }

        [ForeignKey("Film")]
        public int FilmID { get; set; }
        public FilmsModel Film { get; set; }
    }
}