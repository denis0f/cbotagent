using backend.Dtos;
using backend.Services;
using System.ComponentModel;

namespace backend.Tools;

public class CompileSourceTool(
    CompilationService compilationService,
    BotService botService)
{
    [Description("Compile generated cTrader cBot source code using the cTrader Automate .NET 6 compiler and generate a versioned .algo file.")]
    public async Task<CompilationResultDto> CompileSource(
        [Description("User ID that owns the bot.")]
        int userId,
        [Description("Bot ID of the saved cBot.")]
        int botId,
        [Description("BotVersion database ID.")]
        int botVersionId,
        [Description("Complete C# source code of the cBot.")]
        string sourceCode,
        [Description("Name of the cBot.")]
        string botName,
        [Description("Version number of the cBot.")]
        int version)
    {
        var result = await compilationService.CompileAsync(
            userId,
            sourceCode,
            botName,
            version);

        if (result.Success)
        {
            await botService.UpdateCompilationResultAsync(
                userId,
                botId,
                version,
                "Compiled",
                result.ArtifactPath);
        }
        else
        {
            await botService.UpdateCompilationResultAsync(
                userId,
                botId,
                version,
                "Failed",
                null);
        }

        return result;
    }
}