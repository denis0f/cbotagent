using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<BotVersion> BotVersions => Set<BotVersion>();
    public DbSet<BotCreationSession> BotCreationSessions => Set<BotCreationSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.AgentRuns)
                .WithOne(a => a.Conversation)
                .HasForeignKey(a => a.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Bots)
                .WithOne(b => b.CreatedFromConversation)
                .HasForeignKey(b => b.CreatedFromConversationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Role)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(m => m.Content)
                .IsRequired();

            entity.HasIndex(m => new
            {
                m.ConversationId,
                m.Sequence
            });
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.AgentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(a => a.ConversationId);
        });

        modelBuilder.Entity<Bot>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(b => b.Description)
                .HasMaxLength(1000);

            entity.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.CurrentVersion)
                .WithMany()
                .HasForeignKey(b => b.CurrentVersionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(b => b.UserId);
        });

        modelBuilder.Entity<BotVersion>(entity =>
{
    entity.HasKey(v => v.Id);

    entity.Property(v => v.SourceCode)
        .IsRequired();

    entity.Property(v => v.SourceFilePath)
        .IsRequired()
        .HasMaxLength(500);

    entity.Property(v => v.CompiledFilePath)
        .HasMaxLength(500);

    entity.Property(v => v.CompilationStatus)
        .IsRequired()
        .HasMaxLength(50);

    entity.HasOne(v => v.Bot)
        .WithMany(b => b.Versions)
        .HasForeignKey(v => v.BotId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(v => v.CreatedByAgentRun)
        .WithMany()
        .HasForeignKey(v => v.CreatedByAgentRunId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasIndex(v => new
    {
        v.BotId,
        v.VersionNumber
    })
    .IsUnique();
});

        modelBuilder.Entity<BotCreationSession>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.RefinedPrompt);

            entity.Property(s => s.GeneratedSourceCode);

            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Conversation)
                .WithMany()
                .HasForeignKey(s => s.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Bot)
                .WithMany()
                .HasForeignKey(s => s.BotId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.ConversationId);
        });
    }
}