using OpenAI.Chat;

namespace QuizAI.Api.Services;

public class OpenAIService
{
    private readonly ChatClient _chatClient;
    private readonly string _model;

    public OpenAIService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured");
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _chatClient = new ChatClient(_model, apiKey);
    }

    public async Task<string> GenerateQuizJson(string context, int questionCount = 5, string difficulty = "medium")
    {
        var prompt = $$"""
        You are a quiz generator. Create {{questionCount}} multiple-choice questions based ONLY on the provided context.
        Difficulty: {{difficulty}}
        Return ONLY valid JSON (no markdown, no extra text) with this exact structure:
        {
          "questions": [
            {
              "prompt": "Question text here?",
              "type": "mcq",
              "options": [
                {"content": "Option A", "isCorrect": true},
                {"content": "Option B", "isCorrect": false},
                {"content": "Option C", "isCorrect": false},
                {"content": "Option D", "isCorrect": false}
              ]
            }
          ]
        }

        Context:
        {{context}}
        """;

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(prompt)
        };

        var response = await _chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }

    public async Task<string> GradeEssay(string question, string studentAnswer, string rubric)
    {
        var prompt = $$"""
        Grade the following essay answer based on the rubric.
        Return ONLY valid JSON (no markdown, no extra text) with this exact structure:
        {
          "score": 8,
          "maxScore": 10,
          "feedback": "Your answer covers the main points...",
          "strengths": ["Clear explanation", "Good examples"],
          "improvements": ["Could elaborate on X", "Missing Y concept"],
          "citations": []
        }

        Question: {{question}}
        Student Answer: {{studentAnswer}}
        Rubric: {{rubric}}
        """;

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(prompt)
        };

        var response = await _chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }
}
