using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Transport;
using ArtyVoiceBot.Services;
using System.Net;

namespace ArtyVoiceBot.Controllers;

/// <summary>
/// Handles callbacks from Microsoft Graph Communications platform
/// This is where Teams sends notifications about call events
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/Callback")]  // Support both lowercase and PascalCase
public class CallbackController : ControllerBase
{
    private readonly ArtyBotService _botService;
    private readonly ILogger<CallbackController> _logger;

    public CallbackController(
        ArtyBotService botService,
        ILogger<CallbackController> logger)
    {
        _botService = botService;
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
            _logger.LogInformation("📞 Received callback from Graph Communications");

            // Read the request body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            _logger.LogInformation($"Callback body length: {body.Length} bytes");
            _logger.LogDebug($"Callback body: {body}");

            // Process the callback through the Communications Client
            // The client needs to handle this to update call state
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod(Request.Method),
                Content = new StringContent(body)
            };
            
            // Copy headers
            foreach (var header in Request.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            
            // Copy request URI
            request.RequestUri = new Uri(
                $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");
            
            // Process through the bot's communications client
            var response = await _botService.Client.ProcessNotificationAsync(request);
            
            _logger.LogInformation($"✅ Callback processed. Status: {response.StatusCode}");
            
            // Return the response from the SDK
            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing callback");
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

