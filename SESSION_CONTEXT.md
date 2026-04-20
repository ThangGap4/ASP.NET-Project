# QuizAI - Dev Context (Apr 20, 2026)

## Stack
- ASP.NET Core API + Avalonia Desktop App
- API: http://localhost:5127
- DB: PostgreSQL (Render)
- AI: OpenAI GPT-4o-mini
- Storage: Cloudinary
- Packaging: Snap (Linux)

---

## Credentials
| Email | Password | Role |
|-------|----------|------|
| admin@gmail.com | Admin@123 | Admin |
| thangenz0507@gmail.com | 123123 | Student |
| student@test.com | Test@123 | Student |
| test@example.com | Test@123 | Student |

---

## Sample Data

### admin@gmail.com (4 quizzes, 2 attempts)
- **Documents**: Machine Learning Basics (TEXT), Python Wikipedia (URL), history_sample (FILE), AI Basics (TEXT)
- **Quizzes**: ML Quiz (5q), Python Quiz (5q), WWII Quiz (5q), AI Basics Quiz (3q) — all published

### thangenz0507@gmail.com
- **Documents** (10): Python, Vietnam History, Cell Biology, Physics Laws, Economics, programming file, vietnam_history file, biology file, Git Wikipedia (URL), Docker Wikipedia (URL)
- **Quizzes** (5): Python Quiz (5/5), Vietnam History (4/5), Cell Biology (5/5), Physics Laws (5/5), Economics (5/5)
- **Stats**: 7 attempts, avg 96%

---

## API Endpoints

### Documents
- `GET /api/documents` → paginated `{ items, totalCount, page }`
- `POST /api/documents/upload-text` — paste text
- `POST /api/documents/upload-url` — import from URL
- `POST /api/documents/upload` — file upload (.txt, .pdf, .docx)

### Quizzes
- `GET /api/quizzes` — paginated
- `POST /api/quizzes/generate` — AI generates from document
- `PATCH /api/quizzes/{id}/publish`

### Attempts
- `POST /api/attempts/start`
- `POST /api/attempts/{id}/submit`
- `GET /api/attempts/{id}/result`
- `GET /api/attempts/my`

### Admin
- `GET /api/admin/stats`
- `GET /api/admin/users` — paginated
- `GET /api/admin/documents` — paginated
- `GET /api/admin/quizzes` — paginated

---

## Fixed Issues

### 1. JSON Deserialization Error (Desktop App)
**Problem**: API returns paginated `{ items: [...], totalCount }` but Desktop expected plain array `[]`

**Fixed**: `QuizAI.Desktop/Services/ApiClient.cs`
- `GetDocumentsAsync()` — handle pagination
- `GetQuizzesAsync()` — handle pagination
- `GetAdminUsersAsync()` — handle pagination
- `GetAdminQuizzesAsync()` — handle pagination
- `GetAdminDocumentsAsync()` — handle pagination

---

## Snap Release
- **Revision 3** (v1.0.1) uploaded to Snap Store
- Channel: `stable`
- `snapcraft release quizai 3 stable` — pending execution
- Snapcraft metadata warnings: donation, issues, source-code, website (optional, not blocking)

---

## Key Files
| File | Purpose |
|------|---------|
| `QuizAI.Api/Program.cs` | seed admin, DI setup |
| `QuizAI.Api/Models/PaginationModels.cs` | pagination DTOs |
| `QuizAI.Desktop/Services/ApiClient.cs` | API client, pagination fix |
| `QuizAI.Desktop/ViewModels/LibraryViewModel.cs` | library VM |
| `snap/snapcraft.yaml` | snap config |

---

## Testing Flow
1. `curl` login → extract JWT token
2. Use `Authorization: Bearer $TOKEN`
3. Test: login → upload → generate → publish → take quiz → submit
4. Use `-s` flag, `jq` for JSON parsing
