using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;
using Microsoft.AspNetCore.Mvc;

namespace LLMJudgePipeline.Controllers;

[ApiController]
[Route("api")]
public class InferenceController : ControllerBase
{
    private readonly IInferenceService _inferenceService;
    private readonly ILogger<InferenceController> _logger;

    public InferenceController(IInferenceService inferenceService, ILogger<InferenceController> logger)
    {
        _inferenceService = inferenceService;
        _logger = logger;
    }

    /// <summary>
    /// Process a user message and return intent classification with judge validation
    /// </summary>
    /// <param name="request">The inference request containing the user message</param>
    /// <returns>Inference response with intent, confidence, and validation results</returns>
    [HttpPost("infer")]
    [ProducesResponseType(typeof(InferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InferenceResponse>> Infer([FromBody] InferenceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            _logger.LogWarning("Received empty message in inference request");
            return BadRequest(new { error = "Message is required" });
        }

        try
        {
            _logger.LogInformation("Processing inference request: {Message}", request.Message);
            
            var response = await _inferenceService.ProcessAsync(request);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inference request");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
