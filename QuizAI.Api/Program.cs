using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuizAI.Api.Data;
using QuizAI.Api.Services;

// Render injects ASPNETCORE_URLS or PORT — let ASP.NET Core handle it automatically
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
    options.LowercaseUrls = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<DocumentProcessorService>();
builder.Services.AddScoped<CloudinaryService>();

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
    throw new InvalidOperationException("Jwt:Secret is required. Set it in appsettings.json, appsettings.Local.json, or environment variable.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// Enable Swagger always (useful for API testing on Render)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed Default Admin Account (from environment variables)
var adminEmail = builder.Configuration["Admin:Email"];
var adminPassword = builder.Configuration["Admin:Password"];
if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        if (!context.Users.Any(u => u.Role == "Admin"))
        {
            var adminUser = new QuizAI.Api.Models.AppUser
            {
                Email = adminEmail.ToLower().Trim(),
                DisplayName = "Admin System",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = "Admin"
            };
            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}

app.Run();
