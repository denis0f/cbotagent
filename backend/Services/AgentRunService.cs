using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AgentRunService(AppDbContext context)
{
    public async Task<(bool Success, string? Error, AgentRunDto? Response)> CreateRunAsync(
        int conversationId,
        CreateAgentRunDto dto)
    {
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c =>
                c.Id == conversationId &&
                c.UserId == dto.UserId);

        if (conversation is null)
            return (false, "Conversation not found.", null);

        if (string.IsNullOrWhiteSpace(dto.AgentType))
            return (false, "Agent type is required.", null);

        var run = new AgentRun
        {
            ConversationId = conversationId,
            AgentType = dto.AgentType.Trim(),
            Status = "Running",
            StartedAt = DateTime.UtcNow
        };

        context.AgentRuns.Add(run);
        await context.SaveChangesAsync();

        return (
            true,
            null,
            Map(run)
        );
    }

    public async Task<AgentRunDto?> GetRunAsync(
        int userId,
        int runId)
    {
        return await context.AgentRuns
            .Where(r =>
                r.Id == runId &&
                r.Conversation.UserId == userId)
            .Select(r => new AgentRunDto
            {
                Id = r.Id,
                ConversationId = r.ConversationId,
                AgentType = r.AgentType,
                Status = r.Status,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                Error = r.Error
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<AgentRunDto>> GetConversationRunsAsync(
        int userId,
        int conversationId)
    {
        return await context.AgentRuns
            .Where(r =>
                r.ConversationId == conversationId &&
                r.Conversation.UserId == userId)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => new AgentRunDto
            {
                Id = r.Id,
                ConversationId = r.ConversationId,
                AgentType = r.AgentType,
                Status = r.Status,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                Error = r.Error
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CompleteRunAsync(
        int userId,
        int runId)
    {
        var run = await context.AgentRuns
            .FirstOrDefaultAsync(r =>
                r.Id == runId &&
                r.Conversation.UserId == userId);

        if (run is null)
            return (false, "Agent run not found.");

        run.Status = "Completed";
        run.CompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> FailRunAsync(
        int userId,
        int runId,
        string error)
    {
        var run = await context.AgentRuns
            .FirstOrDefaultAsync(r =>
                r.Id == runId &&
                r.Conversation.UserId == userId);

        if (run is null)
            return (false, "Agent run not found.");

        run.Status = "Failed";
        run.Error = error;
        run.CompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return (true, null);
    }

    private static AgentRunDto Map(AgentRun run)
    {
        return new AgentRunDto
        {
            Id = run.Id,
            ConversationId = run.ConversationId,
            AgentType = run.AgentType,
            Status = run.Status,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Error = run.Error
        };
    }
}