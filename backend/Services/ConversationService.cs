using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace backend.Services;

public class ConversationService(
    AppDbContext context,
    AgentService agentService,
    AgentRunService agentRunService)
{
    public async Task<List<ConversationListDto>> GetUserConversationsAsync(
        int userId)
    {
        return await context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationListDto
            {
                Id = c.Id,
                Title = c.Title,
                Summary = c.Summary,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ConversationDetailsDto?> GetConversationAsync(
        int userId,
        int conversationId)
    {
        return await context.Conversations
            .Where(c =>
                c.Id == conversationId &&
                c.UserId == userId)
            .Select(c => new ConversationDetailsDto
            {
                Id = c.Id,
                Title = c.Title,
                Summary = c.Summary,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Messages = c.Messages
                    .OrderBy(m => m.Sequence)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        Role = m.Role,
                        Content = m.Content,
                        Sequence = m.Sequence,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error, ConversationListDto? Response)>
        CreateConversationAsync(CreateConversationDto dto)
    {
        if (dto.UserId <= 0)
            return (false, "User is required.", null);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return (false, "Conversation title is required.", null);

        var userExists = await context.Users
            .AnyAsync(u => u.Id == dto.UserId);

        if (!userExists)
            return (false, "User not found.", null);

        var conversation = new Conversation
        {
            UserId = dto.UserId,
            Title = dto.Title.Trim()
        };

        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        return (
            true,
            null,
            new ConversationListDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                Summary = conversation.Summary,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            }
        );
    }

    public async Task<(bool Success, string? Error, SendMessageResponseDto? Response)>
        AddMessageAsync(
            int conversationId,
            SendMessageDto dto)
    {
        var conversation = await context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                c.Id == conversationId &&
                c.UserId == dto.UserId);

        if (conversation is null)
            return (false, "Conversation not found.", null);

        if (string.IsNullOrWhiteSpace(dto.Content))
            return (false, "Message content is required.", null);

        var runResult = await agentRunService.CreateRunAsync(
            conversationId,
            new CreateAgentRunDto
            {
                UserId = dto.UserId,
                AgentType = "Conversation"
            });

        if (!runResult.Success || runResult.Response is null)
            return (false, runResult.Error ?? "Failed to create agent run.", null);

        var agentRunId = runResult.Response.Id;

        try
        {
            var userMessage = new Message
            {
                ConversationId = conversationId,
                Role = "user",
                Content = dto.Content.Trim(),
                Sequence = conversation.Messages.Count + 1
            };

            context.Messages.Add(userMessage);

            conversation.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var messages = conversation.Messages
                .OrderBy(m => m.Sequence)
                .Select(m => new ChatMessage(
                    m.Role == "assistant"
                        ? ChatRole.Assistant
                        : ChatRole.User,
                    m.Content))
                .ToList();

            var assistantContent = await agentService
                .GenerateResponseAsync(messages);

            var assistantMessage = new Message
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = assistantContent,
                Sequence = userMessage.Sequence + 1
            };

            context.Messages.Add(assistantMessage);

            conversation.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await agentRunService.CompleteRunAsync(
                dto.UserId,
                agentRunId);

            return (
                true,
                null,
                new SendMessageResponseDto
                {
                    UserMessage = new MessageDto
                    {
                        Id = userMessage.Id,
                        Role = userMessage.Role,
                        Content = userMessage.Content,
                        Sequence = userMessage.Sequence,
                        CreatedAt = userMessage.CreatedAt
                    },
                    AssistantMessage = new MessageDto
                    {
                        Id = assistantMessage.Id,
                        Role = assistantMessage.Role,
                        Content = assistantMessage.Content,
                        Sequence = assistantMessage.Sequence,
                        CreatedAt = assistantMessage.CreatedAt
                    }
                }
            );
        }
        catch (Exception ex)
        {
            await agentRunService.FailRunAsync(
                dto.UserId,
                agentRunId,
                ex.Message);

            return (
                false,
                "Failed to generate assistant response.",
                null
            );
        }
    }

    public async Task<(bool Success, string? Error)> DeleteConversationAsync(
        int userId,
        int conversationId)
    {
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c =>
                c.Id == conversationId &&
                c.UserId == userId);

        if (conversation is null)
            return (false, "Conversation not found.");

        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync();

        return (true, null);
    }
}