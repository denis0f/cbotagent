namespace backend.Models;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}