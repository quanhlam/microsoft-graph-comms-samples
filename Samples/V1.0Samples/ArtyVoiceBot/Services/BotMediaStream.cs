using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;

namespace ArtyVoiceBot.Services;

/// <summary>
/// Handles media streams from Teams calls
/// Based on teams-recording-bot BotMediaStream
/// </summary>
public class BotMediaStream : IDisposable
{
    private readonly ILocalMediaSession _mediaSession;
    private readonly AudioCaptureService _audioCapture;
    private readonly WebhookService _webhookService;
    private readonly ILogger<BotMediaStream> _logger;
    private readonly string _callId;
    private bool _disposed;

    public BotMediaStream(
        ILocalMediaSession mediaSession,
        AudioCaptureService audioCapture,
        WebhookService webhookService,
        ILogger<BotMediaStream> logger,
        string callId)
    {
        _mediaSession = mediaSession ?? throw new ArgumentNullException(nameof(mediaSession));
        _audioCapture = audioCapture ?? throw new ArgumentNullException(nameof(audioCapture));
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _callId = callId;

        // Subscribe to audio events
        if (_mediaSession.AudioSocket == null)
        {
            throw new InvalidOperationException("Media session must have an audio socket");
        }

        _mediaSession.AudioSocket.AudioMediaReceived += OnAudioMediaReceived;
        _logger.LogInformation($"BotMediaStream initialized for call {_callId}");
    }

    /// <summary>
    /// Called when audio media is received from the meeting
    /// This is where we capture the audio!
    /// </summary>
    private async void OnAudioMediaReceived(object sender, AudioMediaReceivedEventArgs e)
    {
        try
        {
            _logger.LogDebug(
                $"Received Audio: Length={e.Buffer.Length}, " +
                $"Timestamp={e.Buffer.Timestamp}, " +
                $"IsSilence={e.Buffer.IsSilence()}");

            // Get the audio data
            var audioData = e.Buffer.Data;
            
            // Process unmixed audio (individual speakers) if available
            if (e.Buffer.UnmixedAudioBuffers != null && e.Buffer.UnmixedAudioBuffers.Length > 0)
            {
                _logger.LogInformation($"Received unmixed audio with {e.Buffer.UnmixedAudioBuffers.Length} speakers");
                
                foreach (var unmixedBuffer in e.Buffer.UnmixedAudioBuffers)
                {
                    // Get speaker information
                    var speakerId = unmixedBuffer.ActiveSpeakerId ?? "unknown";
                    var speakerName = "Speaker"; // You can map this to actual names from participants
                    
                    // Convert IntPtr to byte array
                    var unmixedData = new byte[unmixedBuffer.Length];
                    System.Runtime.InteropServices.Marshal.Copy(
                        unmixedBuffer.Data,
                        unmixedData,
                        0,
                        (int)unmixedBuffer.Length
                    );

                    // Save audio for this speaker
                    await _audioCapture.ProcessAudioBuffer(
                        unmixedData,
                        speakerId,
                        speakerName,
                        e.Buffer.Timestamp
                    );
                }
            }
            else if (audioData != IntPtr.Zero && e.Buffer.Length > 0)
            {
                // Process mixed audio (all speakers combined)
                _logger.LogDebug($"Received mixed audio: {e.Buffer.Length} bytes");
                
                var mixedData = new byte[e.Buffer.Length];
                System.Runtime.InteropServices.Marshal.Copy(
                    audioData,
                    mixedData,
                    0,
                    (int)e.Buffer.Length
                );

                // Save mixed audio
                await _audioCapture.ProcessAudioBuffer(
                    mixedData,
                    "mixed",
                    "AllSpeakers",
                    e.Buffer.Timestamp
                );
            }

            // IMPORTANT: Always dispose the buffer when done
            e.Buffer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio media");
            e.Buffer?.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (_mediaSession?.AudioSocket != null)
            {
                _mediaSession.AudioSocket.AudioMediaReceived -= OnAudioMediaReceived;
            }
            
            _logger.LogInformation($"BotMediaStream disposed for call {_callId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing BotMediaStream");
        }

        _disposed = true;
    }
}

