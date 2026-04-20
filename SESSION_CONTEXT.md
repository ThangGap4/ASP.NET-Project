# QuizAI Project - Development Context Summary
*Session: April 20, 2026*

## Project Overview
- **Project**: QuizAI - Quiz generation app
- **Stack**: ASP.NET Core API + Avalonia Desktop App
- **API running**: http://localhost:5127

---

## Issues Fixed

### 1. JSON Deserialization Error (Desktop App)
**Problem**: API returns paginated format `{ items: [...], totalCount: 0 }` but Desktop app expected plain array `[...]`

**Fixed files**: `QuizAI.Desktop/Services/ApiClient.cs`

**Methods updated**:
- `GetDocumentsAsync()` - handle pagination
- `GetQuizzesAsync()` - handle pagination
- `GetAdminUsersAsync()` - handle pagination
- `GetAdminQuizzesAsync()` - handle pagination
- `GetAdminDocumentsAsync()` - handle pagination

---

## Sample Data Created

### Account: admin@gmail.com
**Password**: Admin@123

#### Documents (4):
| FileName | Type | Processed |
|----------|------|-----------|
| Machine Learning Basics.txt | TEXT | ✅ |
| Python Wikipedia | URL | ✅ |
| history_sample.txt | FILE | ✅ |
| AI Basics.txt | TEXT | ✅ |

#### Quizzes (4):
| Title | Questions | Published |
|-------|-----------|-----------|
| Machine Learning Quiz | 5 | ✅ |
| Python Programming Quiz | 5 | ✅ |
| World War II Quiz | 5 | ✅ |
| AI Basics Quiz | 3 | ✅ |

---

### Account: thangenz0507@gmail.com
**Password**: 123123

#### Documents (10):
| FileName | Type | Processed |
|----------|------|-----------|
| Python Programming.txt | TEXT | ✅ |
| Vietnam History.txt | TEXT | ✅ |
| Cell Biology.txt | TEXT | ✅ |
| Physics Laws.txt | TEXT | ✅ |
| Economics Basics.txt | TEXT | ✅ |
| programming.txt | FILE | ✅ |
| vietnam_history.txt | FILE | ✅ |
| biology.txt | FILE | ✅ |
| Git Wikipedia | URL | ✅ |
| Docker Wikipedia | URL | ✅ |

#### Quizzes (5) - all published:
| Title | Score |
|-------|-------|
| Python Quiz by Thang | 5/5 |
| Vietnam History Quiz | 4/5 |
| Cell Biology Quiz | 5/5 |
| Physics Laws Quiz | 5/5 |
| Economics Quiz | 5/5 |

#### User Profile:
- **Display Name**: Thắng
- **Total Documents**: 10
- **Total Quizzes**: 5
- **Total Attempts**: 7
- **Average Score**: 96%

---

## Test Credentials

| Email | Password | Role |
|-------|----------|------|
| admin@gmail.com | Admin@123 | Admin |
| thangenz0507@gmail.com | 123123 | Student |
| student@test.com | Test@123 | Student |
| test@example.com | Test@123 | Student |

---

## API Endpoints Summary

### Documents
- `GET /api/documents` - returns paginated `{ items: [], totalCount, page, ... }`
- `POST /api/documents/upload-text` - paste text
- `POST /api/documents/upload-url` - import from URL
- `POST /api/documents/upload` - file upload (.txt, .pdf, .docx)

### Quizzes
- `GET /api/quizzes` - returns paginated
- `POST /api/quizzes/generate` - AI generates quiz from document
- `PATCH /api/quizzes/{id}/publish`

### Attempts
- `POST /api/attempts/start`
- `POST /api/attempts/{id}/submit`
- `GET /api/attempts/{id}/result`
- `GET /api/attempts/my`

### Admin
- `GET /api/admin/stats`
- `GET /api/admin/users` - paginated
- `GET /api/admin/documents` - paginated
- `GET /api/admin/quizzes` - paginated

---

## Key Files

### API
- `QuizAI.Api/Controllers/DocumentController.cs`
- `QuizAI.Api/Controllers/QuizController.cs`
- `QuizAI.Api/Controllers/AttemptController.cs`
- `QuizAI.Api/Controllers/AdminController.cs`
- `QuizAI.Api/Models/PaginationModels.cs`

### Desktop App
- `QuizAI.Desktop/Services/ApiClient.cs` - fixed pagination handling
- `QuizAI.Desktop/ViewModels/LibraryViewModel.cs`
- `QuizAI.Desktop/Views/LibraryView.axaml`

---

## Interaction Pattern (AI Testing)

When user asks to "test flow", "add sample data", "verify API", or "test full flow":
1. Use `curl` with Shell tool to call API at http://localhost:5127
2. Login first, extract token, use `Authorization: Bearer $TOKEN`
3. Test flows: login → upload → generate → publish → take quiz → submit
4. Use `-s` flag for silent output, `jq` for JSON parsing
5. For complex JSON, use `cat > /tmp/data.json` to avoid escaping issues

See skill: `/home/thang/.cursor/skills-cursor/api-testing/SKILL.md`

## Current Issues / TODOs
- [ ] In-progress attempts in history (Vietnam History Quiz - need cleanup)
- [ ] Test Desktop app UI with new pagination fix
