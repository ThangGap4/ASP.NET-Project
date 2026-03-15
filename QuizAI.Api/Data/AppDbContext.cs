using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Models;

namespace QuizAI.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<QuizAttempt> QuizAttempts { get; set; }
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Owner)
            .WithMany(u => u.Documents)
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentChunk>()
            .HasOne(dc => dc.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(dc => dc.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(dc => new { dc.DocumentId, dc.ChunkIndex })
            .IsUnique();

        modelBuilder.Entity<Quiz>()
            .HasOne(q => q.Creator)
            .WithMany(u => u.CreatedQuizzes)
            .HasForeignKey(q => q.CreatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Quiz>()
            .HasOne(q => q.SourceDocument)
            .WithMany(d => d.GeneratedQuizzes)
            .HasForeignKey(q => q.SourceDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Question>()
            .HasOne(q => q.Quiz)
            .WithMany(qz => qz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Question>()
            .HasIndex(q => new { q.QuizId, q.Seq })
            .IsUnique();

        modelBuilder.Entity<QuestionOption>()
            .HasOne(qo => qo.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(qo => qo.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuestionOption>()
            .HasIndex(qo => new { qo.QuestionId, qo.OptIndex })
            .IsUnique();

        modelBuilder.Entity<QuizAttempt>()
            .HasOne(qa => qa.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(qa => qa.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuizAttempt>()
            .HasOne(qa => qa.User)
            .WithMany(u => u.QuizAttempts)
            .HasForeignKey(qa => qa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttemptAnswer>()
            .HasOne(aa => aa.Attempt)
            .WithMany(qa => qa.Answers)
            .HasForeignKey(aa => aa.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttemptAnswer>()
            .HasOne(aa => aa.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(aa => aa.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttemptAnswer>()
            .HasOne(aa => aa.SelectedOption)
            .WithMany(qo => qo.Answers)
            .HasForeignKey(aa => aa.SelectedOptionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AttemptAnswer>()
            .HasIndex(aa => new { aa.AttemptId, aa.QuestionId })
            .IsUnique();
    }
}
