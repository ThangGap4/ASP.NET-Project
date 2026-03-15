using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;
using QuizAI.Api.Models;
using QuizAI.Api.Services;

namespace QuizAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { message = "Email already exists" });

        var user = new AppUser
        {
            Email = dto.Email.ToLower().Trim(),
            DisplayName = dto.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "student"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return CreatedAtAction(null, null, new AuthResponseDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            token
        ));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower().Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponseDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            token
        ));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idStr, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var totalAttempts = await _context.QuizAttempts
            .CountAsync(a => a.UserId == userId);

        var gradedAttempts = await _context.QuizAttempts
            .Where(a => a.UserId == userId && a.Status == "graded" && a.MaxTotalScore > 0)
            .ToListAsync();

        double avgScore = gradedAttempts.Count == 0 ? 0 :
            gradedAttempts.Average(a => a.MaxTotalScore > 0
                ? (double)a.TotalScore / (double)a.MaxTotalScore * 100 : 0);

        var totalDocs = await _context.Documents.CountAsync(d => d.OwnerId == userId);
        var totalQuizzes = await _context.Quizzes.CountAsync(q => q.CreatorId == userId);

        return Ok(new UserProfileDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.LastLogin,
            totalDocs,
            totalQuizzes,
            totalAttempts,
            Math.Round(avgScore, 1)
        ));
    }
}

public record RegisterDto(string Email, string Password, string DisplayName);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(Guid Id, string Email, string DisplayName, string Role, string Token);
public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime? LastLogin,
    int TotalDocuments,
    int TotalQuizzes,
    int TotalAttempts,
    double AverageScorePercent
);
