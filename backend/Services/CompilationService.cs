using backend.Dtos;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace backend.Services;

public class CompilationService
{
    public async Task<CompilationResultDto> CompileAsync(
        int userId,
        string sourceCode,
        string botName,
        int version)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID is required.");

        if (string.IsNullOrWhiteSpace(sourceCode))
            throw new ArgumentException("Source code is required.");

        if (string.IsNullOrWhiteSpace(botName))
            throw new ArgumentException("Bot name is required.");

        if (version <= 0)
            throw new ArgumentException("Version must be greater than zero.");

        var safeBotName = SanitizeBotName(botName);
        var artifactId = Guid.NewGuid().ToString("N");

        var workspaceRoot = Path.Combine(
            Path.GetTempPath(),
            "cbot-compiler",
            artifactId);

        var botDirectory = Path.Combine(
            workspaceRoot,
            safeBotName);

        var sourceDirectory = Path.Combine(
            botDirectory,
            "src");

        var artifactRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "compiled-bots");

        var userArtifactDirectory = Path.Combine(
            artifactRoot,
            userId.ToString());

        var generatedArtifactName = $"{safeBotName}.algo";

        var artifactName = $"{safeBotName}_{version}.algo";

        var generatedArtifactPath = Path.Combine(
            userArtifactDirectory,
            generatedArtifactName);

        var artifactPath = Path.Combine(
            userArtifactDirectory,
            artifactName);

        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(userArtifactDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(sourceDirectory, "Program.cs"),
                sourceCode);

            await File.WriteAllTextAsync(
                Path.Combine(sourceDirectory, "src.csproj"),
                CreateProjectFile());

            var result = await RunDockerAsync(
                workspaceRoot,
                userArtifactDirectory,
                safeBotName);

            var compilationResult = ParseOutput(
                result.Output,
                userId,
                safeBotName,
                version);

            if (!compilationResult.Success)
                return compilationResult;

            if (!File.Exists(generatedArtifactPath))
            {
                compilationResult.Success = false;

                compilationResult.Diagnostics.Add(
                    new CompilationDiagnosticDto
                    {
                        Code = "ARTIFACT",
                        Severity = "Error",
                        Message = $"Compilation succeeded but the generated .algo artifact '{generatedArtifactName}' was not found."
                    });

                return compilationResult;
            }

            if (File.Exists(artifactPath))
                File.Delete(artifactPath);

            File.Move(
                generatedArtifactPath,
                artifactPath);

            compilationResult.ArtifactId = artifactId;
            compilationResult.ArtifactName = artifactName;
            compilationResult.ArtifactPath = artifactPath;

            return compilationResult;
        }
        finally
        {
            TryDeleteDirectory(workspaceRoot);
        }
    }

    private static async Task<DockerResult> RunDockerAsync(
        string workspaceRoot,
        string artifactDirectory,
        string botName)
    {
        var dockerWorkspace = ToDockerPath(workspaceRoot);
        var dockerArtifactDirectory = ToDockerPath(artifactDirectory);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add("run");
        process.StartInfo.ArgumentList.Add("--rm");

        process.StartInfo.ArgumentList.Add("-e");
        process.StartInfo.ArgumentList.Add("DOTNET_CLI_HOME=/tmp");

        process.StartInfo.ArgumentList.Add("-v");
        process.StartInfo.ArgumentList.Add(
            $"{dockerWorkspace}:/workspace");

        process.StartInfo.ArgumentList.Add("-v");
        process.StartInfo.ArgumentList.Add(
            $"{dockerArtifactDirectory}:/root/cAlgo/Sources/Robots");

        process.StartInfo.ArgumentList.Add("-w");
        process.StartInfo.ArgumentList.Add(
            $"/workspace/{botName}/src");

        process.StartInfo.ArgumentList.Add(
            "cbot-compiler");

        if (!process.Start())
            throw new InvalidOperationException(
                "Failed to start Docker.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return new DockerResult
        {
            ExitCode = process.ExitCode,
            Output = output + Environment.NewLine + error
        };
    }

    private static CompilationResultDto ParseOutput(
        string output,
        int userId,
        string botName,
        int version)
    {
        var result = new CompilationResultDto
        {
            UserId = userId,
            BotName = botName,
            Version = version
        };

        result.Success = output.Contains(
            "Build succeeded.",
            StringComparison.OrdinalIgnoreCase);

        var diagnosticPattern =
            @"(?<code>[A-Z]{2}\d{4}):\s*(?<message>.*)";

        foreach (Match match in Regex.Matches(
                     output,
                     diagnosticPattern))
        {
            result.Diagnostics.Add(
                new CompilationDiagnosticDto
                {
                    Code = match.Groups["code"].Value,
                    Severity = "Error",
                    Message = match.Groups["message"].Value.Trim()
                });
        }

        if (!result.Success &&
            result.Diagnostics.Count == 0)
        {
            result.Diagnostics.Add(
                new CompilationDiagnosticDto
                {
                    Code = "COMPILATION",
                    Severity = "Error",
                    Message = ExtractFailureMessage(output)
                });
        }

        return result;
    }

    private static string ExtractFailureMessage(string output)
    {
        var lines = output
            .Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line =>
                !line.StartsWith("Determining projects to restore") &&
                !line.StartsWith("All projects are up-to-date") &&
                !line.StartsWith("Build FAILED") &&
                !line.StartsWith("Build succeeded") &&
                !line.StartsWith("Time Elapsed") &&
                !line.StartsWith("Warning(s)") &&
                !line.StartsWith("Error(s)"))
            .ToList();

        if (lines.Count == 0)
            return "Compilation failed.";

        return string.Join(Environment.NewLine, lines);
    }

    private static string CreateProjectFile()
    {
        return """
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFramework>net6.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="cTrader.Automate" Version="1.0.21" />
          </ItemGroup>

        </Project>
        """;
    }

    private static string SanitizeBotName(string botName)
    {
        var sanitized = Regex.Replace(
            botName.Trim(),
            @"[^a-zA-Z0-9_]",
            "");

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException(
                "Bot name must contain letters, numbers, or underscores.");

        if (char.IsDigit(sanitized[0]))
            sanitized = $"Bot{sanitized}";

        return sanitized;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
        }
    }

    private static string ToDockerPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private class DockerResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
    }
}