using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class UserAccountsModel
    {
        [Key]
        [ForeignKey("User")]
        public string UserID { get; set; } // Kiểu string nếu dùng Identity

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Balance { get; set; } = 0;

        [Required]
        [Range(0, int.MaxValue)]
        public int PointsBalance { get; set; } = 0;


        // Navigation property
        public AppUserModel User { get; set; }
    }
}
