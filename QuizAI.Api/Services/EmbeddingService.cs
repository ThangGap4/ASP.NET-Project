using OpenAI.Embeddings;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;

namespace QuizAI.Api.Services;

public class EmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly AppDbContext _context;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(IConfiguration configuration, AppDbContext context, ILogger<EmbeddingService> logger)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured");
        var model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _embeddingClient = new EmbeddingClient(model, apiKey);
        _context = context;
        _logger = logger;
    }

    // ─── Get embedding for a single text ─────────────────────────────────────────

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        // Truncate to ~8000 tokens max (text-embedding-3-small limit)
        if (text.Length > 24000)
            text = text[..24000];

        var response = await _embeddingClient.GenerateEmbeddingAsync(text);
        return response.Value.ToFloats().ToArray();
    }

    // ─── Cosine similarity ────────────────────────────────────────────────────────

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same dimension");

        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0f;
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    // ─── Get top-k relevant chunks from a document ───────────────────────────────

    public async Task<List<(Guid ChunkId, int ChunkIndex, string Content, float Score)>> GetTopKChunksAsync(
        Guid documentId,
        string query,
        int k = 5)
    {
        // Load all chunks for this document that have embeddings
        var chunks = await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId && c.EmbeddingVector != null)
            .Select(c => new { c.Id, c.ChunkIndex, c.Content, c.EmbeddingVector })
            .ToListAsync();

        if (!chunks.Any())
        {
            _logger.LogWarning("No embedded chunks found for document {DocumentId}", documentId);
            // Fallback: return first k chunks without scoring
            var fallback = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .Take(k)
                .Select(c => new { c.Id, c.ChunkIndex, c.Content })
                .ToListAsync();
            return fallback.Select(c => (c.Id, c.ChunkIndex, c.Content, 0f)).ToList();
        }

        // Get query embedding
        float[] queryEmbedding;
        try
        {
            queryEmbedding = await GetEmbeddingAsync(query);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get query embedding, falling back to first {K} chunks", k);
            return chunks.Take(k).Select(c => (c.Id, c.ChunkIndex, c.Content, 0f)).ToList();
        }

        // Score and rank
        var scored = chunks
            .Select(c => (
                ChunkId: c.Id,
                ChunkIndex: c.ChunkIndex,
                Content: c.Content,
                Score: CosineSimilarity(queryEmbedding, c.EmbeddingVector!)
            ))
            .OrderByDescending(x => x.Score)
            .Take(k)
            .OrderBy(x => x.ChunkIndex) // Re-order by position for coherence
            .ToList();

        return scored;
    }

    // ─── Get top-k chunks across all documents of a user ─────────────────────────

    public async Task<List<(Guid ChunkId, int ChunkIndex, string Content, Guid DocumentId, float Score)>> GetTopKChunksAcrossDocumentsAsync(
        IEnumerable<Guid> documentIds,
        string query,
        int k = 8)
    {
        var chunks = await _context.DocumentChunks
            .Where(c => documentIds.Contains(c.DocumentId) && c.EmbeddingVector != null)
            .Select(c => new { c.Id, c.ChunkIndex, c.Content, c.DocumentId, c.EmbeddingVector })
            .ToListAsync();

        if (!chunks.Any()) return new();

        float[] queryEmbedding;
        try
        {
            queryEmbedding = await GetEmbeddingAsync(query);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get query embedding for multi-doc search");
            return new();
        }

        return chunks
            .Select(c => (
                ChunkId: c.Id,
                ChunkIndex: c.ChunkIndex,
                Content: c.Content,
                DocumentId: c.DocumentId,
                Score: CosineSimilarity(queryEmbedding, c.EmbeddingVector!)
            ))
            .OrderByDescending(x => x.Score)
            .Take(k)
            .ToList();
    }
}
