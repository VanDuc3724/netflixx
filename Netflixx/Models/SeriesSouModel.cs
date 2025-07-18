using Netflixx.Models.Souvenir;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class SeriesSouModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên series là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên series không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string Description { get; set; }

        // Navigation property
        public ICollection<ProductSouModel> Products { get; set; } = new List<ProductSouModel>();
    }
}
