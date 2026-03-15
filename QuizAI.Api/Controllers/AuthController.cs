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
}

public record RegisterDto(string Email, string Password, string DisplayName);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(Guid Id, string Email, string DisplayName, string Role, string Token);
