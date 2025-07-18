using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ProductionManagerApp.Models;

namespace Netflixx.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [Required]
        public string Text { get; set; }

        /// <summary>Optional image URL (store under /wwwroot/...)</summary>
        public string? ImageUrl { get; set; }

        [ForeignKey(nameof(QuizId))]
        public virtual Quiz Quiz { get; set; }

        public virtual ICollection<AnswerOption> Options { get; set; }
            = new List<AnswerOption>();
    }
}