using Netflixx.Models.Souvenir;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class ProductImageSouModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        public string? ImageUrl { get; set; } = null!;

        public int? DisplayOrder { get; set; } = 0;

        public bool? IsDefault { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public ProductSouModel? Product { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
