namespace backend.Dtos;

public class CreateBotDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CreatedFromConversationId { get; set; }
}

public class CreateBotVersionDto
{
    public int UserId { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string CompilationStatus { get; set; } = "NotCompiled";
}

public class BotListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CurrentVersionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BotVersionDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string CompilationStatus { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
    public string? CompiledFilePath { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BotDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CurrentVersionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<BotVersionDto> Versions { get; set; } = new();
}

public class BotSourceDto
{
    public int BotId { get; set; }
    public int VersionNumber { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string CompilationStatus { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
    public string? CompiledFilePath { get; set; }
}