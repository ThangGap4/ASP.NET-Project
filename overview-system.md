## System Architecture Overview

```mermaid
graph TB
    subgraph USER [Người dùng]
        U([User])
    end

    subgraph DESKTOP [Desktop App - Avalonia UI / Linux]
        AV[QuizAI.Desktop\nAvalonia UI v11\n.NET 8]
        AV --> |MVVM| VM[ViewModels\nCommunityToolkit.Mvvm]
        VM --> |HTTP Bearer JWT| API_CLIENT[ApiClient\nHttpClient]
    end

    subgraph API_SERVER [API Server - Render.com]
        API[QuizAI.Api\nASP.NET Core 8\nhttps://asp-net-project-9dm5.onrender.com]
        API --> AUTH_SVC[JwtService\nHS256 JWT 24h]
        API --> DOC_SVC[DocumentProcessorService\nChunking + Background Task]
        API --> EMB_SVC[EmbeddingService\nRAG - CosineSimilarity]
        API --> QUIZ_SVC[OpenAIService\nPrompt Engineering]
    end

    subgraph STORAGE [External Storage]
        CLOUD[Cloudinary\nFile Storage\ndaxhdyrhl.cloudinary.com\n.txt .pdf .docx]
    end

    subgraph AI [OpenAI API]
        EMB_MODEL[text-embedding-3-small\nVector 1536 dims\nChunk indexing + Query matching]
        CHAT_MODEL[gpt-4o-mini\nQuiz generation\nEssay grading]
    end

    subgraph DATABASE [Database - Render.com]
        PG[(PostgreSQL\ndpg-d6r527v5gffc73f3eib0\nvirginia-postgres.render.com)]
        PG --> T1[Users]
        PG --> T2[Documents]
        PG --> T3[DocumentChunks\n+ EmbeddingVector]
        PG --> T4[Quizzes]
        PG --> T5[Questions + Options]
        PG --> T6[QuizAttempts + Answers]
    end

    U --> AV
    API_CLIENT --> |REST JSON| API
    DOC_SVC --> |UploadRawAsync| CLOUD
    DOC_SVC --> |Download file| CLOUD
    EMB_SVC --> |GenerateEmbeddingAsync| EMB_MODEL
    QUIZ_SVC --> |CompleteChatAsync| CHAT_MODEL
    API --> |EF Core\nNpgsql| PG
```

---

## Tech Stack

| Layer | Công nghệ | Hosting |
|---|---|---|
| Desktop UI | Avalonia UI v11, .NET 8, MVVM | Local machine |
| API Backend | ASP.NET Core 8, EF Core 8 | Render.com (free tier) |
| Database | PostgreSQL 16 | Render.com |
| File Storage | Cloudinary (raw upload) | Cloudinary CDN |
| AI - Embedding | OpenAI text-embedding-3-small | OpenAI API |
| AI - Quiz Gen | OpenAI gpt-4o-mini | OpenAI API |
| Auth | JWT Bearer HS256, BCrypt | - |
| ORM | Entity Framework Core + Npgsql | - |
