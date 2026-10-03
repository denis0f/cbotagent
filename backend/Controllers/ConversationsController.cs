using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController(ConversationService conversationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] int userId)
    {
        var conversations = await conversationService
            .GetUserConversationsAsync(userId);

        return Ok(conversations);
    }

    [HttpPost]
    public async Task<IActionResult> CreateConversation(
        CreateConversationDto dto)
    {
        var result = await conversationService
            .CreateConversationAsync(dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetConversation(
        int id,
        [FromQuery] int userId)
    {
        var conversation = await conversationService
            .GetConversationAsync(userId, id);

        if (conversation is null)
            return NotFound(new { message = "Conversation not found." });

        return Ok(conversation);
    }

    [HttpPost("{id:int}/messages")]
    public async Task<IActionResult> AddMessage(
        int id,
        SendMessageDto dto)
    {
        var result = await conversationService
            .AddMessageAsync(id, dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteConversation(
        int id,
        [FromQuery] int userId)
    {
        var result = await conversationService
            .DeleteConversationAsync(userId, id);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return NoContent();
    }
}