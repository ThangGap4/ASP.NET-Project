using System.Text;
using DocumentFormat.OpenXml.Packaging;
using HtmlAgilityPack;
using UglyToad.PdfPig;
using QuizAI.Api.Data;
using QuizAI.Api.Models;

namespace QuizAI.Api.Services;

public class DocumentProcessorService
{
    private readonly AppDbContext _context;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<DocumentProcessorService> _logger;

    // Max chars per chunk (~500 tokens ≈ 2000 chars for English/Vietnamese)
    private const int MaxChunkChars = 1800;
    private const int OverlapChars = 200;

    public DocumentProcessorService(
        AppDbContext context,
        EmbeddingService embeddingService,
        ILogger<DocumentProcessorService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    // ─── Extract Text ───────────────────────────────────────────────────────────

    public async Task<string> ExtractTextAsync(string filePath, string? mimeType)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractFromPdf(filePath),
            ".docx" => ExtractFromDocx(filePath),
            ".txt" => await File.ReadAllTextAsync(filePath),
            _ => mimeType switch
            {
                "application/pdf" => ExtractFromPdf(filePath),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractFromDocx(filePath),
                "text/plain" => await File.ReadAllTextAsync(filePath),
                _ => await File.ReadAllTextAsync(filePath) // best effort
            }
        };
    }

    public async Task<string> ExtractTextFromUrlAsync(string url)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; QuizAI/1.0)");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var html = await httpClient.GetStringAsync(url);
        return ExtractTextFromHtml(html);
    }

    private static string ExtractFromPdf(string filePath)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(filePath);
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractFromDocx(string filePath)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }
        return sb.ToString();
    }

    private static string ExtractTextFromHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script and style nodes
        var unwanted = doc.DocumentNode
            .SelectNodes("//script|//style|//noscript|//iframe|//nav|//footer|//header");
        if (unwanted != null)
            foreach (var node in unwanted.ToList())
                node.Remove();

        // Get visible text
        var sb = new StringBuilder();
        foreach (var node in doc.DocumentNode.SelectNodes("//p|//h1|//h2|//h3|//h4|//h5|//li|//td|//th") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }
        return sb.ToString();
    }

    // ─── Chunking ────────────────────────────────────────────────────────────────

    public List<string> ChunkText(string text)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // Split by paragraphs first, then sentences
        var paragraphs = text
            .Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        var currentChunk = new StringBuilder();

        foreach (var para in paragraphs)
        {
            // If this paragraph alone exceeds limit, split by sentences
            if (para.Length > MaxChunkChars)
            {
                // Flush current chunk first
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
                // Split long paragraph into sentences
                var sentenceChunks = SplitBySentences(para);
                chunks.AddRange(sentenceChunks);
                continue;
            }

            // Would adding this paragraph exceed limit?
            if (currentChunk.Length + para.Length + 2 > MaxChunkChars)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    // Add overlap: keep last OverlapChars of current chunk
                    var overlap = currentChunk.ToString();
                    currentChunk.Clear();
                    if (overlap.Length > OverlapChars)
                        currentChunk.Append(overlap[^OverlapChars..]);
                    else
                        currentChunk.Append(overlap);
                    currentChunk.AppendLine();
                }
            }

            currentChunk.AppendLine(para);
        }

        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString().Trim());

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }

    private static List<string> SplitBySentences(string text)
    {
        var chunks = new List<string>();
        // Simple sentence split by '. ', '! ', '? '
        var sentences = System.Text.RegularExpressions.Regex
            .Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var current = new StringBuilder();
        foreach (var sentence in sentences)
        {
            if (current.Length + sentence.Length + 1 > 1800)
            {
                if (current.Length > 0)
                {
                    chunks.Add(current.ToString().Trim());
                    current.Clear();
                }
            }
            current.Append(sentence).Append(' ');
        }
        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }

    // Estimate token count (rough: 1 token ≈ 4 chars for English, ~2 for Vietnamese)
    private static int EstimateTokenCount(string text) => Math.Max(1, text.Length / 3);

    // ─── Process Document (orchestrator) ────────────────────────────────────────

    public async Task ProcessDocumentAsync(Guid documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document {Id} not found for processing", documentId);
            return;
        }

        try
        {
            _logger.LogInformation("Processing document {Id}: {FileName}", documentId, document.FileName);

            // 1. Extract text
            string text;
            if (document.StorageUrl.StartsWith("http://") || document.StorageUrl.StartsWith("https://"))
                text = await ExtractTextFromUrlAsync(document.StorageUrl);
            else
                text = await ExtractTextAsync(document.StorageUrl, document.MimeType);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text extracted from document {Id}", documentId);
                document.Processed = true;
                await _context.SaveChangesAsync();
                return;
            }

            // 2. Chunk text
            var chunks = ChunkText(text);
            _logger.LogInformation("Document {Id} split into {Count} chunks", documentId, chunks.Count);

            // 3. Save chunks + embeddings
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkContent = chunks[i];
                float[]? embedding = null;

                try
                {
                    embedding = await _embeddingService.GetEmbeddingAsync(chunkContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get embedding for chunk {Index} of document {Id}", i, documentId);
                }

                var chunk = new DocumentChunk
                {
                    DocumentId = documentId,
                    ChunkIndex = i,
                    Content = chunkContent,
                    TokenCount = EstimateTokenCount(chunkContent),
                    EmbeddingVector = embedding
                };
                _context.DocumentChunks.Add(chunk);
            }

            document.Processed = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Document {Id} processed successfully", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {Id}", documentId);
            throw;
        }
    }
}
