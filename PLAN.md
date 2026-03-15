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

**Trạng thái: ✅ Hoàn thành**

### 2.1 Authentication – `AuthController.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] `POST /api/auth/register` – Đăng ký user (student)
- [x] `POST /api/auth/login` – Đăng nhập, trả JWT token
- [x] Cài package `Microsoft.AspNetCore.Authentication.JwtBearer`
- [x] Hash password bằng `BCrypt.Net-Next`
- [x] Middleware xác thực JWT trong `Program.cs`

### 2.2 Document Controller – `DocumentController.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] `POST /api/documents/upload` – Hỗ trợ đầy đủ:
  - `.txt` – đọc trực tiếp ✅
  - `.pdf` – cài `PdfPig` ✅
  - `.docx` – cài `DocumentFormat.OpenXml` ✅
  - URL web – `POST /api/documents/upload-url` ✅
- [x] Sau khi upload → tự động process background (chunk + embed)
- [x] Lưu `Document` + danh sách `DocumentChunk` vào DB
- [x] `GET /api/documents` – Lấy danh sách documents của user
- [x] `GET /api/documents/{id}/chunks` – Xem chunks (debug)
- [x] `DELETE /api/documents/{id}` – Xóa document
- [x] `[Authorize]` cho tất cả endpoints

### 2.3 Quiz Controller – `QuizController.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] Fix `UserId` lấy từ JWT claim thật
- [x] `POST /api/quizzes/generate` – Sinh quiz từ document qua OpenAI (RAG)
  - Nhận: `documentId`, `questionCount`, `difficulty`
  - Lấy top-k chunks liên quan (cosine similarity)
  - Gọi OpenAI sinh JSON câu hỏi
  - Parse và lưu `Quiz` + `Question` + `QuestionOption`
- [x] `GET /api/quizzes/{id}` – Lấy câu hỏi (ẩn `is_correct`)
- [x] `PATCH /api/quizzes/{id}/publish` – Publish/unpublish quiz
- [x] `[Authorize]` cho tất cả endpoints

### 2.4 Attempt Controller – `AttemptController.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] `POST /api/attempts/start` – Bắt đầu làm quiz (tạo `QuizAttempt`)
- [x] `POST /api/attempts/{id}/submit` – Nộp bài:
  - MCQ: chấm local so `is_correct` ✅
  - Tự luận: gọi `OpenAIService.GradeEssay()` ✅
  - Tính tổng điểm, lưu `AttemptAnswer` ✅
  - Cập nhật `QuizAttempt.Status = "graded"` ✅
- [x] `GET /api/attempts/{id}/result` – Xem kết quả chi tiết
- [x] `GET /api/attempts/my` – Lịch sử làm bài của user
- [x] `[Authorize]` cho tất cả endpoints

---

## PHASE 3 – Services & AI

**Trạng thái: ✅ Hoàn thành**

### 3.1 `OpenAIService.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] Dùng đúng SDK `openai-dotnet` v2.x (`ChatClient`, `ChatMessage.CreateUserMessage()`)
- [x] `GenerateQuizJson(string context, int count, string difficulty)` – sinh MCQ
- [x] `GradeEssay(string question, string studentAnswer, string rubric)` – chấm tự luận

### 3.2 `DocumentProcessorService.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] `ExtractTextAsync(string filePath, string mimeType)` – extract text từ .txt/.pdf/.docx
- [x] `ExtractTextFromUrlAsync(string url)` – fetch + parse HTML từ URL
- [x] `ChunkText(string text)` – cắt text thành chunks ~1800 chars (overlap 200)
- [x] `ProcessDocumentAsync(Guid documentId)` – orchestrate toàn bộ flow (extract→chunk→embed→save)

### 3.3 `EmbeddingService.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] Dùng OpenAI `text-embedding-3-small`
- [x] `GetEmbeddingAsync(string text)` → `float[]`
- [x] `CosineSimilarity(float[] a, float[] b)` – tính điểm tương đồng
- [x] `GetTopKChunksAsync(Guid documentId, string query, int k)` – RAG retrieval
- [x] `GetTopKChunksAcrossDocumentsAsync(...)` – tìm trong nhiều document

---

## PHASE 4 – Avalonia UI

**Trạng thái: ✅ Hoàn thành**

### 4.1 Packages cho Desktop

- [x] `CommunityToolkit.Mvvm` 8.4.0 – MVVM helpers (ObservableObject, RelayCommand, ObservableProperty)

### 4.2 `ApiClient.cs`

**Trạng thái: ✅ Hoàn thành**

- [x] Implement thật: `GetFromJsonAsync`, `PostAsJsonAsync`, `MultipartFormDataContent`
- [x] `SetToken(token)` – thêm `Authorization: Bearer` header
- [x] Methods đầy đủ:
  - `LoginAsync` / `RegisterAsync`
  - `GetDocumentsAsync` / `UploadDocumentAsync` / `UploadUrlAsync` / `DeleteDocumentAsync`
  - `GetQuizzesAsync` / `GetQuizAsync` / `GenerateQuizAsync` / `DeleteQuizAsync`
  - `StartAttemptAsync` / `SubmitAttemptAsync` / `GetResultAsync` / `GetMyAttemptsAsync`

### 4.3 ViewModels (CommunityToolkit.Mvvm)

- [x] `MainWindowViewModel` – navigation hub, điều phối giữa các Views
- [x] `LoginViewModel` – login/register, lưu JWT token
- [x] `LibraryViewModel` – quản lý documents, upload file/URL
- [x] `CreateQuizViewModel` – chọn doc, generate quiz, list quizzes
- [x] `TakeQuizViewModel` – làm bài, next/prev, submit
- [x] `ResultViewModel` – xem kết quả, parse feedback JSON

### 4.4 Views (Avalonia XAML)

- [x] `LoginView.axaml` – form login/register, toggle mode
- [x] `LibraryView.axaml` – list docs, file picker, URL import, delete
- [x] `CreateQuizView.axaml` – chọn doc, số câu, độ khó, generate, list quizzes
- [x] `TakeQuizView.axaml` – MCQ options, essay textbox, next/prev/submit
- [x] `ResultView.axaml` – score summary, chi tiết từng câu, feedback
- [x] `MainWindow.axaml` – DataTemplates routing, top bar khi logged in

---

## PHASE 5 – Build & Deploy

**Trạng thái: ✅ Hoàn thành**

### 5.1 Publish Desktop App

- [x] Script `build-desktop.sh` – tự động build self-contained cho linux-x64/win-x64/osx-x64
- [x] `dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true`

### 5.2 Deploy API lên Render

- [x] Tạo `Dockerfile` (multi-stage build, SDK → aspnet runtime)
- [x] Tạo `.dockerignore` (exclude Desktop, bin, obj)
- [x] Tạo `render.yaml` – deploy config cho Render
- [x] `Program.cs` đọc `PORT` env var từ Render
- [x] Swagger bật ở tất cả môi trường
- [ ] Set environment variables trên Render dashboard:
  - `ConnectionStrings__DefaultConnection` ← connection string PostgreSQL
  - `OpenAI__ApiKey` ← OpenAI API key
  - `Jwt__Secret` ← secret key cho JWT

### 5.3 Cấu hình Desktop kết nối API production

- [ ] Cập nhật `ApiClient.cs` base URL từ `http://localhost:5127/api/` → URL Render sau khi deploy

---

## Checklist tổng (theo thứ tự ưu tiên)

### Ngay bây giờ (fix lỗi)
- [x] **Fix `AppDbContext.cs`** – Xóa duplicate, viết lại sạch
- [x] **Fix `OpenAIService.cs`** – Dùng đúng SDK API
- [x] **Chạy EF migration** – Tạo bảng trên Render Postgres

### Tuần 1 (backend core)
- [x] Cập nhật Models (Phase 1)
- [x] Tạo `AuthController.cs` + JWT
- [x] Fix `DocumentController.cs` – thêm PDF/DOCX/URL support
- [x] Fix `QuizController.cs` – thêm generate endpoint (RAG)
- [x] Tạo `AttemptController.cs`

### Tuần 2 (services + AI)
- [x] Fix `OpenAIService.cs` – đúng SDK
- [x] Tạo `DocumentProcessorService.cs`
- [x] Tạo `EmbeddingService.cs` (RAG + cosine similarity)

### Tuần 3 (desktop UI)
- [x] Fix `ApiClient.cs` – implement thật (tất cả endpoints)
- [x] Tạo đủ 5 Views + ViewModels (MVVM CommunityToolkit)
- [ ] Test end-to-end: upload → generate → làm bài → kết quả

### Tuần 4 (polish + deploy)
- [x] Dockerfile cho API
- [x] .dockerignore + render.yaml
- [x] Build desktop app self-contained (build-desktop.sh)
- [x] Set env vars trên Render dashboard và deploy
- [x] API live tại https://asp-net-project-9dm5.onrender.com/swagger
- [ ] Test end-to-end: upload → generate → làm bài → kết quả

---

## PHASE 6 – UI hoàn thiện (còn thiếu)

**Trạng thái: ❌ Chưa làm**

### Đánh giá giao diện hiện tại vs cần có

| Chức năng | Backend API | Desktop UI | Ghi chú |
|---|---|---|---|
| Login / Register | ✅ | ✅ | Đã có toggle mode |
| Document Library | ✅ | ✅ | List, upload file/URL, delete |
| Generate Quiz | ✅ | ✅ | Chọn doc, số câu, độ khó |
| Làm bài (MCQ + Essay) | ✅ | ✅ | Next/Prev/Submit |
| Xem kết quả | ✅ | ✅ | Score, feedback từng câu |
| **Lịch sử làm bài** | ✅ `GET /attempts/my` | ❌ Chưa có View | Cần thêm HistoryView |
| **Profile user** | ❌ Chưa có endpoint | ❌ Chưa có View | Cần thêm cả backend lẫn UI |
| **Xem lại kết quả cũ** | ✅ `GET /attempts/{id}/result` | ❌ Chưa navigate được | Cần link từ HistoryView |
| **Lỗi 404 khi load Library** | - | ⚠️ Lỗi hiển thị | Cần debug endpoint |

### 6.1 HistoryView – Lịch sử làm bài

- [ ] Tạo `HistoryViewModel.cs` – gọi `GetMyAttemptsAsync()`
- [ ] Tạo `HistoryView.axaml` – hiển thị danh sách attempts (quiz title, score, ngày làm)
- [ ] Từ History → click vào attempt → navigate đến `ResultView` (xem lại kết quả)
- [ ] Thêm nút "History" vào top bar `MainWindow.axaml`
- [ ] Thêm `NavigateToHistory()` vào `MainWindowViewModel`
- [ ] Thêm `DataTemplate` cho `HistoryViewModel` trong `MainWindow.axaml`

### 6.2 ProfileView – Thông tin user

- [ ] Thêm `GET /api/auth/me` endpoint trả về: `displayName`, `email`, `role`, `lastLogin`, tổng số quiz đã làm, điểm trung bình
- [ ] Tạo `ProfileViewModel.cs`
- [ ] Tạo `ProfileView.axaml` – hiển thị thông tin + stats cơ bản
- [ ] Thêm nút "Profile" vào top bar

### 6.3 Fix lỗi 404 Document Library

- [ ] Debug `GET /api/documents` trên Render – xem log tại dashboard
- [ ] Kiểm tra JWT token có được gửi đúng không trong `GetDocumentsAsync()`

### 6.4 Dashboard / Home View (optional)

- [ ] Sau login hiển thị Dashboard thay vì thẳng vào Library
- [ ] Dashboard gồm: số document, số quiz, số lần làm bài, điểm TB gần nhất
- [ ] Quick actions: Upload Document, Generate Quiz, View History

---

## PHASE 7 – Chất lượng & UX

**Trạng thái: ❌ Chưa làm**

### 7.1 Error handling toàn diện

- [ ] Khi API trả 401 → tự động logout, navigate về Login
- [ ] Khi Render spin down (cold start ~50s) → hiển thị loading spinner với message "Connecting to server..."
- [ ] Timeout tăng lên 120s cho lần request đầu tiên

### 7.2 Loading states

- [ ] Skeleton loading cho danh sách documents và quiz
- [ ] Progress indicator khi document đang processing (polling `GET /api/documents/{id}` mỗi 3s)
- [ ] Disable nút Generate khi document chưa `Processed = true`

### 7.3 Validation

- [ ] Email format validation ở Login/Register
- [ ] Minimum password length (6 chars)
- [ ] Không cho submit quiz nếu còn câu chưa trả lời (hoặc warning)

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
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT Auth | ✅ Đã có |
| `BCrypt.Net-Next` | Hash password | ✅ Đã có |
| `UglyToad.PdfPig` | Đọc PDF | ✅ Đã có |
| `DocumentFormat.OpenXml` | Đọc DOCX | ✅ Đã có |
| `HtmlAgilityPack` | Parse HTML từ URL | ✅ Đã có |

### QuizAI.Desktop
| Package | Mục đích | Trạng thái |
|---|---|---|
| `Avalonia` v11.x | UI framework | ✅ Đã có |
| `Avalonia.Themes.Fluent` | Theme | ✅ Đã có |
| `CommunityToolkit.Mvvm` | MVVM | ✅ Đã có |
 

 ##tính năng cơ bản mà ứng dụng cung cấp
- Upload tài liệu (tạo 1 vùng text area để người dùng có thể patse nội dung copy vào => sau đó chuyển thành file text,file txt, docx,... hoặc URL), có thể chỉnh sửa tài liệu đã upload
- có tuỳ chọn tạo bộ quiz có các tuỳ chọn như: độ khó (dễ , trung bình , khó), số lượng câu (từ 5-30 câu), loại câu hỏi (trắc nghiệm, điền từ , đúng/sai)
 => sinh quiz từ tài liệu (MCQ + Essay)
- Làm quiz với giao diện thân thiện
- Chấm điểm tự động (MCQ chấm local, Essay chấm AI)
- Xem kết quả chi tiết + feedback
- Lịch sử làm bài + xem lại kết quả cũ
- Quản lý profile user (display name, stats cơ bản)