using ArtyVoiceBot.Models;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Skype.Bots.Media;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace ArtyVoiceBot.Services;

/// <summary>
/// Core service for Arty bot - handles Teams meeting joining and audio capture
/// Based on teams-recording-bot BotService
/// </summary>
public class ArtyBotService : IDisposable
{
    private readonly BotConfiguration _config;
    private readonly AudioCaptureService _audioCapture;
    private readonly WebhookService _webhookService;
    private readonly ILogger<ArtyBotService> _logger;
    private readonly IGraphLogger _graphLogger;
    
    private ICommunicationsClient? _client;
    private readonly ConcurrentDictionary<string, CallContext> _activeCalls = new();
    private bool _initialized;

    public ArtyBotService(
        BotConfiguration config,
        AudioCaptureService audioCapture,
        WebhookService webhookService,
        ILogger<ArtyBotService> logger,
        IGraphLogger graphLogger)
    {
        _config = config;
        _audioCapture = audioCapture;
        _webhookService = webhookService;
        _logger = logger;
        _graphLogger = graphLogger;
    }

    /// <summary>
    /// Initialize the bot service and Graph Communications client
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
        {
            _logger.LogWarning("Bot service already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing Arty Bot Service...");

            // Create Communications Client Builder
            var builder = new CommunicationsClientBuilder(
                "ArtyVoiceBot",
                _config.AadAppId,
                _graphLogger);

            // Set authentication
            var authProvider = new SimpleAuthenticationProvider(
                _config.AadAppId,
                _config.AadAppSecret,
                _graphLogger);

            builder.SetAuthenticationProvider(authProvider);

            // Set notification URL (where Teams will send callbacks)
            var notificationUrl = new Uri($"https://{_config.ServiceDnsName}:{_config.CallSignalingPort}/api/calling");
            builder.SetNotificationUrl(notificationUrl);

            // Set media platform settings
            var mediaPlatformSettings = new MediaPlatformSettings
            {
                ApplicationId = _config.AadAppId,
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings
                {
                    CertificateThumbprint = _config.CertificateThumbprint,
                    InstanceInternalPort = _config.InstanceInternalPort,
                    InstancePublicPort = _config.InstancePublicPort,
                    ServiceFqdn = _config.ServiceDnsName,
                },
            };

            builder.SetMediaPlatformSettings(mediaPlatformSettings);
            builder.SetServiceBaseUrl(new Uri(_config.PlaceCallEndpointUrl));

            // Build the client
            _client = builder.Build();

            // Subscribe to call events
            _client.Calls().OnIncoming += OnIncomingCall;
            _client.Calls().OnUpdated += OnCallUpdated;

            _initialized = true;
            _logger.LogInformation("Arty Bot Service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Arty Bot Service");
            throw;
        }
    }

    /// <summary>
    /// Join a Teams meeting
    /// </summary>
    public async Task<JoinMeetingResponse> JoinMeetingAsync(JoinMeetingRequest request)
    {
        if (!_initialized || _client == null)
        {
            throw new InvalidOperationException("Bot service not initialized. Call Initialize() first.");
        }

        try
        {
            _logger.LogInformation($"Attempting to join meeting: {request.JoinUrl}");

            // Generate a scenario ID for tracking
            var scenarioId = Guid.NewGuid();

            // Parse the join URL
            var (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(request.JoinUrl);

            // Get tenant ID
            var tenantId = request.TenantId ?? 
                (meetingInfo as OrganizerMeetingInfo)?.Organizer.GetPrimaryIdentity()?.GetTenantId();

            // Create local media session for audio capture
            var mediaSession = CreateLocalMediaSession();

            // Set up join parameters
            var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession)
            {
                TenantId = tenantId,
            };

            // If display name provided, join as guest (NOTE: This prevents unmixed audio access!)
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                _logger.LogWarning("Joining as guest with display name - unmixed audio will NOT be available");
                joinParams.GuestIdentity = new Identity
                {
                    Id = Guid.NewGuid().ToString(),
                    DisplayName = request.DisplayName,
                };
            }

            // Join the call
            var call = await _client.Calls().AddAsync(joinParams, scenarioId);
            var callId = call.Id;

            _logger.LogInformation($"Successfully initiated join to meeting. Call ID: {callId}");

            // Create media stream handler
            var mediaStream = new BotMediaStream(
                mediaSession,
                _audioCapture,
                _webhookService,
                _logger.CreateLogger<BotMediaStream>(),
                callId
            );

            // Track the call
            var callContext = new CallContext
            {
                CallId = callId,
                ScenarioId = scenarioId.ToString(),
                MeetingUrl = request.JoinUrl,
                JoinedAt = DateTime.UtcNow,
                Call = call,
                MediaSession = mediaSession,
                MediaStream = mediaStream
            };

            _activeCalls.TryAdd(callId, callContext);

            // Notify Python backend
            await _webhookService.NotifyMeetingJoinedAsync(callId, request.JoinUrl);

            return new JoinMeetingResponse
            {
                CallId = callId,
                ScenarioId = scenarioId.ToString(),
                Status = "Joining",
                Message = "Bot is joining the meeting"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining meeting");
            await _webhookService.NotifyErrorAsync("unknown", $"Failed to join meeting: {ex.Message}");
            
            throw;
        }
    }

    /// <summary>
    /// Leave a meeting
    /// </summary>
    public async Task<bool> LeaveMeetingAsync(string callId)
    {
        try
        {
            if (!_activeCalls.TryGetValue(callId, out var callContext))
            {
                _logger.LogWarning($"Call {callId} not found in active calls");
                return false;
            }

            _logger.LogInformation($"Leaving call {callId}");

            // Delete the call (hang up)
            await callContext.Call.DeleteAsync();

            // Finalize audio files
            var audioFiles = await _audioCapture.FinalizeCall(callId);

            // Notify Python backend
            await _webhookService.NotifyMeetingLeftAsync(callId, audioFiles);

            // Clean up
            callContext.MediaStream?.Dispose();
            _activeCalls.TryRemove(callId, out _);

            _logger.LogInformation($"Successfully left call {callId}. Captured {audioFiles.Count} audio files");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error leaving call {callId}");
            return false;
        }
    }

    /// <summary>
    /// Get information about active calls
    /// </summary>
    public List<CallInfo> GetActiveCalls()
    {
        return _activeCalls.Values.Select(ctx => new CallInfo
        {
            CallId = ctx.CallId,
            ScenarioId = ctx.ScenarioId,
            MeetingUrl = ctx.MeetingUrl,
            JoinedAt = ctx.JoinedAt,
            Status = ctx.Call.State.ToString()
        }).ToList();
    }

    /// <summary>
    /// Create a local media session for capturing audio
    /// </summary>
    private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Client not initialized");
        }

        try
        {
            // Create media session with audio capture enabled
            return _client.CreateMediaSession(
                new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    SupportedAudioFormat = AudioFormat.Pcm16K, // Teams uses 16kHz PCM
                    ReceiveUnmixedMeetingAudio = true // Get individual speaker streams!
                },
                new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Inactive // We don't need video for POC
                },
                mediaSessionId: mediaSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating local media session");
            throw;
        }
    }

    /// <summary>
    /// Handle incoming calls (for compliance recording scenarios)
    /// </summary>
    private void OnIncomingCall(ICallCollection sender, CollectionEventArgs<ICall> args)
    {
        foreach (var call in args.AddedResources)
        {
            _logger.LogInformation($"Incoming call: {call.Id}");
            // For now, we're only handling explicit join requests
            // Compliance recording would handle this differently
        }
    }

    /// <summary>
    /// Handle call state updates
    /// </summary>
    private void OnCallUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
    {
        foreach (var call in args.AddedResources)
        {
            _logger.LogInformation($"Call updated: {call.Id}, State: {call.State}");
            
            if (_activeCalls.TryGetValue(call.Id, out var context))
            {
                _ = _webhookService.SendStatusAsync(new StatusWebhook
                {
                    CallId = call.Id,
                    Status = call.State.ToString(),
                    Message = $"Call state changed to {call.State}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Handle removed calls
        foreach (var call in args.RemovedResources)
        {
            _logger.LogInformation($"Call removed: {call.Id}");
            
            if (_activeCalls.TryRemove(call.Id, out var context))
            {
                _ = Task.Run(async () =>
                {
                    var audioFiles = await _audioCapture.FinalizeCall(call.Id);
                    await _webhookService.NotifyMeetingLeftAsync(call.Id, audioFiles);
                    context.MediaStream?.Dispose();
                });
            }
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing Arty Bot Service");

        foreach (var context in _activeCalls.Values)
        {
            try
            {
                context.MediaStream?.Dispose();
                context.Call?.DeleteAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error disposing call {context.CallId}");
            }
        }

        _activeCalls.Clear();
        _client?.Dispose();
    }
}

/// <summary>
/// Context for tracking active calls
/// </summary>
internal class CallContext
{
    public string CallId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string MeetingUrl { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public ICall Call { get; set; } = null!;
    public ILocalMediaSession MediaSession { get; set; } = null!;
    public BotMediaStream? MediaStream { get; set; }
}

/// <summary>
/// Simple authentication provider for Graph Communications
/// </summary>
internal class SimpleAuthenticationProvider : Microsoft.Graph.Communications.Client.Authentication.IRequestAuthenticationProvider
{
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly IGraphLogger _logger;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public SimpleAuthenticationProvider(string appId, string appSecret, IGraphLogger logger)
    {
        _appId = appId;
        _appSecret = appSecret;
        _logger = logger;
    }

    public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
    {
        // Get or refresh token
        if (_cachedToken == null || DateTime.UtcNow >= _tokenExpiry)
        {
            await RefreshTokenAsync();
        }

        // Add authorization header
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cachedToken);
    }

    public Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
    {
        // For this POC, we're not validating inbound requests
        // In production, you would validate the request signature from Microsoft Graph
        var result = new RequestValidationResult();
        return Task.FromResult(result);
    }

    private async Task RefreshTokenAsync()
    {
        // This is a simplified version - in production, use Microsoft.Identity.Client (MSAL)
        var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        var scope = "https://graph.microsoft.com/.default";

        using var httpClient = new HttpClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _appId),
            new KeyValuePair<string, string>("client_secret", _appSecret),
            new KeyValuePair<string, string>("scope", scope)
        });

        var response = await httpClient.PostAsync(tokenEndpoint, content);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadAsStringAsync();
        var token = System.Text.Json.JsonDocument.Parse(tokenResponse);
        
        _cachedToken = token.RootElement.GetProperty("access_token").GetString();
        var expiresIn = token.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Refresh 60s early
    }
}

