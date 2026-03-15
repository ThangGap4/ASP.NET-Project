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

    public async Task<string> GenerateQuizJson(string context, int questionCount = 5, string difficulty = "medium", string questionType = "mcq")
    {
        var typeInstruction = questionType switch
        {
            "true_false" => """
                Generate ONLY True/False questions. Each question must have exactly 2 options:
                {"content": "True", "isCorrect": true/false} and {"content": "False", "isCorrect": true/false}
                Set type to "true_false".
                """,
            "fill_blank" => """
                Generate ONLY Fill-in-the-blank questions. The prompt must contain "___" as the blank.
                Do NOT include options array. Put the correct answer in the "rubric" field.
                Set type to "fill_blank".
                """,
            "mixed" => """
                Generate a MIX of question types: roughly 1/3 MCQ, 1/3 True/False, 1/3 Fill-in-the-blank.
                - MCQ: 4 options, type = "mcq"
                - True/False: 2 options (True/False), type = "true_false"
                - Fill-in-blank: prompt has "___", no options, answer in rubric, type = "fill_blank"
                """,
            _ => """
                Generate ONLY multiple-choice questions with exactly 4 options each.
                Set type to "mcq".
                """
        };

        var prompt = $$"""
        You are a quiz generator. Create exactly {{questionCount}} questions based ONLY on the provided context.
        Difficulty: {{difficulty}}

        {{typeInstruction}}

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
              ],
              "rubric": null
            }
          ]
        }

        Context:
        {{context}}
        """;

        var messages = new List<ChatMessage> { ChatMessage.CreateUserMessage(prompt) };
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
