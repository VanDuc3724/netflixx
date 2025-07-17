using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PromotionPackagesModel
    {
        [ForeignKey("Promotion")]
        public int PromotionID { get; set; }
        public PromotionsModel Promotion { get; set; }

        [ForeignKey("Package")]
        public int PackageID { get; set; }
        public PackagesModel Package { get; set; }
    }
}