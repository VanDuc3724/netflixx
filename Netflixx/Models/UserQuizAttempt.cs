using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ProductionManagerApp.Models;

namespace Netflixx.Models
{
    public class UserQuizAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int QuizId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Number of correct answers</summary>
        public int Score { get; set; }

        public bool RewardClaimed { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public virtual AppUserModel User { get; set; }

        [ForeignKey(nameof(QuizId))]
        public virtual Quiz Quiz { get; set; }

        public virtual ICollection<UserAnswer> Answers { get; set; }
            = new List<UserAnswer>();
    }
}