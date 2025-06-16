using Microsoft.AspNetCore.Mvc.Rendering;
using Netflixx.Models.Souvenir;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    // Đặt trong thư mục ViewModels hoặc Models
    public class ProductCreateViewModel
    {
        public ProductSouModel Product { get; set; }
        public SelectList Categories { get; set; }
        public SelectList Brands { get; set; }
        public SelectList Series { get; set; }

    }
}
