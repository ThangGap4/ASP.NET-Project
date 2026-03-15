using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;

using QuizAI.Api.Models;

using QuizAI.Api.Models;

namespace QuizAI.Api.Data;

using QuizAI.Api.Models;using QuizAI.Api.Models;

public class AppDbContext : DbContext

{namespace QuizAI.Api.Data;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }



    public DbSet<AppUser> Users { get; set; }

    public DbSet<Document> Documents { get; set; }public class AppDbContext : DbContext

    public DbSet<DocumentChunk> DocumentChunks { get; set; }

    public DbSet<Quiz> Quizzes { get; set; }{namespace QuizAI.Api.Data;namespace QuizAI.Api.Data;

    public DbSet<Question> Questions { get; set; }

    public DbSet<QuestionOption> QuestionOptions { get; set; }    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<QuizAttempt> QuizAttempts { get; set; }

    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)    public DbSet<AppUser> Users { get; set; }

    {

        base.OnModelCreating(modelBuilder);    public DbSet<Document> Documents { get; set; }public class AppDbContext : DbContextpublic class AppDbContext : DbContext



        modelBuilder.Entity<AppUser>()    public DbSet<DocumentChunk> DocumentChunks { get; set; }

            .HasMany(u => u.Quizzes)

            .WithOne(q => q.User)    public DbSet<Quiz> Quizzes { get; set; }{{

            .HasForeignKey(q => q.UserId)

            .OnDelete(DeleteBehavior.Cascade);    public DbSet<Question> Questions { get; set; }



        modelBuilder.Entity<Quiz>()    public DbSet<QuestionOption> QuestionOptions { get; set; }    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }    public DbSet<AppUser> Users { get; set; }

            .HasMany(q => q.Questions)

            .WithOne(qu => qu.Quiz)    public DbSet<QuizAttempt> QuizAttempts { get; set; }

            .HasForeignKey(qu => qu.QuizId)

            .OnDelete(DeleteBehavior.Cascade);    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }    public DbSet<Document> Documents { get; set; }



        modelBuilder.Entity<Question>()

            .HasMany(q => q.Options)

            .WithOne(qo => qo.Question)    protected override void OnModelCreating(ModelBuilder modelBuilder)    public DbSet<AppUser> Users { get; set; }    public DbSet<DocumentChunk> DocumentChunks { get; set; }

            .HasForeignKey(qo => qo.QuestionId)

            .OnDelete(DeleteBehavior.Cascade);    {



        modelBuilder.Entity<Document>()        base.OnModelCreating(modelBuilder);    public DbSet<Document> Documents { get; set; }    public DbSet<Quiz> Quizzes { get; set; }

            .HasMany(d => d.Chunks)

            .WithOne(dc => dc.Document)

            .HasForeignKey(dc => dc.DocumentId)

            .OnDelete(DeleteBehavior.Cascade);        modelBuilder.Entity<AppUser>()    public DbSet<DocumentChunk> DocumentChunks { get; set; }    public DbSet<Question> Questions { get; set; }

    }

}            .HasMany(u => u.Quizzes)


            .WithOne(q => q.User)    public DbSet<Quiz> Quizzes { get; set; }    public DbSet<QuestionOption> QuestionOptions { get; set; }

            .HasForeignKey(q => q.UserId)

            .OnDelete(DeleteBehavior.Cascade);    public DbSet<Question> Questions { get; set; }    public DbSet<QuizAttempt> QuizAttempts { get; set; }



        modelBuilder.Entity<Quiz>()    public DbSet<QuestionOption> QuestionOptions { get; set; }    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }

            .HasMany(q => q.Questions)

            .WithOne(qu => qu.Quiz)    public DbSet<QuizAttempt> QuizAttempts { get; set; }

            .HasForeignKey(qu => qu.QuizId)

            .OnDelete(DeleteBehavior.Cascade);    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }



        modelBuilder.Entity<Question>()

            .HasMany(q => q.Options)

            .WithOne(qo => qo.Question)    protected override void OnModelCreating(ModelBuilder modelBuilder)    protected override void OnModelCreating(ModelBuilder modelBuilder)

            .HasForeignKey(qo => qo.QuestionId)

            .OnDelete(DeleteBehavior.Cascade);    {    {



        modelBuilder.Entity<Document>()        base.OnModelCreating(modelBuilder);        base.OnModelCreating(modelBuilder);

            .HasMany(d => d.Chunks)

            .WithOne(dc => dc.Document)

            .HasForeignKey(dc => dc.DocumentId)

            .OnDelete(DeleteBehavior.Cascade);        modelBuilder.Entity<AppUser>()        // AppUser

    }

}            .HasMany(u => u.Quizzes)        modelBuilder.Entity<AppUser>()


            .WithOne(q => q.User)            .HasIndex(u => u.Email)

            .HasForeignKey(q => q.UserId)            .IsUnique();

            .OnDelete(DeleteBehavior.Cascade);

        // Document

        modelBuilder.Entity<Quiz>()        modelBuilder.Entity<Document>()

            .HasMany(q => q.Questions)            .HasOne(d => d.Owner)

            .WithOne(qu => qu.Quiz)            .WithMany(u => u.Documents)

            .HasForeignKey(qu => qu.QuizId)            .HasForeignKey(d => d.OwnerId)

            .OnDelete(DeleteBehavior.Cascade);            .OnDelete(DeleteBehavior.Cascade);



        modelBuilder.Entity<Question>()        // DocumentChunk

            .HasMany(q => q.Options)        modelBuilder.Entity<DocumentChunk>()

            .WithOne(qo => qo.Question)            .HasOne(dc => dc.Document)

            .HasForeignKey(qo => qo.QuestionId)            .WithMany(d => d.Chunks)

            .OnDelete(DeleteBehavior.Cascade);            .HasForeignKey(dc => dc.DocumentId)

            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()

            .HasMany(d => d.Chunks)        modelBuilder.Entity<DocumentChunk>()

            .WithOne(dc => dc.Document)            .HasIndex(dc => new { dc.DocumentId, dc.ChunkIndex })

            .HasForeignKey(dc => dc.DocumentId)            .IsUnique();

            .OnDelete(DeleteBehavior.Cascade);

    }        // Quiz

}        modelBuilder.Entity<Quiz>()

            .HasOne(q => q.Creator)
            .WithMany(u => u.CreatedQuizzes)
            .HasForeignKey(q => q.CreatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Quiz>()
            .HasOne(q => q.SourceDocument)
            .WithMany(d => d.GeneratedQuizzes)
            .HasForeignKey(q => q.SourceDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Question
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Quiz)
            .WithMany(qz => qz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Question>()
            .HasIndex(q => new { q.QuizId, q.Seq })
            .IsUnique();

        // QuestionOption
        modelBuilder.Entity<QuestionOption>()
            .HasOne(qo => qo.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(qo => qo.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuestionOption>()
            .HasIndex(qo => new { qo.QuestionId, qo.OptIndex })
            .IsUnique();

        // QuizAttempt
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

        // AttemptAnswer
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
