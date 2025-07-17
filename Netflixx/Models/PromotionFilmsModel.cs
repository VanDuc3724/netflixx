using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PromotionFilmsModel
    {
        [ForeignKey("Promotion")]
        public int PromotionID { get; set; }
        public PromotionsModel Promotion { get; set; }

        [ForeignKey("Film")]
        public int FilmID { get; set; }
        public FilmsModel Film { get; set; }
    }
}