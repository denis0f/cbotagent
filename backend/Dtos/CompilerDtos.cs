namespace backend.Dtos;

public class CompileSourceDto
{
    public int UserId { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string BotName { get; set; } = string.Empty;
    public int Version { get; set; }
}

public class CompilerAnalysisDto
{
    public string Analysis { get; set; } = string.Empty;
}