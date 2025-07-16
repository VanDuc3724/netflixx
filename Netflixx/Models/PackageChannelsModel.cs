using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PackageChannelsModel
    {
        [ForeignKey("Package")] // Khóa ngoại đến PackagesModel
        public int PackageID { get; set; }
        public PackagesModel Package { get; set; }

        [ForeignKey("Channel")] // Khóa ngoại đến ChannelsModel
        public int ChannelID { get; set; }
        public ChannelsModel Channel { get; set; }
    }
}
