using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class BrandHistory
    {
        [Key]
        public int Id { get; set; }

        public int? BrandId { get; set; }

        [StringLength(100)]
        public string? BrandName { get; set; }

        [StringLength(20)]
        public string Action { get; set; } = string.Empty;

        public string? Details { get; set; }

        public DateTime Timestamp { get; set; }

        [ForeignKey("BrandId")]
        public BrandSouModel? Brand { get; set; }
    }
}
