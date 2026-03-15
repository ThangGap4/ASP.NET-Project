# PLAN – Quiz AI (.NET + Avalonia + PostgreSQL + OpenAI)

## Tổng quan stack

| Layer | Công nghệ |
|---|---|
| OS dev | Ubuntu (Linux) |
| IDE | VS Code |
| Ngôn ngữ | C# / .NET 8 |
| Backend | ASP.NET Core Web API |
| Frontend Desktop | Avalonia UI 11.x |
| Database | PostgreSQL (Render) |
| AI | OpenAI API (`openai-dotnet` SDK v2.x) |
| ORM | Entity Framework Core 8 + Npgsql |

## Kiến trúc tổng thể

```
[Avalonia Desktop]
       |
       | HTTP REST
       v
[ASP.NET Core API]
       |
       |-- EF Core --> [PostgreSQL (Render)]
       |
       `-- OpenAI SDK --> [OpenAI API]
                          (sinh câu hỏi, chấm tự luận)
```

> ❌ Avalonia KHÔNG gọi OpenAI trực tiếp  
> ✅ Avalonia CHỈ gọi REST API backend  
> ✅ Backend giữ toàn bộ secret (API key, DB credentials)

---

## Cấu trúc thư mục đích

```
QuizAI/
├── QuizAI.sln.proj
├── PLAN.md
├── .gitignore
│
├── QuizAI.Api/                    ← ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── DocumentController.cs
│   │   ├── QuizController.cs
│   │   └── AttemptController.cs
│   ├── Models/
│   │   ├── AppUser.cs
│   │   ├── Document.cs
│   │   ├── DocumentChunk.cs
│   │   ├── Quiz.cs
│   │   ├── Question.cs
│   │   ├── QuestionOption.cs
│   │   ├── QuizAttempt.cs
│   │   └── AttemptAnswer.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Services/
│   │   ├── OpenAIService.cs
│   │   ├── DocumentProcessorService.cs
│   │   └── EmbeddingService.cs
│   ├── appsettings.json
│   ├── appsettings.Local.json     ← gitignored (chứa secrets)
│   └── Program.cs
│
└── QuizAI.Desktop/                ← Avalonia UI Desktop App
    ├── Views/
    │   ├── LibraryView.axaml
    │   ├── CreateQuizView.axaml
    │   ├── TakeQuizView.axaml
    │   └── ResultView.axaml
    ├── ViewModels/
    │   ├── MainWindowViewModel.cs
    │   ├── LibraryViewModel.cs
    │   ├── CreateQuizViewModel.cs
    │   ├── TakeQuizViewModel.cs
    │   └── ResultViewModel.cs
    ├── Services/
    │   └── ApiClient.cs
    ├── MainWindow.axaml
    └── Program.cs
```

---

## Database Schema (PostgreSQL)

### Entities & Relations

```
app_users ──< documents ──< document_chunks
app_users ──< quizzes <── documents
quizzes ──< questions ──< question_options
app_users ──< quiz_attempts >── quizzes
quiz_attempts ──< attempt_answers >── questions
attempt_answers >── question_options
```

### Models hiện có

| Model | File | Trạng thái |
|---|---|---|
| `AppUser` | `Models/AppUser.cs` | ⚠️ Thiếu `Role`, `PasswordHash` |
| `Document` | `Models/Document.cs` | ⚠️ Có `Content` (không nên lưu DB) |
| `DocumentChunk` | `Models/DocumentChunk.cs` | ⚠️ `Embedding` dùng `Dictionary` sai |
| `Quiz` | `Models/Quiz.cs` | ✅ Cơ bản ổn |
| `Question` | `Models/Question.cs` | ⚠️ Thiếu `MaxScore`, `Rubric` |
| `QuestionOption` | `Models/QuestionOption.cs` | ✅ Ổn |
| `QuizAttempt` | `Models/QuizAttempt.cs` | ⚠️ Thiếu `Status`, `MaxTotalScore` |
| `AttemptAnswer` | `Models/AttemptAnswer.cs` | ⚠️ Thiếu `AutoScore`, `FinalScore`, `Feedback` |

---

## PHASE 0 – Fix lỗi nền (BẮT BUỘC trước khi làm tiếp)

**Trạng thái: ✅ Hoàn thành**

### 0.1 Fix `AppDbContext.cs` bị corrupt

- [x] Xóa toàn bộ nội dung cũ, viết lại `AppDbContext.cs` đúng cú pháp
- [x] Đủ `DbSet` cho tất cả 8 entities
- [x] Cấu hình đầy đủ Fluent API relationships

### 0.2 Fix `OpenAIService.cs` dùng sai SDK

- [x] Sửa lại dùng `ChatClient`, `ChatMessage.CreateUserMessage()`
- [x] `CompleteChatAsync()` đúng theo `openai-dotnet` v2.x

### 0.3 EF Core migration + Render PostgreSQL

- [x] Bật `ImplicitUsings` + `Nullable` trong `.csproj`
- [x] Fix `Program.cs`: `WebApplication.CreateBuilder` (sai `WebApplicationBuilder`)
- [x] Chạy `dotnet ef migrations add InitialCreate` thành công
- [x] Drop DB cũ (có bảng conflict), recreate và `database update` thành công
- [x] Tất cả 8 bảng + indexes đã được tạo trên Render PostgreSQL
- [x] API chạy được, query DB trả về response đúng (`GET /api/quiz` → `[]`)

---

## PHASE 1 – Models hoàn chỉnh

**Trạng thái: ✅ Hoàn thành (thực hiện cùng Phase 0)**

- [x] `AppUser`: `Role`, `PasswordHash`, `DisplayName`, `LastLogin`
- [x] `Document`: bỏ `Content`, thêm `StorageUrl`, `OwnerId`, `MimeType`, `FileSize`, `Processed`
- [x] `DocumentChunk`: `float[] EmbeddingVector`, `TokenCount`
- [x] `Question`: `Seq`, `Prompt`, `MaxScore`, `RubricJson`, `SourceChunkIdsJson`
- [x] `QuizAttempt`: `Status`, `MaxTotalScore`, `FinishedAt`
- [x] `AttemptAnswer`: `SelectedOptionId`, `AutoScore`, `ManualScore`, `FeedbackJson`, `GradedAt`

---

## PHASE 2 – Backend API hoàn chỉnh

**Trạng thái: ⚠️ Partial (thiếu Auth, thiếu Attempt)**

### 2.1 Authentication – `AuthController.cs`

**Trạng thái: ❌ Chưa có**

- [ ] `POST /api/auth/register` – Đăng ký user (student)
- [ ] `POST /api/auth/login` – Đăng nhập, trả JWT token
- [ ] Cài package `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Hash password bằng `BCrypt.Net` hoặc `ASP.NET Identity`
- [ ] Middleware xác thực JWT trong `Program.cs`

### 2.2 Document Controller – `DocumentController.cs`

**Trạng thái: ⚠️ Chỉ đọc được `.txt`**

- [ ] `POST /api/documents/upload` – Hỗ trợ đầy đủ:
  - `.txt` – đọc trực tiếp ✅ (đã có)
  - `.pdf` – cài `PdfPig` (`UglyToad.PdfPig`)
  - `.docx` – cài `DocumentFormat.OpenXml`
  - URL web – dùng `HttpClient` + `HtmlAgilityPack`
- [ ] Sau khi extract text → tự động chunk (300–800 tokens/chunk)
- [ ] Lưu `Document` + danh sách `DocumentChunk` vào DB
- [ ] `GET /api/documents` – Lấy danh sách documents của user
- [ ] `DELETE /api/documents/{id}` – Xóa document
- [ ] Thêm `[Authorize]` cho tất cả endpoints

### 2.3 Quiz Controller – `QuizController.cs`

**Trạng thái: ⚠️ CRUD cơ bản nhưng `UserId` hardcode**

- [ ] Fix `UserId` lấy từ JWT claim thật (không hardcode `Guid.NewGuid()`)
- [ ] `POST /api/quizzes/generate` – Sinh quiz từ document qua OpenAI:
  - Nhận: `documentId`, `questionCount`, `difficulty`
  - Lấy top-k chunks liên quan (RAG)
  - Gọi OpenAI sinh JSON câu hỏi
  - Parse và lưu `Quiz` + `Question` + `QuestionOption`
- [ ] `GET /api/quizzes/{id}/questions` – Lấy câu hỏi (ẩn `is_correct`)
- [ ] `PATCH /api/quizzes/{id}/publish` – Admin publish quiz
- [ ] Thêm `[Authorize]` cho tất cả endpoints

### 2.4 Attempt Controller – `AttemptController.cs`

**Trạng thái: ❌ Chưa có**

- [ ] `POST /api/attempts` – Bắt đầu làm quiz (tạo `QuizAttempt`)
- [ ] `POST /api/attempts/{id}/submit` – Nộp bài:
  - MCQ: chấm local so `is_correct`
  - Tự luận: gọi `OpenAIService.GradeEssay()`
  - Tính tổng điểm, lưu `AttemptAnswer`
  - Cập nhật `QuizAttempt.Status = "graded"`
- [ ] `GET /api/attempts/{id}/result` – Xem kết quả chi tiết
- [ ] `GET /api/attempts/my` – Lịch sử làm bài của user
- [ ] Thêm `[Authorize]` cho tất cả endpoints

---

## PHASE 3 – Services & AI

**Trạng thái: ⚠️ Partial**

### 3.1 Fix `OpenAIService.cs`

- [ ] Sửa đúng SDK `openai-dotnet` v2.x:
  - Dùng `ChatClient` thay vì `OpenAIClient.CreateChatCompletionAsync`
  - `ChatMessage.CreateUserMessage(prompt)`
- [ ] `GenerateQuizJson(string context, int count, string difficulty)` – sinh MCQ
- [ ] `GradeEssay(string question, string studentAnswer, string rubric)` – chấm tự luận
- [ ] Retry logic khi gặp lỗi JSON parse

### 3.2 Tạo `DocumentProcessorService.cs`

**Trạng thái: ❌ Chưa có**

- [ ] `ExtractText(string filePath, string mimeType)` – extract text từ file
- [ ] `ChunkText(string text, int maxTokens = 500)` – cắt text thành chunks
- [ ] `ProcessDocument(Guid documentId)` – orchestrate toàn bộ flow

### 3.3 Tạo `EmbeddingService.cs`

**Trạng thái: ❌ Chưa có**

- [ ] Dùng OpenAI `text-embedding-3-small` (1536 dim)
- [ ] `GetEmbedding(string text)` → `float[]`
- [ ] `GetTopKChunks(Guid documentId, string query, int k = 5)` – cosine similarity
- [ ] Lưu embedding vào `DocumentChunk.EmbeddingVector`

---

## PHASE 4 – Avalonia UI

**Trạng thái: ❌ Views/ rỗng, MainWindow chỉ có 3 button giả**

### 4.1 Cài thêm packages cho Desktop

- [ ] `CommunityToolkit.Mvvm` – MVVM helpers
- [ ] `Avalonia.ReactiveUI` – hoặc dùng CommunityToolkit.Mvvm

### 4.2 Fix `ApiClient.cs`

- [ ] Implement thật: `GetFromJsonAsync`, `PostAsJsonAsync`
- [ ] Thêm `Authorization: Bearer {token}` header
- [ ] Thêm các method:
  - `LoginAsync(string email, string password)`
  - `UploadDocumentAsync(string filePath)`
  - `GenerateQuizAsync(Guid documentId, int count)`
  - `GetQuizQuestionsAsync(Guid quizId)`
  - `SubmitAttemptAsync(SubmitDto dto)`
  - `GetResultAsync(Guid attemptId)`

### 4.3 Tạo Views & ViewModels

**Màn 1 – Login (`LoginView.axaml`)**
- [ ] Form nhập email + password
- [ ] Nút Login gọi `AuthController`
- [ ] Lưu JWT token vào memory

**Màn 2 – Library (`LibraryView.axaml`)**
- [ ] Danh sách tài liệu đã upload
- [ ] Button Import File (mở file picker: PDF, DOCX, TXT)
- [ ] Input URL web
- [ ] Trạng thái processing (loading indicator)

**Màn 3 – Create Quiz (`CreateQuizView.axaml`)**
- [ ] Chọn document từ danh sách
- [ ] Nhập số câu (5/10/15/20)
- [ ] Chọn độ khó (Easy/Medium/Hard)
- [ ] Nút Generate Quiz
- [ ] Loading + hiện danh sách quiz đã tạo

**Màn 4 – Take Quiz (`TakeQuizView.axaml`)**
- [ ] Hiển thị từng câu hỏi
- [ ] Radio button cho MCQ
- [ ] TextBox cho câu tự luận
- [ ] Nút Next/Previous
- [ ] Nút Submit

**Màn 5 – Result (`ResultView.axaml`)**
- [ ] Tổng điểm / điểm tối đa
- [ ] Chi tiết từng câu: đúng/sai, điểm, feedback
- [ ] Trích dẫn nguồn (chunk id)

---

## PHASE 5 – Build & Deploy

**Trạng thái: ❌ Chưa làm**

### 5.1 Publish Desktop App

```bash
cd QuizAI.Desktop
dotnet publish -c Release -r linux-x64 --self-contained true
dotnet publish -c Release -r win-x64 --self-contained true
```

### 5.2 Deploy API lên Render

- [ ] Tạo `Dockerfile` cho `QuizAI.Api`
- [ ] Set environment variables trên Render:
  - `ConnectionStrings__DefaultConnection`
  - `OpenAI__ApiKey`
  - `Jwt__SecretKey`
- [ ] Connect Render PostgreSQL (đã có connection string trong `appsettings.Local.json`)

### 5.3 Cấu hình Desktop kết nối API production

- [ ] Tạo `appsettings.json` trong Desktop với `ApiBaseUrl`
- [ ] Switch URL theo môi trường (dev/prod)

---

## Checklist tổng (theo thứ tự ưu tiên)

### Ngay bây giờ (fix lỗi)
- [x] **Fix `AppDbContext.cs`** – Xóa duplicate, viết lại sạch
- [x] **Fix `OpenAIService.cs`** – Dùng đúng SDK API
- [x] **Chạy EF migration** – Tạo bảng trên Render Postgres

### Tuần 1 (backend core)
- [ ] Cập nhật Models (Phase 1)
- [ ] Tạo `AuthController.cs` + JWT
- [ ] Fix `DocumentController.cs` – thêm PDF/DOCX support
- [ ] Fix `QuizController.cs` – thêm generate endpoint
- [ ] Tạo `AttemptController.cs`

### Tuần 2 (services + AI)
- [ ] Fix `OpenAIService.cs` – đúng SDK
- [ ] Tạo `DocumentProcessorService.cs`
- [ ] Tạo `EmbeddingService.cs` (RAG)

### Tuần 3 (desktop UI)
- [ ] Fix `ApiClient.cs` – implement thật
- [ ] Tạo đủ 5 Views + ViewModels
- [ ] Test end-to-end: upload → generate → làm bài → kết quả

### Tuần 4 (polish + deploy)
- [ ] Dockerfile cho API
- [ ] Deploy lên Render
- [ ] Build desktop app self-contained
- [ ] Test trên Windows + Linux

---

## Ghi chú quan trọng

- **Không lưu file binary vào DB** – Lưu file ra folder `uploads/` local, lưu path vào `Document.StorageUrl`
- **Không lưu `is_correct` trong response API cho student** – Chỉ trả sau khi submit
- **MCQ chấm local** – Không cần gọi OpenAI, tiết kiệm token
- **Chỉ gọi AI khi**: sinh câu hỏi + chấm tự luận
- **`appsettings.Local.json` đã có secrets** – đảm bảo đã gitignore
- **DB đã có trên Render**: `quizz_asp_net` tại `virginia-postgres.render.com`

---

## NuGet Packages cần thêm

### QuizAI.Api
| Package | Mục đích | Trạng thái |
|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider | ✅ Đã có |
| `Microsoft.EntityFrameworkCore.Design` | EF migrations | ✅ Đã có |
| `Swashbuckle.AspNetCore` | Swagger | ✅ Đã có |
| `OpenAI` v2.x | OpenAI SDK | ✅ Đã có |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT Auth | ❌ Cần thêm |
| `BCrypt.Net-Next` | Hash password | ❌ Cần thêm |
| `UglyToad.PdfPig` | Đọc PDF | ❌ Cần thêm |
| `DocumentFormat.OpenXml` | Đọc DOCX | ❌ Cần thêm |
| `HtmlAgilityPack` | Parse HTML từ URL | ❌ Cần thêm |

### QuizAI.Desktop
| Package | Mục đích | Trạng thái |
|---|---|---|
| `Avalonia` v11.x | UI framework | ✅ Đã có |
| `Avalonia.Themes.Fluent` | Theme | ✅ Đã có |
| `CommunityToolkit.Mvvm` | MVVM | ❌ Cần thêm |
