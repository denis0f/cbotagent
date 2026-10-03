using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace backend.Services;

public class BotService(AppDbContext context)
{
    public async Task<List<BotListDto>> GetUserBotsAsync(int userId)
    {
        return await context.Bots
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.UpdatedAt)
            .Select(b => new BotListDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                CurrentVersionId = b.CurrentVersionId,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<BotDetailsDto?> GetBotAsync(
        int userId,
        int botId)
    {
        return await context.Bots
            .Where(b => b.Id == botId && b.UserId == userId)
            .Select(b => new BotDetailsDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                CurrentVersionId = b.CurrentVersionId,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Versions = b.Versions
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => new BotVersionDto
                    {
                        Id = v.Id,
                        VersionNumber = v.VersionNumber,
                        CompilationStatus = v.CompilationStatus,
                        SourceFilePath = v.SourceFilePath,
                        CompiledFilePath = v.CompiledFilePath,
                        CreatedAt = v.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error, BotListDto? Response)> CreateBotAsync(
        CreateBotDto dto)
    {
        if (!await context.Users.AnyAsync(u => u.Id == dto.UserId))
            return (false, "User not found.", null);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return (false, "Bot name is required.", null);

        if (dto.CreatedFromConversationId.HasValue)
        {
            var conversationExists = await context.Conversations.AnyAsync(c =>
                c.Id == dto.CreatedFromConversationId.Value &&
                c.UserId == dto.UserId);

            if (!conversationExists)
                return (false, "Conversation not found.", null);
        }

        var bot = new Bot
        {
            UserId = dto.UserId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            CreatedFromConversationId = dto.CreatedFromConversationId
        };

        context.Bots.Add(bot);
        await context.SaveChangesAsync();

        return (
            true,
            null,
            new BotListDto
            {
                Id = bot.Id,
                Name = bot.Name,
                Description = bot.Description,
                CurrentVersionId = bot.CurrentVersionId,
                CreatedAt = bot.CreatedAt,
                UpdatedAt = bot.UpdatedAt
            }
        );
    }

    public async Task<(bool Success, string? Error, BotVersionDto? Response)> CreateVersionAsync(
        int botId,
        CreateBotVersionDto dto)
    {
        var bot = await context.Bots
            .FirstOrDefaultAsync(b =>
                b.Id == botId &&
                b.UserId == dto.UserId);

        if (bot is null)
            return (false, "Bot not found.", null);

        if (string.IsNullOrWhiteSpace(dto.SourceCode))
            return (false, "Source code is required.", null);

        var lastVersion = await context.BotVersions
            .Where(v => v.BotId == botId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync() ?? 0;

        var versionNumber = lastVersion + 1;

        var sourceFileResult = await SaveSourceFileAsync(
            dto.UserId,
            bot.Name,
            versionNumber,
            dto.SourceCode);

        var version = new BotVersion
        {
            BotId = botId,
            VersionNumber = versionNumber,
            SourceCode = dto.SourceCode,
            SourceFilePath = sourceFileResult.RelativePath,
            CompilationStatus = dto.CompilationStatus
        };

        context.BotVersions.Add(version);

        bot.CurrentVersion = version;
        bot.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return (
            true,
            null,
            new BotVersionDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                CompilationStatus = version.CompilationStatus,
                SourceFilePath = version.SourceFilePath,
                CompiledFilePath = version.CompiledFilePath,
                CreatedAt = version.CreatedAt
            }
        );
    }

    public async Task<BotCreationSession?> GetCreationSessionAsync(
        int userId,
        int sessionId)
    {
        return await context.BotCreationSessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);
    }

    public async Task<(bool Success, string? Error, BotVersionDto? Response)> SaveCreationSessionVersionAsync(
        int userId,
        BotCreationSession session,
        string botName,
        string sourceCode,
        int? createdByAgentRunId = null)
    {
        if (session.UserId != userId)
            return (false, "Bot creation session does not belong to the user.", null);

        if (session.Status != "AwaitingSourceApproval")
            return (
                false,
                "Bot creation session is not awaiting source approval.",
                null
            );

        if (string.IsNullOrWhiteSpace(sourceCode))
            return (false, "Source code is required.", null);

        if (string.IsNullOrWhiteSpace(botName))
            return (false, "Bot name is required.", null);

        Bot? bot = null;

        if (session.BotId.HasValue)
        {
            bot = await context.Bots
                .FirstOrDefaultAsync(b =>
                    b.Id == session.BotId.Value &&
                    b.UserId == userId);

            if (bot is null)
                return (
                    false,
                    "The bot linked to this creation session was not found.",
                    null
                );
        }
        else
        {
            var safeName = botName.Trim();

            bot = new Bot
            {
                UserId = userId,
                Name = safeName,
                CreatedFromConversationId = session.ConversationId
            };

            context.Bots.Add(bot);

            await context.SaveChangesAsync();

            session.BotId = bot.Id;
        }

        var lastVersion = await context.BotVersions
            .Where(v => v.BotId == bot.Id)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync() ?? 0;

        var versionNumber = lastVersion + 1;

        var sourceFileResult = await SaveSourceFileAsync(
            userId,
            bot.Name,
            versionNumber,
            sourceCode);

        var version = new BotVersion
        {
            BotId = bot.Id,
            VersionNumber = versionNumber,
            SourceCode = sourceCode,
            SourceFilePath = sourceFileResult.RelativePath,
            CompilationStatus = "NotCompiled",
            CreatedByAgentRunId = createdByAgentRunId
        };

        context.BotVersions.Add(version);

        bot.CurrentVersion = version;
        bot.UpdatedAt = DateTime.UtcNow;

        session.GeneratedSourceCode = sourceCode;
        session.Status = "Saved";
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return (
            true,
            null,
            new BotVersionDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                CompilationStatus = version.CompilationStatus,
                SourceFilePath = version.SourceFilePath,
                CompiledFilePath = version.CompiledFilePath,
                CreatedAt = version.CreatedAt
            }
        );
    }

    public async Task<List<BotVersionDto>?> GetVersionsAsync(
        int userId,
        int botId)
    {
        var botExists = await context.Bots.AnyAsync(b =>
            b.Id == botId &&
            b.UserId == userId);

        if (!botExists)
            return null;

        return await context.BotVersions
            .Where(v => v.BotId == botId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new BotVersionDto
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                CompilationStatus = v.CompilationStatus,
                SourceFilePath = v.SourceFilePath,
                CompiledFilePath = v.CompiledFilePath,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<BotSourceDto?> GetSourceAsync(
        int userId,
        int botId,
        int? versionNumber = null)
    {
        var bot = await context.Bots
            .FirstOrDefaultAsync(b =>
                b.Id == botId &&
                b.UserId == userId);

        if (bot is null)
            return null;

        var version = versionNumber.HasValue
            ? await context.BotVersions.FirstOrDefaultAsync(v =>
                v.BotId == botId &&
                v.VersionNumber == versionNumber.Value)
            : await context.BotVersions.FirstOrDefaultAsync(v =>
                v.Id == bot.CurrentVersionId);

        if (version is null)
            return null;

        return new BotSourceDto
        {
            BotId = bot.Id,
            VersionNumber = version.VersionNumber,
            SourceCode = version.SourceCode,
            CompilationStatus = version.CompilationStatus,
            SourceFilePath = version.SourceFilePath,
            CompiledFilePath = version.CompiledFilePath
        };
    }

    public async Task<(bool Success, string? Error)> DeleteBotAsync(
        int userId,
        int botId)
    {
        var bot = await context.Bots
            .FirstOrDefaultAsync(b =>
                b.Id == botId &&
                b.UserId == userId);

        if (bot is null)
            return (false, "Bot not found.");

        context.Bots.Remove(bot);
        await context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<BotVersion?> GetVersionAsync(
        int userId,
        int botId,
        int versionNumber)
    {
        return await context.BotVersions
            .Include(v => v.Bot)
            .FirstOrDefaultAsync(v =>
                v.BotId == botId &&
                v.VersionNumber == versionNumber &&
                v.Bot.UserId == userId);
    }

    public async Task<(bool Success, string? Error, string? FilePath, string? FileName)> GetCompiledBotAsync(
        int userId,
        int botId,
        int versionNumber)
    {
        var version = await context.BotVersions
            .Include(v => v.Bot)
            .FirstOrDefaultAsync(v =>
                v.BotId == botId &&
                v.VersionNumber == versionNumber &&
                v.Bot.UserId == userId);

        if (version is null)
            return (false, "Bot version not found.", null, null);

        if (version.CompilationStatus != "Compiled")
            return (
                false,
                "This bot version has not been successfully compiled.",
                null,
                null
            );

        if (string.IsNullOrWhiteSpace(version.CompiledFilePath))
            return (
                false,
                "The compiled bot file path is not available.",
                null,
                null
            );

        var absolutePath = Path.IsPathRooted(version.CompiledFilePath)
            ? version.CompiledFilePath
            : Path.Combine(
                Directory.GetCurrentDirectory(),
                version.CompiledFilePath);

        if (!File.Exists(absolutePath))
            return (
                false,
                "The compiled bot file could not be found.",
                null,
                null
            );

        var fileName = Path.GetFileName(absolutePath);

        return (true, null, absolutePath, fileName);
    }

    public async Task UpdateCompilationResultAsync(
        int userId,
        int botId,
        int versionNumber,
        string compilationStatus,
        string? compiledFilePath)
    {
        var version = await context.BotVersions
            .Include(v => v.Bot)
            .FirstOrDefaultAsync(v =>
                v.BotId == botId &&
                v.VersionNumber == versionNumber &&
                v.Bot.UserId == userId);

        if (version is null)
            throw new InvalidOperationException(
                "Bot version not found.");

        version.CompilationStatus = compilationStatus;
        version.CompiledFilePath = compiledFilePath;

        version.Bot.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    private static async Task<(string AbsolutePath, string RelativePath)> SaveSourceFileAsync(
        int userId,
        string botName,
        int versionNumber,
        string sourceCode)
    {
        var safeBotName = SanitizeBotName(botName);

        var sourceRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "source_files");

        var userSourceDirectory = Path.Combine(
            sourceRoot,
            userId.ToString());

        Directory.CreateDirectory(userSourceDirectory);

        var fileName = $"{safeBotName}_{versionNumber}.cs";

        var absolutePath = Path.Combine(
            userSourceDirectory,
            fileName);

        await File.WriteAllTextAsync(
            absolutePath,
            sourceCode);

        var relativePath = Path.Combine(
            "source_files",
            userId.ToString(),
            fileName)
            .Replace('\\', '/');

        return (absolutePath, relativePath);
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
}