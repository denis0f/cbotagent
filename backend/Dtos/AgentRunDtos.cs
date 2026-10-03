namespace backend.Dtos;

public class AgentRunDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string AgentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}

public class CreateAgentRunDto
{
    public int UserId { get; set; }
    public string AgentType { get; set; } = string.Empty;
}