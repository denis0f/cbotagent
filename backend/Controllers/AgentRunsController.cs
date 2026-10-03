using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api")]
public class AgentRunsController(AgentRunService agentRunService) : ControllerBase
{
    [HttpPost("conversations/{conversationId:int}/agent-runs")]
    public async Task<IActionResult> CreateRun(
        int conversationId,
        CreateAgentRunDto dto)
    {
        var result = await agentRunService
            .CreateRunAsync(conversationId, dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpGet("agent-runs/{id:int}")]
    public async Task<IActionResult> GetRun(
        int id,
        [FromQuery] int userId)
    {
        var run = await agentRunService
            .GetRunAsync(userId, id);

        if (run is null)
            return NotFound(new { message = "Agent run not found." });

        return Ok(run);
    }

    [HttpGet("conversations/{conversationId:int}/agent-runs")]
    public async Task<IActionResult> GetConversationRuns(
        int conversationId,
        [FromQuery] int userId)
    {
        var runs = await agentRunService
            .GetConversationRunsAsync(userId, conversationId);

        return Ok(runs);
    }

    [HttpPost("agent-runs/{id:int}/complete")]
    public async Task<IActionResult> CompleteRun(
        int id,
        [FromQuery] int userId)
    {
        var result = await agentRunService
            .CompleteRunAsync(userId, id);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(new { message = "Agent run completed." });
    }

    [HttpPost("agent-runs/{id:int}/fail")]
    public async Task<IActionResult> FailRun(
        int id,
        [FromQuery] int userId,
        [FromQuery] string error)
    {
        var result = await agentRunService
            .FailRunAsync(userId, id, error);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(new { message = "Agent run failed." });
    }
}