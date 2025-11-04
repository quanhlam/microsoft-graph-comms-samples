namespace ArtyVoiceBot.Models;

/// <summary>
/// Bot configuration settings
/// </summary>
public class BotConfiguration
{
    public string BotName { get; set; } = "Arty";
    public string AadAppId { get; set; } = string.Empty;
    public string AadAppSecret { get; set; } = string.Empty;
    public string ServiceDnsName { get; set; } = string.Empty;
    public string ServiceCname { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public int InstancePublicPort { get; set; } = 8445;
    public int InstanceInternalPort { get; set; } = 8445;
    public int CallSignalingPort { get; set; } = 9441;
    public string PlaceCallEndpointUrl { get; set; } = "https://graph.microsoft.com/v1.0";
    public string GraphApiBaseUrl { get; set; } = "https://graph.microsoft.com/v1.0";
}

/// <summary>
/// Audio capture settings
/// </summary>
public class AudioSettings
{
    public string OutputFolder { get; set; } = "AudioCapture";
    public int SampleRate { get; set; } = 16000;
    public int BitsPerSample { get; set; } = 16;
    public int Channels { get; set; } = 1;
    public bool CaptureUnmixedAudio { get; set; } = true;
}

/// <summary>
/// Python backend integration settings
/// </summary>
public class PythonBackendSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string TranscriptionWebhookPath { get; set; } = "/api/arty/transcription";
    public string StatusWebhookPath { get; set; } = "/api/arty/status";
}

