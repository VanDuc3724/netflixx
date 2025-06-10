using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class ChannelSubscriptionsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubscriptionID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserID { get; set; } // Kiểu string nếu dùng Identity
        public AppUserModel User { get; set; }

        [Required]
        [ForeignKey("Channel")]
        public int ChannelID { get; set; }
        public ChannelsModel Channel { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // "Active", "Expired", "Cancelled"
    }
}