using backend.Dtos;
using backend.Services;
using System.ComponentModel;

namespace backend.Tools;

public class SaveBotVersionTool(
    BotService botService)
{
    [Description("Save the generated cTrader cBot source code as a new bot version. Use this only when the user explicitly asks to save the generated source code.")]
    public async Task<BotVersionDto> SaveBotVersion(
        [Description("User ID that owns the bot creation session.")]
        int userId,
        [Description("Bot creation session ID associated with the generated cBot.")]
        int sessionId,
        [Description("Name of the cBot.")]
        string botName,
        [Description("Complete generated cTrader C# source code.")]
        string sourceCode)
    {
        var session = await botService.GetCreationSessionAsync(
            userId,
            sessionId);

        if (session is null)
            throw new InvalidOperationException(
                "Bot creation session not found.");

        var result = await botService.SaveCreationSessionVersionAsync(
            userId,
            session,
            botName,
            sourceCode);

        if (!result.Success || result.Response is null)
            throw new InvalidOperationException(
                result.Error ?? "Failed to save bot version.");

        return result.Response;
    }
}