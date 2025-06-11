using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductionManagerApp.Models
{
    public class Country
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public virtual ICollection<ProductionManager> ProductionManagers { get; set; } = new List<ProductionManager>();
    }
}