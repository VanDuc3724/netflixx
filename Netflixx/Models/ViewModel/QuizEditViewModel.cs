// Models/ViewModel/QuizEditViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel {
public class QuizEditViewModel
{
    public int? Id { get; set; }                 // null for “new”
    [Required] public string Title { get; set; }
    [Required] public DateTime PublishDate { get; set; }

    public List<QuestionEditViewModel> Questions { get; set; }
        = new List<QuestionEditViewModel>();
}

    public class QuestionEditViewModel
    {
        public int? Id { get; set; }

        [Required]
        public string Text { get; set; }

        public int CorrectOptionIndex { get; set; }

        public List<OptionEditViewModel> Options { get; set; }
            = new List<OptionEditViewModel>();
    }

    public class OptionEditViewModel
{
    public int? Id { get; set; }
    [Required] public string Text { get; set; }
    public bool IsCorrect { get; set; }
}

}