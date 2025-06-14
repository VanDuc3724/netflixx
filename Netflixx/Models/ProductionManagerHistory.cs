using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionManagerApp.Models
{
    public class ProductionManagerHistory
    {
        [Key]
        public int Id { get; set; }

        public int? ProductionManagerId { get; set; }

        [StringLength(100)]
        public string? ProductionManagerName { get; set; }

        [StringLength(20)]
        public string Action { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        [ForeignKey("ProductionManagerId")]
        public ProductionManager? ProductionManager { get; set; }
    }
}