using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ProductionManagerApp.Models;

namespace Netflixx.Models
{
    public class FilmsModel
    {
        // =============================================
        // PHẦN GIỮ NGUYÊN TỪ BẢN ĐANG CHẠY (BẢN SỐ 1)
        // =============================================
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên không được để trống.")]
        public string Title { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public string? FilmURL { get; set; }

        [Required(ErrorMessage = "Vui lòng không để trống thể loại.")]
        public string? Genre { get; set; }

        [Column("Description")]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public string? PosterPath { get; set; }

        [Display(Name = "Quản lý sản xuất")]
        public int? ProductionManagerId { get; set; }

        [ForeignKey("ProductionManagerId")]  // Áp dụng vào Navigation Property
        public virtual ProductionManager? ProductionManager { get; set; }

        public string? TrailerURL { get; set; }

        public float? Rating { get; set; } = 0.0f;

        public virtual ICollection<PackagesModel> Packages { get; set; } = new List<PackagesModel>();
        public virtual ICollection<PromotionFilmsModel> PromotionFilms { get; set; } = new List<PromotionFilmsModel>();
        public virtual ICollection<FilmPurchasesModel> Purchases { get; set; } = new List<FilmPurchasesModel>();
        public virtual ICollection<FavoriteFilmsModel> FavoriteFilms { get; set; } = new List<FavoriteFilmsModel>();

        // =============================================
        // PHẦN THÊM MỚI TỪ BẢN SỐ 2 (OPTIONAL)
        // =============================================
        // Thông tin mở rộng
        [Display(Name = "Thời lượng (phút)")]
        [Range(1, 500)]
        public int? Duration { get; set; }

        [Display(Name = "Trạng thái")]
        [Column(TypeName = "nvarchar(20)")]
        public string? Status { get; set; }

        [Display(Name = "Độ tuổi")]
        [Column(TypeName = "nvarchar(10)")]
        public string? RatingValue { get; set; }

        [Display(Name = "Đạo diễn")]
        [StringLength(100)]
        public string? Director { get; set; }

        [Display(Name = "Diễn viên")]
        public string? Actors { get; set; }

        // Hệ thống tracking
        [Display(Name = "Hành động cuối")]
        [Column(TypeName = "nvarchar(20)")]
        public string? LastAction { get; set; }

        [Display(Name = "Người sửa cuối")]
        public string? ModifiedBy { get; set; }

        [Display(Name = "Ngày sửa cuối")]
        public DateTime? ModificationDate { get; set; }

        [Display(Name = "Trường đã sửa")]
        [Column(TypeName = "nvarchar(500)")]
        public string? ModifiedFields { get; set; }

        [Display(Name = "Ghi chú thay đổi")]
        [StringLength(500)]
        public string? ChangeNote { get; set; }

        // =============================================
        // PHƯƠNG THỨC TIỆN ÍCH (OPTIONAL)
        // =============================================
        public void UpdateTrackingInfo(FilmsModel original, string action, string modifiedBy, string note = null)
        {
            var changedFields = new List<string>();
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (ShouldIgnoreProperty(prop.Name)) continue;

                var oldValue = prop.GetValue(original);
                var newValue = prop.GetValue(this);

                if (!Equals(oldValue, newValue))
                {
                    changedFields.Add(GetDisplayName(prop));
                }
            }

            ModifiedFields = changedFields.Any() ? string.Join(", ", changedFields) : null;
            LastAction = action;
            ModifiedBy = modifiedBy;
            ModificationDate = DateTime.Now;
            ChangeNote = note;
        }

        private bool ShouldIgnoreProperty(string propName)
        {
            var ignoreList = new List<string>
            {
                nameof(Id), nameof(LastAction), nameof(ModifiedBy),
                nameof(ModificationDate), nameof(ModifiedFields),
                nameof(ChangeNote), nameof(Packages),
                nameof(PromotionFilms), nameof(Purchases),
                nameof(FavoriteFilms), nameof(ProductionManager)
            };
            return ignoreList.Contains(propName);
        }

        public string GetStatusText()
        {
            return Status switch
            {
                "Active" => "Đang chiếu",
                "Inactive" => "Ngừng chiếu",
                "Upcoming" => "Sắp chiếu",
                "Deleted" => "Đã xóa",
                _ => Status ?? "Không xác định"
            };
        }

        public string GetRatingText()
        {
            return RatingValue switch
            {
                "P" => "Mọi lứa tuổi",
                "C13" => "13+",
                "C16" => "16+",
                "C18" => "18+",
                _ => RatingValue ?? "Không xác định"
            };
        }

        private string GetDisplayName(PropertyInfo prop)
        {
            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
            return displayAttr?.Name ?? prop.Name;
        }
    }
}