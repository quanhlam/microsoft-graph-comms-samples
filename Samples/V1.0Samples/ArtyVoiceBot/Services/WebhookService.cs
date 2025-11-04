using ArtyVoiceBot.Models;
using System.Text;
using System.Text.Json;

namespace ArtyVoiceBot.Services;

/// <summary>
/// Service for sending webhooks to Python FastAPI backend
/// </summary>
public class WebhookService
{
    private readonly HttpClient _httpClient;
    private readonly PythonBackendSettings _settings;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IHttpClientFactory httpClientFactory,
        PythonBackendSettings settings,
        ILogger<WebhookService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("PythonBackend");
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Send transcription data to Python backend
    /// </summary>
    public async Task SendTranscriptionAsync(TranscriptionWebhook data)
    {
        try
        {
            var url = $"{_settings.BaseUrl}{_settings.TranscriptionWebhookPath}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation($"Sending transcription webhook to: {url}");
            
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Successfully sent transcription webhook for call {data.CallId}");
            }
            else
            {
                _logger.LogWarning($"Failed to send transcription webhook. Status: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            // Python backend not running - this is OK for POC
            _logger.LogDebug($"Python backend not available (this is OK if testing without Python)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending transcription webhook");
        }
    }

    /// <summary>
    /// Send status update to Python backend
    /// </summary>
    public async Task SendStatusAsync(StatusWebhook data)
    {
        try
        {
            var url = $"{_settings.BaseUrl}{_settings.StatusWebhookPath}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation($"Sending status webhook to: {url} - Status: {data.Status}");
            
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Successfully sent status webhook for call {data.CallId}");
            }
            else
            {
                _logger.LogWarning($"Failed to send status webhook. Status: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            // Python backend not running - this is OK for POC, just log as debug
            _logger.LogDebug($"Python backend not available at {_settings.BaseUrl} (this is OK if testing without Python)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending status webhook");
        }
    }

    /// <summary>
    /// Notify Python backend that bot joined a meeting
    /// </summary>
    public async Task NotifyMeetingJoinedAsync(string callId, string meetingUrl)
    {
        await SendStatusAsync(new StatusWebhook
        {
            CallId = callId,
            Status = "joined",
            Message = $"Bot successfully joined meeting: {meetingUrl}",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify Python backend that bot left a meeting
    /// </summary>
    public async Task NotifyMeetingLeftAsync(string callId, List<string> audioFiles)
    {
        await SendStatusAsync(new StatusWebhook
        {
            CallId = callId,
            Status = "left",
            Message = $"Bot left meeting. Captured {audioFiles.Count} audio files.",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify Python backend of an error
    /// </summary>
    public async Task NotifyErrorAsync(string callId, string errorMessage)
    {
        await SendStatusAsync(new StatusWebhook
        {
            CallId = callId,
            Status = "error",
            Message = errorMessage,
            Timestamp = DateTime.UtcNow
        });
    }
}

