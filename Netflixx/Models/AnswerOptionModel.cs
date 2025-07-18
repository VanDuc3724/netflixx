using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ProductionManagerApp.Models;

namespace Netflixx.Models
{
    public class AnswerOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public string Text { get; set; }

        /// <summary>True if this option is correct for its question</summary>
        public bool IsCorrect { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual Question Question { get; set; }
    }
}