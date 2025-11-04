using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Transport;

namespace ArtyVoiceBot.Controllers;

/// <summary>
/// Handles callbacks from Microsoft Graph Communications platform
/// This is where Teams sends notifications about call events
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CallbackController : ControllerBase
{
    private readonly ILogger<CallbackController> _logger;

    public CallbackController(ILogger<CallbackController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle calling callbacks from Graph Communications
    /// POST /api/callback/calling
    /// </summary>
    [HttpPost("calling")]
    public async Task<IActionResult> OnIncomingRequest()
    {
        try
        {
            _logger.LogInformation("Received callback from Graph Communications");

            // Read the request body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            _logger.LogDebug($"Callback body: {body}");

            // The Graph Communications SDK will handle this automatically
            // when properly configured with the CommunicationsClient
            // For now, we'll just acknowledge receipt
            
            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing callback");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Health check for callback endpoint
    /// GET /api/callback/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy",
            endpoint = "callback",
            timestamp = DateTime.UtcNow
        });
    }
}

