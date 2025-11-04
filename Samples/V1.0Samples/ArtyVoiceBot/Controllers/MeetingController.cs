using ArtyVoiceBot.Models;
using ArtyVoiceBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArtyVoiceBot.Controllers;

/// <summary>
/// API endpoints for Python backend to control meeting join/leave
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MeetingController : ControllerBase
{
    private readonly ArtyBotService _botService;
    private readonly ILogger<MeetingController> _logger;

    public MeetingController(
        ArtyBotService botService,
        ILogger<MeetingController> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    /// <summary>
    /// Join a Teams meeting
    /// POST /api/meeting/join
    /// </summary>
    [HttpPost("join")]
    [ProducesResponseType(typeof(JoinMeetingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JoinMeetingResponse>> JoinMeeting([FromBody] JoinMeetingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.JoinUrl))
            {
                return BadRequest(new { error = "JoinUrl is required" });
            }

            _logger.LogInformation($"Received request to join meeting: {request.JoinUrl}");

            var response = await _botService.JoinMeetingAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining meeting");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Leave a meeting
    /// POST /api/meeting/leave
    /// </summary>
    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LeaveMeeting([FromBody] LeaveMeetingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CallId))
            {
                return BadRequest(new { error = "CallId is required" });
            }

            _logger.LogInformation($"Received request to leave call: {request.CallId}");

            var success = await _botService.LeaveMeetingAsync(request.CallId);
            
            if (!success)
            {
                return NotFound(new { error = $"Call {request.CallId} not found" });
            }

            return Ok(new { message = "Successfully left meeting" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving meeting");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get active calls
    /// GET /api/meeting/active
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<CallInfo>), StatusCodes.Status200OK)]
    public ActionResult<List<CallInfo>> GetActiveCalls()
    {
        try
        {
            var calls = _botService.GetActiveCalls();
            return Ok(calls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active calls");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// GET /api/meeting/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy",
            service = "ArtyVoiceBot",
            timestamp = DateTime.UtcNow
        });
    }
}

