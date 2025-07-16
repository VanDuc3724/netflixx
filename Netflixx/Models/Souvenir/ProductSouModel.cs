using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models.Souvenir
{
    public class ProductSouModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên!")]
        public string Name { get; set; } = null!;

        public int? CategoryId { get; set; }

        public int? BrandId { get; set; }

        public int? SeriesId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm!")]
        [Range(0, 10000000000, ErrorMessage = "Giá sản phẩm không hợp lệ!")]
        [Precision(18, 2)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Chỉ được nhập số")]
        public int StockQuantity { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageUpload { get; set; }

        public CategorySouModel? Category { get; set; }

        public BrandSouModel? Brand { get; set; }

        public SeriesSouModel? Series { get; set; }

        public virtual ICollection<OrderDetailSouModel> OrderDetails { get; set; } = new List<OrderDetailSouModel>();

        public virtual ICollection<ProductImageSouModel> ProductImages { get; set; } = new List<ProductImageSouModel>();
    }
}
