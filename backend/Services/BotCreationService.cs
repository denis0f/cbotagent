using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using backend.Tools;

namespace backend.Services;

public class BotCreationService(
    AppDbContext context,
    PromptRefinerAgentService promptRefinerAgentService,
    CoderAgentService coderAgentService,
    AgentRunService agentRunService,
    BotService botService,
    CompileBotVersionTool compileBotVersionTool)
{
    public async Task<(bool Success, string? Error, BotCreationSessionDto? Response)>
        StartAsync(
            int conversationId,
            StartBotCreationDto dto)
    {
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c =>
                c.Id == conversationId &&
                c.UserId == dto.UserId);

        if (conversation is null)
            return (false, "Conversation not found.", null);

        var existingSession = await context.BotCreationSessions
            .FirstOrDefaultAsync(s =>
                s.ConversationId == conversationId &&
                s.UserId == dto.UserId &&
                s.Status != "Completed" &&
                s.Status != "Failed");

        if (existingSession is not null)
            return (
                true,
                null,
                Map(existingSession)
            );

        var session = new BotCreationSession
        {
            UserId = dto.UserId,
            ConversationId = conversationId,
            Status = "PromptRefining"
        };

        context.BotCreationSessions.Add(session);
        await context.SaveChangesAsync();

        var runResult = await agentRunService.CreateRunAsync(
            conversationId,
            new CreateAgentRunDto
            {
                UserId = dto.UserId,
                AgentType = "PromptRefiner"
            });

        if (!runResult.Success || runResult.Response is null)
        {
            session.Status = "Failed";
            session.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return (
                false,
                runResult.Error ?? "Failed to create agent run.",
                null
            );
        }

        try
        {
            var messages = await context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.Sequence)
                .Select(m => new ChatMessage(
                    m.Role == "assistant"
                        ? ChatRole.Assistant
                        : ChatRole.User,
                    m.Content))
                .ToListAsync();

            messages.Add(
                new ChatMessage(
                    ChatRole.User,
                    """
                    Analyze the complete conversation above and produce the precise cBot technical specification now.
                    """
                )
            );

            var refinedPrompt = await promptRefinerAgentService
                .RefineAsync(messages);

            if (string.IsNullOrWhiteSpace(refinedPrompt))
                throw new InvalidOperationException(
                    "Prompt refiner returned an empty specification.");

            session.RefinedPrompt = refinedPrompt;
            session.Status = "AwaitingPromptApproval";
            session.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await agentRunService.CompleteRunAsync(
                dto.UserId,
                runResult.Response.Id);

            return (
                true,
                null,
                Map(session)
            );
        }
        catch (Exception ex)
        {
            session.Status = "Failed";
            session.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await agentRunService.FailRunAsync(
                dto.UserId,
                runResult.Response.Id,
                ex.Message);

            return (
                false,
                "Failed to refine the cBot specification.",
                null
            );
        }
    }

    public async Task<BotCreationSessionDto?> GetAsync(
        int userId,
        int sessionId)
    {
        var session = await context.BotCreationSessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);

        return session is null
            ? null
            : Map(session);
    }

    public async Task<BotCreationSessionDto?> GetConversationSessionAsync(
        int userId,
        int conversationId)
    {
        var session = await context.BotCreationSessions
            .Where(s =>
                s.UserId == userId &&
                s.ConversationId == conversationId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        return session is null
            ? null
            : Map(session);
    }

    public async Task<(bool Success, string? Error)> ApproveAsync(
        int userId,
        int sessionId)
    {
        var session = await context.BotCreationSessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);

        if (session is null)
            return (false, "Bot creation session not found.");

        if (session.Status != "AwaitingPromptApproval")
            return (
                false,
                "Bot creation session is not awaiting prompt approval."
            );

        if (string.IsNullOrWhiteSpace(session.RefinedPrompt))
            return (false, "There is no refined prompt to approve.");

        session.Status = "Coding";
        session.ApprovedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectAsync(
        int userId,
        int sessionId)
    {
        var session = await context.BotCreationSessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);

        if (session is null)
            return (false, "Bot creation session not found.");

        context.BotCreationSessions.Remove(session);
        await context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error, BotCreationSessionDto? Response)>
        RefineAsync(
            int userId,
            int sessionId,
            RefineBotCreationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Changes))
            return (false, "Changes are required.", null);

        var session = await context.BotCreationSessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);

        if (session is null)
            return (false, "Bot creation session not found.", null);

        if (session.Status != "AwaitingPromptApproval")
            return (
                false,
                "Bot creation session is not awaiting prompt approval.",
                null
            );

        if (string.IsNullOrWhiteSpace(session.RefinedPrompt))
            return (false, "There is no refined prompt to update.", null);

        var runResult = await agentRunService.CreateRunAsync(
            session.ConversationId,
            new CreateAgentRunDto
            {
                UserId = userId,
                AgentType = "PromptRefiner"
            });

        if (!runResult.Success || runResult.Response is null)
            return (
                false,
                runResult.Error ?? "Failed to create agent run.",
                null
            );

        try
        {
            var messages = await context.Messages
                .Where(m => m.ConversationId == session.ConversationId)
                .OrderBy(m => m.Sequence)
                .Select(m => new ChatMessage(
                    m.Role == "assistant"
                        ? ChatRole.Assistant
                        : ChatRole.User,
                    m.Content))
                .ToListAsync();

            messages.Add(
                new ChatMessage(
                    ChatRole.User,
                    $"""
                    The current refined cBot specification is:

                    {session.RefinedPrompt}

                    The user has requested these changes:

                    {dto.Changes}

                    Analyze the original conversation, the current specification, and the requested changes.

                    Produce a new complete cBot technical specification.

                    Keep all confirmed requirements that remain valid.
                    Apply the requested changes.
                    Identify any genuinely unresolved requirements.
                    Do not generate C# code.
                    """
                )
            );

            var refinedPrompt = await promptRefinerAgentService
                .RefineAsync(messages);

            if (string.IsNullOrWhiteSpace(refinedPrompt))
                throw new InvalidOperationException(
                    "Prompt refiner returned an empty specification.");

            session.RefinedPrompt = refinedPrompt;
            session.Status = "AwaitingPromptApproval";
            session.ApprovedAt = null;
            session.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await agentRunService.CompleteRunAsync(
                userId,
                runResult.Response.Id);

            return (
                true,
                null,
                Map(session)
            );
        }
        catch (Exception ex)
        {
            await agentRunService.FailRunAsync(
                userId,
                runResult.Response.Id,
                ex.Message);

            return (
                false,
                "Failed to refine the cBot specification.",
                null
            );
        }
    }

    public async Task<(bool Success, string? Error, BotCreationSessionDto? Response)>
        GenerateSourceAsync(
            int userId,
            int sessionId)
    {
        var session = await context.BotCreationSessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);

        if (session is null)
            return (false, "Bot creation session not found.", null);

        if (session.Status != "Coding")
            return (
                false,
                "Bot creation session is not ready for coding.",
                null
            );

        if (string.IsNullOrWhiteSpace(session.RefinedPrompt))
            return (false, "There is no refined prompt for the coder.", null);

        var runResult = await agentRunService.CreateRunAsync(
            session.ConversationId,
            new CreateAgentRunDto
            {
                UserId = userId,
                AgentType = "Coder"
            });

        if (!runResult.Success || runResult.Response is null)
            return (
                false,
                runResult.Error ?? "Failed to create coder agent run.",
                null
            );

        try
        {
            var sourceCode = await coderAgentService
                .GenerateCodeAsync(session.RefinedPrompt);

            if (string.IsNullOrWhiteSpace(sourceCode))
                throw new InvalidOperationException(
                    "Coder agent returned empty source code.");

            session.GeneratedSourceCode = sourceCode;
            session.Status = "AwaitingSourceApproval";
            session.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await agentRunService.CompleteRunAsync(
                userId,
                runResult.Response.Id);

            return (
                true,
                null,
                Map(session)
            );
        }
        catch (Exception ex)
        {
            session.Status = "Failed";
            session.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await agentRunService.FailRunAsync(
                userId,
                runResult.Response.Id,
                ex.Message);

            return (
                false,
                "Failed to generate cBot source code.",
                null
            );
        }
    }

    public async Task<(bool Success, string? Error, BotVersionDto? Response)>
    SaveSourceAsync(
        int userId,
        int sessionId,
        string botName)
{
    var session = await context.BotCreationSessions
        .FirstOrDefaultAsync(s =>
            s.Id == sessionId &&
            s.UserId == userId);

    if (session is null)
        return (false, "Bot creation session not found.", null);

    if (session.Status != "AwaitingSourceApproval")
        return (
            false,
            "Bot creation session is not awaiting source approval.",
            null
        );

    if (string.IsNullOrWhiteSpace(session.GeneratedSourceCode))
        return (false, "There is no generated source code to save.", null);

    var result = await botService.SaveCreationSessionVersionAsync(
        userId,
        session,
        botName,
        session.GeneratedSourceCode);

    if (!result.Success)
        return (false, result.Error, null);

    return (true, null, result.Response);
}

public async Task<(bool Success, string? Error, CompilationResultDto? Response)>
    CompileSourceAsync(
        int userId,
        int sessionId)
{
    var session = await context.BotCreationSessions
        .FirstOrDefaultAsync(s =>
            s.Id == sessionId &&
            s.UserId == userId);

    if (session is null)
        return (false, "Bot creation session not found.", null);

    if (session.Status != "Saved")
        return (
            false,
            "The bot must be saved before it can be compiled.",
            null
        );

    if (!session.BotId.HasValue)
        return (
            false,
            "The bot creation session is not linked to a bot.",
            null
        );

    var bot = await context.Bots
        .FirstOrDefaultAsync(b =>
            b.Id == session.BotId.Value &&
            b.UserId == userId);

    if (bot is null)
        return (false, "Bot not found.", null);

    var version = await context.BotVersions
        .Where(v => v.BotId == bot.Id)
        .OrderByDescending(v => v.VersionNumber)
        .FirstOrDefaultAsync();

    if (version is null)
        return (false, "No saved bot version was found.", null);

    var result = await compileBotVersionTool.CompileBotVersion(
        userId,
        bot.Id,
        version.VersionNumber);

    if (result.Success)
    {
        session.Status = "Completed";
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }
    else
    {
        session.Status = "Failed";
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    return (
        true,
        null,
        result
    );
}

    private static BotCreationSessionDto Map(
        BotCreationSession session)
    {
        return new BotCreationSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            ConversationId = session.ConversationId,
            BotId = session.BotId,
            Status = session.Status,
            RefinedPrompt = session.RefinedPrompt,
            GeneratedSourceCode = session.GeneratedSourceCode,
            ApprovedAt = session.ApprovedAt,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }
}