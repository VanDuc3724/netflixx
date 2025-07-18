using ProductionManagerApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Netflixx.Models
{
    public class AIQuizResponse
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("questions")]
        public List<AIQuestion> Questions { get; set; }
    }

    public class AIQuestion
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("options")]
        public List<AIOption> Options { get; set; }
    }

    public class AIOption
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }
    }

    public class OpenAICompletion
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}