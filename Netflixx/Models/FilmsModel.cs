using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProductionManagerApp.Models;

namespace Netflixx.Models
{
    public class FilmsModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên không được để trống.")]
        public string Title { get; set; }

        public DateTime? ReleaseDate { get; set; }
        public string? FilmURL { get; set; }

        [Required(ErrorMessage = "Vui lòng không để trống thể loại.")]
        public string Genre { get; set; }

        [Column("Description")] // Sửa lại chính tả nếu database đã có trường "Decription"
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public int? ProductionManagerId { get; set; }


        public ICollection<PackageFilmsModel> PackageFilms { get; set; }
        public ICollection<PromotionFilmsModel> PromotionFilms { get; set; }

        public ICollection<FilmPurchasesModel> Purchases { get; set; }


        public float Rating { get; set; } = 0.0f; // Giá trị mặc định là 0.0f



        [ForeignKey("ProductionManagerId")]
        public virtual ProductionManage ProductionManager { get; set; }
    }
}
