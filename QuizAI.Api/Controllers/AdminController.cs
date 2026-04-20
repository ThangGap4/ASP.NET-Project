using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;
using QuizAI.Api.Models;
using System.Linq;
using System.Threading.Tasks;

namespace QuizAI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/users
        // Quản lý người dùng: Lấy danh sách tất cả người dùng kèm số lượng quiz đã tạo
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationParams pagination)
        {
            var query = _context.Users
                .Select(u => new 
                {
                    u.Id,
                    u.Email,
                    u.DisplayName,
                    u.Role,
                    u.IsBanned,
                    u.CreatedAt,
                    u.LastLogin,
                    QuizCount = u.CreatedQuizzes.Count,
                    AttemptCount = u.QuizAttempts.Count
                })
                .OrderByDescending(u => u.CreatedAt);

            var result = await PaginatedResult<object>.CreateAsync(query, pagination);
            return Ok(result);
        }

        // GET: api/admin/quizzes
        // Quản lý quiz: Lấy danh sách các bài quiz đã được published (chỉ Admin và tác giả mới được thấy theo luồng)
        [HttpGet("quizzes")]
        public async Task<IActionResult> GetPublishedQuizzes([FromQuery] PaginationParams pagination)
        {
            var query = _context.Quizzes
                .Where(q => q.Published)
                .Select(q => new
                {
                    q.Id,
                    q.Title,
                    q.Description,
                    q.CreatedAt,
                    q.Published,
                    CreatorId = q.CreatorId,
                    CreatorName = q.Creator != null ? q.Creator.DisplayName : "Unknown",
                    QuestionCount = q.Questions.Count,
                    AttemptCount = q.Attempts.Count
                })
                .OrderByDescending(q => q.CreatedAt);

            var result = await PaginatedResult<object>.CreateAsync(query, pagination);
            return Ok(result);
        }
        
        
        // ─── ADMIN ACTIONS ───────────────────────────────────────────────────

        // POST: api/admin/users/{id}/toggle-ban
        [HttpPost("users/{id}/toggle-ban")]
        public async Task<IActionResult> ToggleUserBan(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found");

            // Prevent Admin from banning themselves
            if (user.Role == "Admin")
                return BadRequest("Cannot ban an admin user.");

            user.IsBanned = !user.IsBanned;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User {(user.IsBanned ? "banned" : "unbanned")} successfully.", isBanned = user.IsBanned });
        }

        // PATCH: api/admin/quizzes/{id}/unpublish
        [HttpPatch("quizzes/{id}/unpublish")]
        public async Task<IActionResult> ForceUnpublishQuiz(Guid id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz not found");

            quiz.Published = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quiz forced unpublished successfully" });
        }

        // DELETE: api/admin/quizzes/{id}
        [HttpDelete("quizzes/{id}")]
        public async Task<IActionResult> DeleteQuiz(Guid id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz not found");

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quiz deleted successfully" });
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalQuizzes = await _context.Quizzes.CountAsync();
            var totalPublishedQuizzes = await _context.Quizzes.CountAsync(q => q.Published);
            var totalAttempts = await _context.QuizAttempts.CountAsync();

            var topUsers = await _context.Users
                .Where(u => u.Role != "Admin")
                .Select(u => new
                {
                    u.Id,
                    u.DisplayName,
                    AttemptCount = u.QuizAttempts.Count,
                    AverageScore = u.QuizAttempts.Any() 
                        ? u.QuizAttempts.Average(a => a.MaxTotalScore > 0 ? (double)a.TotalScore / (double)a.MaxTotalScore * 100 : 0)
                        : 0
                })
                .OrderByDescending(u => u.AverageScore)
                .Take(5)
                .ToListAsync();

            var topQuizzes = await _context.Quizzes
                .Where(q => q.Published)
                .Select(q => new
                {
                    q.Id,
                    q.Title,
                    AttemptCount = q.Attempts.Count
                })
                .OrderByDescending(q => q.AttemptCount)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalQuizzes = totalQuizzes,
                TotalPublishedQuizzes = totalPublishedQuizzes,
                TotalAttempts = totalAttempts,
                TopUsers = topUsers,
                TopQuizzes = topQuizzes
            });
        }

        // GET: api/admin/users/{id}/history
        [HttpGet("users/{id}/history")]
        public async Task<IActionResult> GetUserHistory(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found");

            var history = await _context.QuizAttempts
                .Where(a => a.UserId == id)
                .Select(a => new
                {
                    a.Id,
                    a.QuizId,
                    QuizTitle = a.Quiz.Title,
                    Score = (int)a.TotalScore,
                    TotalQuestions = (int)a.MaxTotalScore,
                    CompletedAt = a.FinishedAt ?? a.StartedAt
                })
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            return Ok(history);
        }

        // GET: api/admin/documents
        [HttpGet("documents")]
        public async Task<IActionResult> GetSystemDocuments([FromQuery] PaginationParams pagination)
        {
            var query = _context.Documents
                .Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.UploadedAt,
                    d.Processed,
                    OwnerName = d.Owner.DisplayName,
                    Tokens = d.FileSize,
                    FileType = d.MimeType
                })
                .OrderByDescending(d => d.UploadedAt);

            var result = await PaginatedResult<object>.CreateAsync(query, pagination);
            return Ok(result);
        }

        // DELETE: api/admin/documents/{id}
        [HttpDelete("documents/{id}")]
        public async Task<IActionResult> AdminDeleteDocument(Guid id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound(new { message = "Document not found" });

            // (Optional) Xóa file trên Cloudinary ở đây nếu cần gọi service
            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document deleted successfully" });
        }
    }
}
