namespace backend.Dtos;

public class CreateConversationDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class SendMessageDto
{
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class ConversationListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MessageDto
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
}

public class SendMessageResponseDto
{
    public MessageDto UserMessage { get; set; } = null!;
    public MessageDto AssistantMessage { get; set; } = null!;
}