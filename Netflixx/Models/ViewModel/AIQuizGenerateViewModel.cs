using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class AIQuizGenerateViewModel
    {
        [Required]
        public string Topic { get; set; }

        [Range(1, 20)]
        public int NumQuestions { get; set; } = 5; // Default to 5 questions
    }
}