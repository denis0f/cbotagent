using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/compiler")]
public class CompilerController(
    CompilationService compilationService) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        CompileSourceDto dto)
    {
        if (dto.UserId <= 0)
            return BadRequest(new { message = "User ID is required." });

        if (string.IsNullOrWhiteSpace(dto.SourceCode))
            return BadRequest(new { message = "Source code is required." });

        if (string.IsNullOrWhiteSpace(dto.BotName))
            return BadRequest(new { message = "Bot name is required." });

        if (dto.Version <= 0)
            return BadRequest(new { message = "Version must be greater than zero." });

        var result = await compilationService.CompileAsync(
            dto.UserId,
            dto.SourceCode,
            dto.BotName,
            dto.Version);

        return Ok(result);
    }
}