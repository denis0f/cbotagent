using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api")]
public class BotCreationController(BotCreationService botCreationService)
    : ControllerBase
{
    [HttpPost("conversations/{conversationId:int}/bot-creation")]
    public async Task<IActionResult> Start(
        int conversationId,
        StartBotCreationDto dto)
    {
        var result = await botCreationService.StartAsync(
            conversationId,
            dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpGet("bot-creation/{id:int}")]
    public async Task<IActionResult> Get(
        int id,
        [FromQuery] int userId)
    {
        var session = await botCreationService.GetAsync(
            userId,
            id);

        if (session is null)
            return NotFound(new { message = "Bot creation session not found." });

        return Ok(session);
    }

    [HttpGet("conversations/{conversationId:int}/bot-creation")]
    public async Task<IActionResult> GetConversationSession(
        int conversationId,
        [FromQuery] int userId)
    {
        var session = await botCreationService
            .GetConversationSessionAsync(
                userId,
                conversationId);

        if (session is null)
            return NotFound(new { message = "Bot creation session not found." });

        return Ok(session);
    }

    [HttpPost("bot-creation/{id:int}/approve")]
    public async Task<IActionResult> Approve(
        int id,
        [FromQuery] int userId)
    {
        var result = await botCreationService.ApproveAsync(
            userId,
            id);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new
        {
            message = "cBot specification approved. Coding can begin."
        });
    }

    [HttpPost("bot-creation/{id:int}/reject")]
    public async Task<IActionResult> Reject(
        int id,
        [FromQuery] int userId)
    {
        var result = await botCreationService.RejectAsync(
            userId,
            id);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(new
        {
            message = "Bot creation session discarded."
        });
    }

    [HttpPost("bot-creation/{id:int}/refine")]
    public async Task<IActionResult> Refine(
        int id,
        RefineBotCreationDto dto)
    {
        var result = await botCreationService.RefineAsync(
            dto.UserId,
            id,
            dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpPost("bot-creation/{id:int}/generate-source")]
    public async Task<IActionResult> GenerateSource(
        int id,
        [FromQuery] int userId)
    {
        var result = await botCreationService.GenerateSourceAsync(
            userId,
            id);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }
    [HttpPost("bot-creation/{id:int}/save")]
public async Task<IActionResult> SaveSource(
    int id,
    [FromQuery] int userId,
    [FromQuery] string botName)
{
    var result = await botCreationService.SaveSourceAsync(
        userId,
        id,
        botName);

    if (!result.Success)
        return BadRequest(new { message = result.Error });

    return Ok(result.Response);
}

[HttpPost("bot-creation/{id:int}/compile")]
public async Task<IActionResult> CompileSource(
    int id,
    [FromQuery] int userId)
{
    var result = await botCreationService.CompileSourceAsync(
        userId,
        id);

    if (!result.Success)
        return BadRequest(new { message = result.Error });

    return Ok(result.Response);
}
}