using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ProductionManagerApp.Models;

namespace Netflixx.Models
{
    public class Quiz
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        /// <summary>Date the quiz goes live (e.g. user can take it once per day)</summary>
        public DateTime PublishDate { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Question> Questions { get; set; }
            = new List<Question>();
    }
}