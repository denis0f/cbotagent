namespace backend.Models;

public class Conversation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<AgentRun> AgentRuns { get; set; } = new List<AgentRun>();
    public ICollection<Bot> Bots { get; set; } = new List<Bot>();
}