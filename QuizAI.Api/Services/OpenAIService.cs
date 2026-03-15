using OpenAI;
using OpenAI.Chat;

namespace QuizAI.Api.Services;

public class OpenAIService
{
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient _client;

    public OpenAIService(IConfiguration configuration)
    {
        _configuration = configuration;
        var apiKey = _configuration["OpenAI:ApiKey"];
        _client = new OpenAIClient(apiKey);
    }

    public async Task<string> GenerateQuizJson(string documentContent, int questionCount = 5)
    {
        var prompt = $$"""
        Based on the following document, generate a quiz with {{questionCount}} questions.
        Return ONLY valid JSON (no markdown, no extra text) with this structure:
        {
          "questions": [
            {
              "content": "Question text here?",
              "type": "multiple_choice",
              "options": [
                {"content": "Option A", "isCorrect": true},
                {"content": "Option B", "isCorrect": false},
                {"content": "Option C", "isCorrect": false},
                {"content": "Option D", "isCorrect": false}
              ]
            }
          ]
        }
        
        Document:
        {{documentContent}}
        """;

        var message = new Message { Content = prompt, Role = "user" };
        var chatRequest = new CreateChatCompletionRequest
        {
            Model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
            Messages = new List<Message> { message }
        };

        var response = await _client.CreateChatCompletionAsync(chatRequest);
        return response.Choices[0].Message.Content;
    }

    public async Task<string> GradeEssay(string question, string studentAnswer, string rubric)
    {
        var prompt = $$"""
        Grade the following essay answer based on the rubric.
        Return ONLY valid JSON (no markdown, no extra text) with this structure:
        {
          "score": 85,
          "feedback": "Your answer is good because...",
          "strengths": ["Point 1", "Point 2"],
          "improvements": ["Improvement 1", "Improvement 2"]
        }
        
        Question: {{question}}
        Student Answer: {{studentAnswer}}
        Rubric: {{rubric}}
        """;

        var message = new Message { Content = prompt, Role = "user" };
        var chatRequest = new CreateChatCompletionRequest
        {
            Model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
            Messages = new List<Message> { message }
        };

        var response = await _client.CreateChatCompletionAsync(chatRequest);
        return response.Choices[0].Message.Content;
    }
}
