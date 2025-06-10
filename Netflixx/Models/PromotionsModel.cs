using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PromotionsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PromotionID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } // 'Flat', 'Percent'

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<PromotionPackagesModel> PromotionPackages { get; set; }

        public ICollection<PromotionFilmsModel> PromotionFilms { get; set; }

        public ICollection<PromotionChannelsModel> PromotionChannels { get; set; }

        public ICollection<PromotionUsageModel> PromotionUsages { get; set; }
    }
}