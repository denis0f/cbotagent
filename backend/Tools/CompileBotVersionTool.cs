using backend.Dtos;
using backend.Services;
using System.ComponentModel;

namespace backend.Tools;

public class CompileBotVersionTool(
    BotService botService,
    CompilerAgentService compilerAgentService)
{
    [Description("Compile a previously saved cTrader cBot version. Use this only when the user explicitly asks to compile the saved bot version.")]
    public async Task<CompilationResultDto> CompileBotVersion(
        [Description("User ID that owns the bot.")]
        int userId,
        [Description("Bot ID of the saved cBot.")]
        int botId,
        [Description("Version number to compile.")]
        int versionNumber)
    {
        var version = await botService.GetVersionAsync(
            userId,
            botId,
            versionNumber);

        if (version is null)
            throw new InvalidOperationException(
                "Bot version not found.");

        await botService.UpdateCompilationResultAsync(
            userId,
            botId,
            versionNumber,
            "Compiling",
            null);

        try
        {
            var result = await compilerAgentService.AnalyzeAsync(
                userId,
                botId,
                version.Id,
                version.SourceCode,
                version.Bot.Name,
                versionNumber);

            return result;
        }
        catch
        {
            await botService.UpdateCompilationResultAsync(
                userId,
                botId,
                versionNumber,
                "Failed",
                null);

            throw;
        }
    }
}