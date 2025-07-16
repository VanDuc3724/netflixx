using Netflixx.Models.Souvenir;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class BrandSouModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Brand name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;
        public virtual ICollection<ProductSouModel> Products { get; set; } = new HashSet<ProductSouModel>();

    }
}
