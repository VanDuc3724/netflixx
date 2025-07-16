using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PromotionChannelsModel
    {
        [ForeignKey("Promotion")]
        public int PromotionID { get; set; }
        public PromotionsModel Promotion { get; set; }

        [ForeignKey("Channel")]
        public int ChannelID { get; set; }
        public ChannelsModel Channel { get; set; }
    }
}