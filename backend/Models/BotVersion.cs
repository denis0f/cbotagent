namespace backend.Models;

public class BotVersion
{
    public int Id { get; set; }
    public int BotId { get; set; }
    public int VersionNumber { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
    public string? CompiledFilePath { get; set; }
    public string CompilationStatus { get; set; } = string.Empty;
    public int? CreatedByAgentRunId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Bot Bot { get; set; } = null!;
    public AgentRun? CreatedByAgentRun { get; set; }
}