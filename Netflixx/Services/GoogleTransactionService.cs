using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Netflixx.Models;

namespace Netflixx.Services
{
    public class GoogleTransactionService : IGoogleTransactionService
    {
        private readonly HttpClient _httpClient;
        private const string Url = "https://script.google.com/macros/s/AKfycbyi5nMWfuHp3BigdOV9h0lq97ci0f1YTWOlULUsRgkW8_AT0e34FVu67ao3l00EXOHx/exec";

        public GoogleTransactionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<GoogleTransactionModel>> FetchTransactionsAsync()
        {
            using var response = await _httpClient.GetAsync(Url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<GoogleTransactionWrapper>(content);
            return wrapper?.Data ?? new List<GoogleTransactionModel>();
        }

        private class GoogleTransactionWrapper
        {
            public List<GoogleTransactionModel> Data { get; set; }
            public bool Error { get; set; }
        }
    }
}
