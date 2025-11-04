namespace ArtyVoiceBot.Models;

/// <summary>
/// Request to join a Teams meeting
/// </summary>
public class JoinMeetingRequest
{
    /// <summary>
    /// Teams meeting join URL
    /// Example: https://teams.microsoft.com/l/meetup-join/...
    /// </summary>
    public string JoinUrl { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the bot in the meeting (optional)
    /// If not provided, bot joins as application (can access unmixed audio)
    /// If provided, bot joins as guest (cannot access individual audio streams)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Tenant ID for the meeting
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// Response after joining a meeting
/// </summary>
public class JoinMeetingResponse
{
    public string CallId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request to leave a meeting
/// </summary>
public class LeaveMeetingRequest
{
    public string CallId { get; set; } = string.Empty;
}

/// <summary>
/// Audio transcription data sent to Python backend
/// </summary>
public class TranscriptionWebhook
{
    public string CallId { get; set; } = string.Empty;
    public string AudioFilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SpeakerId { get; set; } = string.Empty;
    public string SpeakerName { get; set; } = string.Empty;
    public int DurationMs { get; set; }
}

/// <summary>
/// Bot status update sent to Python backend
/// </summary>
public class StatusWebhook
{
    public string CallId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Active call information
/// </summary>
public class CallInfo
{
    public string CallId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string MeetingUrl { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public List<string> AudioFiles { get; set; } = new();
}

