using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class QuizResultsViewModel
    {
        public Quiz Quiz { get; set; }
        public UserQuizAttempt Attempt { get; set; }

        public Dictionary<int, int> SelectedAnswers { get; set; }
            = new Dictionary<int, int>();
    }
}