namespace backend.Models;

public class BotCreationSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ConversationId { get; set; }
    public int? BotId { get; set; }
    public string Status { get; set; } = "AwaitingPromptApproval";
    public string? RefinedPrompt { get; set; }
    public string? GeneratedSourceCode { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Conversation Conversation { get; set; } = null!;
    public Bot? Bot { get; set; }
}