using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class TakeQuizViewModel
    {
        public Quiz Quiz { get; set; }
        public UserQuizAttempt Attempt { get; set; }
    }
}