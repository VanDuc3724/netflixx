using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PackagesModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên không được để trống.")]
        public string Name { get; set; }
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<PackageChannelsModel> PackageChannels { get; set; }
        public ICollection<PackageFilmsModel> PackageFilms { get; set; }

        public ICollection<PromotionPackagesModel> PromotionPackages { get; set; }

        public ICollection<PackageSubscriptionsModel> Subscriptions { get; set; }
    }
}
