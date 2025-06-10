using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Netflixx.Models
{
    public class ChannelsModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên không được để trống.")]
        public string Name { get; set; }
        public string? Description { get; set; }

        [Required(ErrorMessage = "Thể loại không được để trống.")]
        public string Category { get; set; }

        [InverseProperty("Channel")] // Liên kết ngược với PackageChannelsModel
        public ICollection<PackageChannelsModel> PackageChannels { get; set; }

        public ICollection<PromotionChannelsModel> PromotionChannels { get; set; }

        public ICollection<ChannelSubscriptionsModel> Subscriptions { get; set; }
    }
}
