using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ProductionManagerApp.Models;

namespace Netflixx.Models
{
    public class UserAnswer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttemptId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public int ChosenOptionId { get; set; }

        [ForeignKey(nameof(AttemptId))]
        public virtual UserQuizAttempt Attempt { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual Question Question { get; set; }

        [ForeignKey(nameof(ChosenOptionId))]
        public virtual AnswerOption ChosenOption { get; set; }
    }
}