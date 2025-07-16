using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Netflixx.Models.Souvenir;

namespace Netflixx.Models
{
    public class BrandSouModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [Display(Name = "Tên thương hiệu")]
        public string Name { get; set; } = null!;

        [StringLength(200)]
        [Display(Name = "Website")]
        [Url(ErrorMessage = "Vui lòng nhập URL hợp lệ")]
        public string? Website { get; set; }

        [Required(ErrorMessage = "Quốc gia là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Quốc gia")]
        public string Country { get; set; } = null!;

        [Display(Name = "Ngày thành lập")]
        [DataType(DataType.Date)]
        public DateTime? EstablishedDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên viết tắt")]
        public string? Alias { get; set; }

        [StringLength(100)]
        [Display(Name = "CEO")]
        public string? CEO { get; set; }

        [StringLength(200)]
        [Display(Name = "Trụ sở chính")]
        public string? Headquarters { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [StringLength(300)]
        [Display(Name = "Logo")]
        public string? LogoUrl { get; set; }

        [NotMapped]
        [Display(Name = "Logo")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "Đã xóa")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Thời gian xóa")]
        public DateTime? DeletedAt { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<ProductSouModel> Products { get; set; } = new List<ProductSouModel>();
        public ICollection<BrandHistory> Histories { get; set; } = new List<BrandHistory>();
    }
}
