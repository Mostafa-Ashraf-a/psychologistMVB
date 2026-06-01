using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MentalHealthAssessment.Application.Interfaces;

namespace MentalHealthAssessment.Infrastructure.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelName;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? "";
            _modelName = configuration["Gemini:ModelName"] ?? "gemini-3-flash";
        }

        public async Task<string> AnalyzeChatResponseAsync(string conversationHistory, string latestUserMessage, string questionOptionsJson)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "{\"error\": \"Gemini API key is not configured.\"}";
            }

            var prompt = $@"
You are an expert AI clinical psychologist assistant working in a mental health rehabilitation center in Saudi Arabia.
Your job is to analyze the patient's latest statement in a natural conversation, check it against the conversation history, and map it to one of the predefined options provided below.

Predefined Question Options (JSON format):
{questionOptionsJson}

Conversation History so far:
{conversationHistory}

Patient's latest statement:
""{latestUserMessage}""

Instructions:
1. Determine which of the predefined options best matches the patient's statement.
2. Calculate your confidence score (0.0 to 1.0) for this mapping.
3. If the patient's statement is ambiguous or does not give enough context to be sure, set 'isSure' to false so we can keep asking questions about the same point in a friendly way.
4. Add a clinical note ('aiNote') in Arabic describing your analysis of their response.
5. You must output ONLY a valid JSON object matching this schema, without markdown formatting or code blocks:
{{
  ""mappedOptionId"": ""the_matched_option_id_or_null"",
  ""confidence"": 0.95,
  ""isSure"": true,
  ""aiNote"": ""تحليل الذكاء الاصطناعي هنا باللغة العربية""
}}
";

            return await CallGeminiApiAsync(prompt);
        }

        public async Task<string> GenerateSelfHelpInsightsAsync(string conversationHistory)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "Gemini API key is not configured.";
            }

            var prompt = $@"
You are an expert AI clinical psychologist assistant. Based on the following completed patient assessment conversation, generate personalized, professional self-help recommendations and insights in Arabic.
Focus on actionable steps, sleep hygiene tips if applicable, and emotional regulation exercises. Keep your tone supportive, encouraging, and culturally appropriate for Saudi Arabia.

Assessment Chat Transcript:
{conversationHistory}

Generate the self-help report in clear markdown format.
";

            return await CallGeminiApiAsync(prompt);
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                
                // Extract text response from Gemini's JSON structure
                var candidateText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return candidateText?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Gemini API Call failed: {ex.Message}\"}}";
            }
        }
    }
}
