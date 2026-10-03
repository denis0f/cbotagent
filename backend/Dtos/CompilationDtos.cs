namespace backend.Dtos;

public class CompilationDiagnosticDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}

public class CompilationResultDto
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string BotName { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? ArtifactId { get; set; }
    public string? ArtifactName { get; set; }
    public string? ArtifactPath { get; set; }
    public List<CompilationDiagnosticDto> Diagnostics { get; set; } = new();
}