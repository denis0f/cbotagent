using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BotsController(BotService botService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBots([FromQuery] int userId)
    {
        var bots = await botService.GetUserBotsAsync(userId);

        return Ok(bots);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBot(CreateBotDto dto)
    {
        var result = await botService.CreateBotAsync(dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBot(
        int id,
        [FromQuery] int userId)
    {
        var bot = await botService.GetBotAsync(userId, id);

        if (bot is null)
            return NotFound(new { message = "Bot not found." });

        return Ok(bot);
    }

    [HttpPost("{id:int}/versions")]
    public async Task<IActionResult> CreateVersion(
        int id,
        CreateBotVersionDto dto)
    {
        var result = await botService.CreateVersionAsync(id, dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Response);
    }

    [HttpGet("{id:int}/versions")]
    public async Task<IActionResult> GetVersions(
        int id,
        [FromQuery] int userId)
    {
        var versions = await botService.GetVersionsAsync(userId, id);

        if (versions is null)
            return NotFound(new { message = "Bot not found." });

        return Ok(versions);
    }

    [HttpGet("{id:int}/versions/{version:int}/download")]
    public async Task<IActionResult> DownloadVersion(
        int id,
        int version,
        [FromQuery] int userId)
    {
        // var result = await botService.GetCompiledBotAsync(
        //     userId,
        //     id,
        //     version);

        // if (!result.Success)
        //     return NotFound(new { message = result.Error });

        return PhysicalFile(
            "/home/dengt1/dev/cbotagent/backend/compiled-bots/1/DenisBot.algo",
            "application/octet-stream",
            "DenisBot.algo");
    }

    [HttpGet("{id:int}/source")]
    public async Task<IActionResult> GetSource(
        int id,
        [FromQuery] int userId,
        [FromQuery] int? version)
    {
        var source = await botService.GetSourceAsync(
            userId,
            id,
            version);

        if (source is null)
            return NotFound(new { message = "Bot or version not found." });

        return Ok(source);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBot(
        int id,
        [FromQuery] int userId)
    {
        var result = await botService.DeleteBotAsync(userId, id);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return NoContent();
    }
}