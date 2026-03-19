## 0. Overview - Toàn bộ luồng

```mermaid
flowchart TD
    subgraph CLIENT [Desktop - Avalonia UI]
        U([User]) --> LV[LoginView]
        LV --> LVM[LoginViewModel\nLoginCommand]
        U --> LIB[LibraryView]
        LIB --> UPFILE[UploadFileCommand]
        LIB --> UPURL[ImportUrlCommand]
        LIB --> UPPASTE[SavePasteTextAsync]
        LIB --> CQ[CreateQuizView\nGenerateQuizCommand]
        CQ --> TQ[TakeQuizView\nStartQuizAsync]
        TQ --> MCQ[Chọn option MCQ]
        TQ --> TF[Bấm True/False]
        TQ --> FB[Nhập fill blank]
        MCQ & TF & FB --> SUB[SubmitAsync]
        SUB --> RES[ResultView]
        U --> HIST[HistoryView\nLoadHistoryAsync]
        U --> PROF[ProfileView]
        CQ --> PUB[TogglePublishCommand]
    end

    subgraph API [ASP.NET Core API - localhost:5127]
        AUTH[AuthController\nPOST /auth/login\nBCrypt + JWT]
        DOCC[DocumentController\nPOST /documents/upload\nPOST /documents/upload-url\nPOST /documents/upload-text]
        QUIZC[QuizController\nPOST /quizzes/generate\nPATCH /quizzes/id/publish]
        ATMC[AttemptController\nPOST /attempts/start\nPOST /attempts/id/submit\nGET /attempts/my]
    end

    subgraph BACKGROUND [Background Processing]
        PROC[DocumentProcessorService\nProcessDocumentAsync]
        PROC --> |text/html| HTMLX[ExtractTextFromUrlAsync\nHttpClient + HtmlAgilityPack]
        PROC --> |file URL| DLEXT[DownloadAndExtractAsync\ntemp file]
        DLEXT --> |.txt| TXT[File.ReadAllTextAsync]
        DLEXT --> |.docx| DOCX[OpenXML paragraphs]
        DLEXT --> |.pdf| PDF[PdfPig pages]
        HTMLX & TXT & DOCX & PDF --> CHUNK[ChunkText\n~1800 chars/chunk]
        CHUNK --> EMB1[EmbeddingService\nGetEmbeddingAsync\ntext-embedding-3-small\n→ float 1536 dims]
        EMB1 --> SAVEC[Lưu DocumentChunk\n+ EmbeddingVector vào DB]
    end

    subgraph AI [OpenAI / External]
        OPENAI_EMB[text-embedding-3-small\nVector embedding]
        OPENAI_CHAT[gpt-4o-mini\nChat completion]
        CLOUDINARY[Cloudinary\nFile storage]
    end

    subgraph DB [PostgreSQL - Render]
        USERS[(Users)]
        DOCS[(Documents)]
        CHUNKS[(DocumentChunks)]
        QUIZZES[(Quizzes)]
        QUESTIONS[(Questions)]
        OPTIONS[(QuestionOptions)]
        ATTEMPTS[(QuizAttempts)]
        ANSWERS[(AttemptAnswers)]
    end

    LVM --> AUTH
    AUTH --> USERS

    UPFILE & UPURL & UPPASTE --> DOCC
    DOCC --> CLOUDINARY
    DOCC --> DOCS
    DOCC --> PROC

    CQ --> QUIZC
    QUIZC --> |RAG| EMB2[EmbeddingService\nGetTopKChunksAsync\nCosineSimilarity]
    EMB2 --> OPENAI_EMB
    EMB2 --> CHUNKS
    QUIZC --> OPENAI_CHAT
    OPENAI_CHAT --> QUIZC
    QUIZC --> QUIZZES & QUESTIONS & OPTIONS
    PUB --> QUIZC

    TQ --> ATMC
    SUB --> ATMC
    ATMC --> |fill_blank/essay| OPENAI_CHAT
    ATMC --> ATTEMPTS & ANSWERS

    HIST --> ATMC
    ATMC --> ATTEMPTS
```

---

## 1. Auth Flow


```mermaid
flowchart TD
    A([User mở app]) --> B[MainWindow.axaml.cs\nOnStartup]
    B --> C{Token trong\nmemory?}
    C -- Không --> D[NavigateToLogin\nMainWindowViewModel]
    C -- Có --> E[NavigateToLibrary]
    D --> F[LoginView.axaml\nUser nhập email + password]
    F --> G[LoginViewModel\nLoginCommand]
    G --> H[ApiClient.LoginAsync\nPOST /api/auth/login]
    H --> I[AuthController.Login\nBCrypt.Verify password]
    I --> J{Đúng?}
    J -- Không --> K[401 → StatusMessage lỗi]
    J -- Có --> L[JwtService.GenerateToken\nHS256 JWT 24h]
    L --> M[Trả token về Desktop]
    M --> N[ApiClient lưu token\nhttp.DefaultHeaders Bearer]
    N --> E
```

---

## 2. Upload Document Flow

```mermaid
flowchart TD
    subgraph UPLOAD_FILE [Luồng A: Upload File .txt/.pdf/.docx]
        A1[LibraryView\nBrowse File button] --> A2[LibraryViewModel\nUploadFileCommand]
        A2 --> A3[ApiClient.UploadDocumentAsync\nPOST /api/documents/upload\nmultipart/form-data]
        A3 --> A4[DocumentController.UploadFile\nValidate ext: .txt .pdf .docx]
        A4 --> A5[CloudinaryService.UploadRawAsync\nStream → Cloudinary API]
        A5 --> A6[Lưu Document vào DB\nStorageUrl = Cloudinary URL\nProcessed = false]
        A6 --> A7[Task.Run background\nIServiceScopeFactory tạo scope mới]
    end

    subgraph UPLOAD_URL [Luồng B: Import URL]
        B1[LibraryView\nImport URL button] --> B2[LibraryViewModel\nImportUrlCommand]
        B2 --> B3[ApiClient.UploadUrlAsync\nPOST /api/documents/upload-url]
        B3 --> B4[DocumentController.UploadFromUrl\nValidate URI scheme http/https]
        B4 --> B5[Lưu Document vào DB\nStorageUrl = URL gốc\nMimeType = text/html\nProcessed = false]
        B5 --> B6[Task.Run background]
    end

    subgraph UPLOAD_TEXT [Luồng C: Paste Text]
        C1[LibraryView\nSave as Doc button] --> C2[LibraryViewModel\nSavePasteTextAsync\nCheck ≤ 50000 chars]
        C2 --> C3[ApiClient.UploadTextAsync\nPOST /api/documents/upload-text]
        C3 --> C4[DocumentController.UploadText\nMemoryStream → Cloudinary]
        C4 --> C5[Lưu Document vào DB\nMimeType = text/plain]
        C5 --> C6[Task.Run background]
    end

    subgraph PROCESS [Background: DocumentProcessorService.ProcessDocumentAsync]
        P1{MimeType?} --> |text/html - web URL| P2[ExtractTextFromUrlAsync\nHttpClient + AutoDecompression\nHtmlAgilityPack extract p/h1/h2/li]
        P1 --> |Cloudinary file URL| P3[DownloadAndExtractAsync\nDownload bytes → temp file]
        P3 --> |.txt| P4[File.ReadAllTextAsync]
        P3 --> |.docx| P5[WordprocessingDocument\nOpenXML extract paragraphs]
        P3 --> |.pdf| P6[PdfPig\nextract page text]
        P2 & P4 & P5 & P6 --> P7{Text rỗng?}
        P7 -- Có --> P8[Processed=true, 0 chunks\nLưu DB]
        P7 -- Không --> P9[ChunkText\nSplit theo paragraph\n~1800 chars/chunk\n200 chars overlap]
        P9 --> P10[Loop từng chunk\nEmbeddingService.GetEmbeddingAsync\nOpenAI text-embedding-3-small\n→ float vector 1536 dims]
        P10 --> P11[Lưu DocumentChunk vào DB\nContent + EmbeddingVector]
        P11 --> P12[Processed=true\nLưu DB]
    end

    A7 & B6 & C6 --> P1
```

---

## 3. Generate Quiz Flow

```mermaid
flowchart TD
    A[CreateQuizView\nUser chọn Document + options] --> B[CreateQuizViewModel\nGenerateQuizCommand]
    B --> C[ApiClient.GenerateQuizAsync\nPOST /api/quizzes/generate\ndocumentId, questionCount\ndifficulty, questionType, title]
    C --> D[QuizController.GenerateQuiz]
    D --> E[Verify document thuộc user\nKiểm tra Processed = true]
    E --> F[EmbeddingService.GetTopKChunksAsync\nRAG - Retrieval]

    subgraph RAG [RAG: Tìm chunks liên quan nhất]
        F --> F1[Load tất cả chunks có EmbeddingVector\ntừ DocumentChunks table]
        F1 --> F2[GetEmbeddingAsync query\nQuery = generate N questions difficulty: X\n→ float vector 1536 dims]
        F2 --> F3[CosineSimilarity\nSo sánh query vector vs mỗi chunk vector\ndot product / norm_a * norm_b]
        F3 --> F4[Lấy top-k chunks\nk = min 8, questionCount+3\nSắp xếp lại theo ChunkIndex để coherent]
    end

    F4 --> G[Build context string\nCHUNK_0 ... CHUNK_N\nGhép nội dung top chunks]

    G --> H[OpenAIService.GenerateQuizJson\ngpt-4o-mini]

    subgraph PROMPT [Prompt Engineering]
        H --> H1{questionType?}
        H1 -- mcq --> H2[Instruction: 4 options, type=mcq]
        H1 -- true_false --> H3[Instruction: 2 options True/False]
        H1 -- fill_blank --> H4[Instruction: prompt có ___, answer trong rubric]
        H1 -- mixed --> H5[Instruction: 1/3 MCQ + 1/3 TF + 1/3 Fill]
        H2 & H3 & H4 & H5 --> H6[System prompt:\nCreate exactly N questions\nDifficulty: X\nReturn ONLY valid JSON\nContext: chunks text]
    end

    H6 --> I[OpenAI API call\nChatClient.CompleteChatAsync]
    I --> J[Raw JSON response]

    subgraph PARSE [Parse & Validate AI Response]
        J --> J1[Strip markdown code blocks\nnếu có backtick]
        J1 --> J2[JsonSerializer.Deserialize\nQuizJsonResponse\nPropertyNameCaseInsensitive]
        J2 --> J3{Parse thành công\nvà có questions?}
        J3 -- Không --> J4[502 error trả về client]
    end

    J3 -- Có --> K[Tạo Quiz entity\nTitle = dto.Title ?? auto từ fileName\nPublished = false]
    K --> L[Loop từng question\nTạo Question entity\nType, Prompt, MaxScore=1, RubricJson]
    L --> M[Loop từng option\nTạo QuestionOption\nContent, IsCorrect]
    M --> N[SaveChangesAsync\nLưu tất cả vào DB]
    N --> O[Trả về QuizDto\nid, title, questionCount]
    O --> P[CreateQuizViewModel\nThêm vào đầu Quizzes list\nClear QuizTitle field]
```

---

## 4. Take Quiz & Submit Flow

```mermaid
flowchart TD
    A[CreateQuizView\nBấm Take Quiz button] --> B[CreateQuizViewModel\nTakeQuizCommand]
    B --> C[MainWindowViewModel\nNavigateToTakeQuiz\nquizId, quizTitle]
    C --> D[TakeQuizViewModel constructor\nStartQuizAsync]

    subgraph START [Start Attempt]
        D --> E[ApiClient.StartAttemptAsync\nPOST /api/attempts/start]
        E --> F[AttemptController.StartAttempt]
        F --> G{Quiz Published\nhoặc creator?}
        G -- Không → creator check fail --> H[400 Bad Request]
        G -- Đúng --> I[Tạo QuizAttempt\nStatus=in_progress\nMaxTotalScore = sum MaxScore]
        I --> J[Trả attemptId về]
    end

    J --> K[ApiClient.GetQuizForAttemptAsync\nGET /api/quizzes/id/attempt\nLấy questions + options]
    K --> L[TakeQuizView hiện câu đầu\nLoadQuestion index=0]

    subgraph ANSWER [User trả lời từng câu]
        L --> M{Question Type?}
        M -- mcq --> N[ItemsControl hiện 4 options\nClick OnOptionClick\nSelectOptionCommand]
        M -- true_false --> O[2 nút True/False\nOnTrueClick / OnFalseClick\nSelectTrueFalse bool\nTrueFalseAnswer binding\nBoolToSelectedBrushConverter]
        M -- fill_blank --> P[TextBox EssayAnswer]
        N & O & P --> Q[SaveCurrentAnswer\n_answers dict QuestionId → OptionId/Text]
        Q --> R{Next/Previous?}
        R -- Next --> S[LoadQuestion index+1\nRestore answer nếu đã làm]
        R -- Submit --> T[SubmitAsync]
    end

    subgraph SUBMIT [Submit & Grade]
        T --> U[ApiClient.SubmitAttemptAsync\nPOST /api/attempts/id/submit\nGửi List answers]
        U --> V[AttemptController.SubmitAttempt]
        V --> W[Loop từng answer]
        W --> X{Question Type?}
        X -- mcq/true_false --> Y[Kiểm tra SelectedOption.IsCorrect\nautoScore = MaxScore hoặc 0]
        X -- fill_blank --> Z[So sánh lowercase string\nautoScore = MaxScore hoặc 0\nfeedbackJson]
        X -- essay --> AA[OpenAIService.GradeEssay\ngpt-4o-mini chấm điểm\ntrả JSON score/feedback]
        Y & Z & AA --> AB[Lưu AttemptAnswer\nAutoScore, FeedbackJson]
        AB --> AC[attempt.TotalScore = sum\nStatus = graded\nFinishedAt = now]
        AC --> AD[Trả TotalScore / MaxTotalScore về]
    end

    AD --> AE[TakeQuizViewModel\nNavigateToResult]
    AE --> AF[ResultView\nhiện Score + từng câu\nĐúng/Sai + feedback]
```

---

## 5. Publish Quiz Flow

```mermaid
flowchart TD
    A[CreateQuizView\nBấm nút Draft/Published] --> B[OnTogglePublish\ncode-behind]
    B --> C[CreateQuizViewModel\nTogglePublishCommand\nquiz param]
    C --> D[ApiClient.PublishQuizAsync\nPATCH /api/quizzes/id/publish\nbody: published = !current]
    D --> E[QuizController.PublishQuiz\nVerify CreatorId = userId]
    E --> F[quiz.Published = dto.Published\nSaveChangesAsync]
    F --> G[Trả id + Published mới]
    G --> H[ViewModel\nQuizzes index = quiz with Published=newState\nRecord immutable update]
    H --> I[UI tự refresh\nBinding hiện Draft/Published mới]
```

---

## 6. History Flow

```mermaid
flowchart TD
    A[Header bấm History] --> B[MainWindowViewModel\nNavigateToHistory]
    B --> C[HistoryViewModel constructor\nLoadHistoryAsync]
    C --> D[ApiClient.GetMyAttemptsAsync\nGET /api/attempts/my]
    D --> E[AttemptController.GetMyAttempts\nJoin QuizAttempts + Quizzes\nWhere UserId = current]
    E --> F[Trả List AttemptSummaryDto\nquizTitle, totalScore, maxScore\nstatus, startedAt, finishedAt]
    F --> G[HistoryView\nhiện danh sách\nScore % + thời gian]
    G --> H{Bấm View Result?}
    H --> I[ApiClient.GetAttemptResultAsync\nGET /api/attempts/id/result]
    I --> J[AttemptController.GetResult\nInclude Answers + Questions + Options]
    J --> K[ResultView\ntừng câu: prompt, selected, isCorrect\nautoScore, feedback]
```
