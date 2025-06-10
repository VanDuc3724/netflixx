using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PromotionUsageModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UsageID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserID { get; set; }  // Lưu ý: Kiểu string nếu dùng Identity
        public AppUserModel User { get; set; }

        [Required]
        [ForeignKey("Promotion")]
        public int PromotionID { get; set; }
        public PromotionsModel Promotion { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime UsageDate { get; set; } = DateTime.UtcNow;
    }
}