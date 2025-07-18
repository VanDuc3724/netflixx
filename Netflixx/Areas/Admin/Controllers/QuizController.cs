using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Netflixx.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuizController : Controller
    {
        private readonly DBContext _db;
        private readonly ILogger<QuizController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Constructor with dependency injection
        public QuizController(
            DBContext db,
            ILogger<QuizController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _apiKey = "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";
        }

        // GET: /Admin/Quiz
        public async Task<IActionResult> Index()
        {
            var quizzes = await _db.Quizzes
                                   .Include(z => z.Questions)
                                     .ThenInclude(q => q.Options)
                                   .AsNoTracking()
                                   .ToListAsync();

            return View(quizzes);
        }

        // GET: /Admin/Quiz/Create
        public IActionResult Create()
        {
            var vm = new QuizEditViewModel
            {
                PublishDate = DateTime.UtcNow,
                Questions = new List<QuestionEditViewModel>
        {
            new QuestionEditViewModel
            {
                Options = new List<OptionEditViewModel>
                {
                    new OptionEditViewModel(),
                    new OptionEditViewModel()
                }
            }
        }
            };
            return View(vm);
        }

        // POST: /Admin/Quiz/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizEditViewModel vm)
        {
            _logger.LogInformation("POST Create Quiz: Title={Title}, #Questions={Q}",
                                    vm.Title, vm.Questions.Count);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create ModelState invalid: {Errors}",
                    string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors)
                                                       .Select(e => e.ErrorMessage)));
                return View(vm);
            }

            // MAP VM → ENTITIES
            var quiz = new Quiz
            {
                Title = vm.Title,
                PublishDate = vm.PublishDate
            };

            foreach (var qVm in vm.Questions)
            {
                var q = new Question { Text = qVm.Text };
                for (int i = 0; i < qVm.Options.Count; i++)
                {
                    var oVm = qVm.Options[i];
                    q.Options.Add(new AnswerOption
                    {
                        Text = oVm.Text,
                        IsCorrect = (i == qVm.CorrectOptionIndex)
                    });
                }
                quiz.Questions.Add(q);
            }

            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created Quiz Id={Id}", quiz.Id);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Quiz/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _db.Quizzes
                                .Include(z => z.Questions)
                                  .ThenInclude(q => q.Options)
                                .FirstOrDefaultAsync(z => z.Id == id);

            if (quiz == null) return NotFound();

            // MAP ENTITIES → VM
            var vm = new QuizEditViewModel
            {
                Id = quiz.Id,
                Title = quiz.Title,
                PublishDate = quiz.PublishDate
            };

            foreach (var q in quiz.Questions)
            {
                var qVm = new QuestionEditViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    CorrectOptionIndex = q.Options
                                              .Select((o, idx) => new { o.IsCorrect, idx })
                                              .FirstOrDefault(x => x.IsCorrect)?.idx ?? 0
                };

                foreach (var o in q.Options)
                {
                    qVm.Options.Add(new OptionEditViewModel
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    });
                }

                vm.Questions.Add(qVm);
            }

            return View(vm);
        }

        // POST: /Admin/Quiz/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QuizEditViewModel vm)
        {
            _logger.LogInformation("POST Edit Quiz Id={Id}: Title={Title}, #Q={Q}",
                                    id, vm.Title, vm.Questions.Count);

            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit ModelState invalid: {Errors}",
                    string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors)
                                                       .Select(e => e.ErrorMessage)));
                return View(vm);
            }

            // load existing
            var quiz = await _db.Quizzes
                                .Include(z => z.Questions)
                                  .ThenInclude(q => q.Options)
                                .FirstAsync(z => z.Id == id);

            // update top‐level
            quiz.Title = vm.Title;
            quiz.PublishDate = vm.PublishDate;

            // 1) remove deleted questions
            var removeQs = quiz.Questions
                               .Where(q => !vm.Questions.Any(qv => qv.Id == q.Id))
                               .ToList();
            _db.Questions.RemoveRange(removeQs);

            // 2) sync each VM question
            foreach (var qVm in vm.Questions)
            {
                if (qVm.Id.HasValue)
                {
                    // try to locate the existing entity
                    var existingQuestion = quiz.Questions
                                               .FirstOrDefault(x => x.Id == qVm.Id.Value);
                    if (existingQuestion != null)
                    {
                        // **UPDATE** the found question
                        existingQuestion.Text = qVm.Text;

                        // remove any options the user deleted
                        var toRemoveOpts = existingQuestion.Options
                                            .Where(o => !qVm.Options.Any(ov => ov.Id == o.Id))
                                            .ToList();
                        _db.AnswerOptions.RemoveRange(toRemoveOpts);

                        // now update + add
                        for (int i = 0; i < qVm.Options.Count; i++)
                        {
                            var oVm = qVm.Options[i];
                            if (oVm.Id.HasValue)
                            {
                                var existingOpt = existingQuestion.Options
                                                    .FirstOrDefault(o => o.Id == oVm.Id.Value);
                                if (existingOpt != null)
                                {
                                    existingOpt.Text = oVm.Text;
                                    existingOpt.IsCorrect = (i == qVm.CorrectOptionIndex);
                                }
                                else
                                {
                                    // it had an ID but we didn't find it—treat as new
                                    existingQuestion.Options.Add(new AnswerOption
                                    {
                                        Text = oVm.Text,
                                        IsCorrect = (i == qVm.CorrectOptionIndex)
                                    });
                                }
                            }
                            else
                            {
                                // brand-new option
                                existingQuestion.Options.Add(new AnswerOption
                                {
                                    Text = oVm.Text,
                                    IsCorrect = (i == qVm.CorrectOptionIndex)
                                });
                            }
                        }

                        continue;
                    }
                    // if we get here, qVm.Id.HasValue but we didn't find it—
                    // fall through to "create new question"
                }

                // new question (either Id was null, or lookup failed)
                var brandNew = new Question { Text = qVm.Text };
                for (int i = 0; i < qVm.Options.Count; i++)
                {
                    var oVm = qVm.Options[i];
                    brandNew.Options.Add(new AnswerOption
                    {
                        Text = oVm.Text,
                        IsCorrect = (i == qVm.CorrectOptionIndex)
                    });
                }
                quiz.Questions.Add(brandNew);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Saved edits to Quiz Id={Id}", id);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Quiz/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("POST Delete Quiz Id={Id}", id);

            var quiz = await _db.Quizzes
                                .Include(z => z.Questions)
                                  .ThenInclude(q => q.Options)
                                .FirstOrDefaultAsync(z => z.Id == id);
            if (quiz == null)
            {
                _logger.LogWarning("Delete: quiz Id={Id} not found", id);
                return RedirectToAction(nameof(Index));
            }

            // delete all user answers on *all* options in this quiz
            var allOptIds = quiz.Questions.SelectMany(q => q.Options).Select(o => o.Id).ToList();
            var orphans = _db.UserAnswers.Where(ua => allOptIds.Contains(ua.ChosenOptionId));
            _db.UserAnswers.RemoveRange(orphans);

            // delete all options
            _db.AnswerOptions.RemoveRange(quiz.Questions.SelectMany(q => q.Options));

            // delete all questions
            _db.Questions.RemoveRange(quiz.Questions);

            // delete quiz
            _db.Quizzes.Remove(quiz);

            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted Quiz Id={Id}", id);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Quiz/GenerateAIQuiz
        public IActionResult GenerateAIQuiz()
        {
            return View(new AIQuizGenerateViewModel());
        }

        // POST: /Admin/Quiz/GenerateAIQuiz
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAIQuiz(AIQuizGenerateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("GenerateAIQuiz ModelState invalid: {Errors}",
                    string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors)
                                                       .Select(e => e.ErrorMessage)));
                return View(vm);
            }

            // Craft the prompt for OpenAI with stricter JSON instruction
            var prompt = $"Generate a quiz about {vm.Topic} with {vm.NumQuestions} questions. " +
                         "Each question must have exactly four options, with exactly one correct answer. " +
                         "Return *only* the raw JSON string matching this exact structure, without any markdown (e.g., no ```json), additional text, or formatting: " +
                         "{\"title\": \"<quiz-title>\", \"questions\": [{\"text\": \"<question-text>\", " +
                         "\"options\": [{\"text\": \"<option1>\", \"isCorrect\": true}, " +
                         "{\"text\": \"<option2>\", \"isCorrect\": false}, " +
                         "{\"text\": \"<option3>\", \"isCorrect\": false}, " +
                         "{\"text\": \"<option4>\", \"isCorrect\": false}]}]}";

            // Prepare the request body
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
            new { role = "system", content = "You are a helpful assistant that generates quizzes in exact raw JSON format only." },
            new { role = "user", content = prompt }
        },
                temperature = 0.7
            };

            // Create and configure the HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Send the request and log the raw response
            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("OpenAI API Response: {Response}", responseJson);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API call failed: {StatusCode} - Response: {Response}", response.StatusCode, responseJson);
                ModelState.AddModelError("", "Failed to generate quiz from AI.");
                return View(vm);
            }

            // Parse the response
            var completion = JsonSerializer.Deserialize<OpenAICompletion>(responseJson);

            if (completion?.Choices == null || completion.Choices.Count == 0 || string.IsNullOrEmpty(completion.Choices[0].Message.Content))
            {
                _logger.LogError("Invalid response from OpenAI API - Response: {Response}", responseJson);
                ModelState.AddModelError("", "Failed to generate quiz from AI due to invalid response format.");
                return View(vm);
            }

            var quizJson = completion.Choices[0].Message.Content;
            _logger.LogInformation("Raw Extracted Quiz JSON: {QuizJson}", quizJson);

            // Remove markdown formatting (```json
            quizJson = quizJson.Trim();
            if (quizJson.StartsWith("```json") && quizJson.EndsWith("```"))
            {
                quizJson = quizJson.Substring(7, quizJson.Length - 10).Trim(); // Remove ```json and ```
            }
            _logger.LogInformation("Cleaned Quiz JSON: {QuizJson}", quizJson);

            AIQuizResponse aiQuiz;
            try
            {
                aiQuiz = JsonSerializer.Deserialize<AIQuizResponse>(quizJson);
                if (aiQuiz?.Questions == null || !aiQuiz.Questions.All(q => q.Options?.Count == 4))
                {
                    throw new JsonException("Invalid quiz structure: Questions must have exactly 4 options each.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI quiz response - Cleaned JSON: {QuizJson}", quizJson);
                ModelState.AddModelError("", "Invalid quiz format from AI.");
                return View(vm);
            }

            // Map to Quiz entity
            var quiz = new Quiz
            {
                Title = aiQuiz.Title,
                PublishDate = DateTime.UtcNow,
                IsActive = true
            };

            foreach (var q in aiQuiz.Questions)
            {
                var question = new Question { Text = q.Text };
                foreach (var o in q.Options)
                {
                    question.Options.Add(new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect });
                }
                quiz.Questions.Add(question);
            }

            // Save to the database
            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Generated AI Quiz Id={Id}", quiz.Id);

            // Redirect to the edit page
            return RedirectToAction(nameof(Edit), new { id = quiz.Id });
        }
    }
}