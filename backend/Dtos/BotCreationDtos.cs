namespace backend.Dtos;

public class StartBotCreationDto
{
    public int UserId { get; set; }
}

public class BotCreationSessionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ConversationId { get; set; }
    public int? BotId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RefinedPrompt { get; set; }
    public string? GeneratedSourceCode { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RefineBotCreationDto
{
    public int UserId { get; set; }
    public string Changes { get; set; } = string.Empty;
}