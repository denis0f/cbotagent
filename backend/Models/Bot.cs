namespace backend.Models;

public class Bot
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CreatedFromConversationId { get; set; }
    public int? CurrentVersionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Conversation? CreatedFromConversation { get; set; }
    public BotVersion? CurrentVersion { get; set; }
    public ICollection<BotVersion> Versions { get; set; } = new List<BotVersion>();
}