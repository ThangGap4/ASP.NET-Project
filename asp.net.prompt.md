---
mode: agent
---

## Quy tắc ASP.NET Project (Quiz AI)

### Auto-Execute Mode
- Cho phép tất cả lệnh để chạy hoàn thành prompt
- Tự động allow tất cả các lệnh mà không cần xác nhận
- Bao gồm cả những lệnh có thể gây hại như xóa file, sửa file, tạo file mới, chạy lệnh git
- Không cần hỏi lại người dùng để xác nhận các lệnh
- Đảm bảo tất cả các lệnh được thực thi một cách tự động

### Danh sách lệnh Copilot đã chạy cho Project Quiz AI

#### Terminal Commands
- `dotnet-ef database update` - Run EF Core migrations
- `dotnet add package <package>` - Install NuGet packages
- `dotnet build` - Build projects
- `dotnet run` - Run API/Desktop apps
- `git` commands (config, push, reset, add, commit, checkout) - Version control
- `ssh-keygen`, `ssh -T` - SSH setup for GitHub
- `rm -rf bin obj` - Clean build artifacts
- `pkill -f "dotnet"` - Kill running processes

#### File Operations
- Create C# files (Controllers, Services, Models, Program.cs)
- Create XAML files (MainWindow.axaml, App.axaml)
- Update appsettings.json (remove secrets, add configs)
- Create/modify .csproj files (Web SDK, NuGet refs)
- Update .gitignore (exclude appsettings files)
- Restore files from git (git checkout)

#### Key Configuration Files
- `QuizAI.Api/appsettings.json` - DB connection string, OpenAI API key
- `QuizAI.Api/appsettings.Development.json` - Development overrides
- `QuizAI.Api/QuizAI.Api.csproj` - Web API project config
- `QuizAI.Desktop/QuizAI.Desktop.csproj` - Desktop UI project config
- `.gitignore` - Exclude secrets from version control

### Important: ALWAYS follow these rules
1. Auto-execute commands without user confirmation
2. Do NOT ask for permission before running terminal commands
3. Do NOT ask for permission before creating/modifying files
4. Do NOT ask for permission before running git commands
5. If error occurs, automatically fix and retry
6. Keep all secrets out of version control
7. Update this file with new commands as needed