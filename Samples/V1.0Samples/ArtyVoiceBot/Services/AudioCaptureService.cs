using ArtyVoiceBot.Models;
using NAudio.Wave;
using System.Collections.Concurrent;

namespace ArtyVoiceBot.Services;

/// <summary>
/// Service for capturing and saving audio streams to WAV files
/// Based on teams-recording-bot AudioProcessor
/// </summary>
public class AudioCaptureService : IDisposable
{
    private readonly AudioSettings _settings;
    private readonly ILogger<AudioCaptureService> _logger;
    private readonly ConcurrentDictionary<string, WaveFileWriter> _writers = new();
    private readonly string _outputPath;
    private bool _disposed;

    public AudioCaptureService(
        AudioSettings settings,
        ILogger<AudioCaptureService> logger)
    {
        _settings = settings;
        _logger = logger;
        
        // Create output directory
        _outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            _settings.OutputFolder
        );
        
        Directory.CreateDirectory(_outputPath);
        _logger.LogInformation($"Audio capture output directory: {_outputPath}");
    }

    /// <summary>
    /// Process and save audio buffer to WAV file
    /// </summary>
    public async Task ProcessAudioBuffer(
        byte[] audioData,
        string speakerId,
        string speakerName,
        long timestamp)
    {
        try
        {
            if (audioData == null || audioData.Length == 0)
            {
                return;
            }

            // Get or create WAV writer for this speaker
            var writer = GetOrCreateWriter(speakerId, speakerName);
            
            // Write audio data
            await writer.WriteAsync(audioData, 0, audioData.Length);
            
            _logger.LogDebug($"Wrote {audioData.Length} bytes for speaker {speakerName} ({speakerId})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing audio buffer for speaker {speakerId}");
        }
    }

    /// <summary>
    /// Get or create a WAV file writer for a specific speaker
    /// </summary>
    private WaveFileWriter GetOrCreateWriter(string speakerId, string speakerName)
    {
        if (_writers.TryGetValue(speakerId, out var existingWriter))
        {
            return existingWriter;
        }

        // Create new WAV file for this speaker
        var sanitizedName = SanitizeFileName(speakerName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        // Take up to 8 chars from speakerId, or less if it's shorter
        var speakerIdShort = speakerId.Length > 8 ? speakerId.Substring(0, 8) : speakerId;
        var fileName = $"{timestamp}_{sanitizedName}_{speakerIdShort}.wav";
        var filePath = Path.Combine(_outputPath, fileName);

        // Initialize Wave Format using PCM 16bit 16kHz (Teams default)
        var waveFormat = new WaveFormat(
            rate: _settings.SampleRate,
            bits: _settings.BitsPerSample,
            channels: _settings.Channels
        );

        var writer = new WaveFileWriter(filePath, waveFormat);
        _writers.TryAdd(speakerId, writer);

        _logger.LogInformation($"Created new audio file: {fileName}");

        return writer;
    }

    /// <summary>
    /// Create a WAV file for mixed audio (all speakers)
    /// </summary>
    public WaveFileWriter CreateMixedAudioWriter(string callId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        // Take up to 8 chars from callId, or less if it's shorter
        var callIdShort = callId.Length > 8 ? callId.Substring(0, 8) : callId;
        var fileName = $"{timestamp}_mixed_{callIdShort}.wav";
        var filePath = Path.Combine(_outputPath, fileName);

        var waveFormat = new WaveFormat(
            rate: _settings.SampleRate,
            bits: _settings.BitsPerSample,
            channels: _settings.Channels
        );

        var writer = new WaveFileWriter(filePath, waveFormat);
        _writers.TryAdd($"mixed_{callId}", writer);

        _logger.LogInformation($"Created mixed audio file: {fileName}");

        return writer;
    }

    /// <summary>
    /// Finalize all audio files for a call
    /// </summary>
    public async Task<List<string>> FinalizeCall(string callId)
    {
        var audioFiles = new List<string>();

        try
        {
            // Flush and close all writers
            foreach (var kvp in _writers)
            {
                var writer = kvp.Value;
                await writer.FlushAsync();
                audioFiles.Add(writer.Filename);
                writer.Dispose();
            }

            _writers.Clear();

            _logger.LogInformation($"Finalized {audioFiles.Count} audio files for call {callId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error finalizing audio files for call {callId}");
        }

        return audioFiles;
    }

    /// <summary>
    /// Get all audio files in the output directory
    /// </summary>
    public List<string> GetAudioFiles()
    {
        if (!Directory.Exists(_outputPath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(_outputPath, "*.wav")
            .OrderByDescending(f => File.GetCreationTime(f))
            .ToList();
    }

    /// <summary>
    /// Sanitize file name to remove invalid characters
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var writer in _writers.Values)
        {
            try
            {
                writer.Flush();
                writer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing audio writer");
            }
        }

        _writers.Clear();
        _disposed = true;
    }
}

