using ArtyVoiceBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArtyVoiceBot.Controllers;

/// <summary>
/// API endpoints for accessing captured audio files
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AudioController : ControllerBase
{
    private readonly AudioCaptureService _audioCaptureService;
    private readonly ILogger<AudioController> _logger;

    public AudioController(
        AudioCaptureService audioCaptureService,
        ILogger<AudioController> logger)
    {
        _audioCaptureService = audioCaptureService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of all captured audio files
    /// GET /api/audio/files
    /// </summary>
    [HttpGet("files")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public ActionResult<List<string>> GetAudioFiles()
    {
        try
        {
            var files = _audioCaptureService.GetAudioFiles();
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audio files");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download a specific audio file
    /// GET /api/audio/download?filename=example.wav
    /// </summary>
    [HttpGet("download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadAudioFile([FromQuery] string filename)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return BadRequest(new { error = "Filename is required" });
            }

            var files = _audioCaptureService.GetAudioFiles();
            var file = files.FirstOrDefault(f => Path.GetFileName(f) == filename);

            if (file == null || !System.IO.File.Exists(file))
            {
                return NotFound(new { error = $"File {filename} not found" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(file);
            return File(fileBytes, "audio/wav", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading audio file");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

