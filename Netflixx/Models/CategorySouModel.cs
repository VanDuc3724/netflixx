using Netflixx.Models.Souvenir;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class CategorySouModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        // Navigation property
        public ICollection<ProductSouModel> Products { get; set; }
    }
}
