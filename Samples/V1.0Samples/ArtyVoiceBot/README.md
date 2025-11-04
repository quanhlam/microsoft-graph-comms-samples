# Arty Voice Bot - Teams Meeting Audio Capture

A .NET 8.0 bot that joins Microsoft Teams meetings, captures audio in real-time, and saves it to WAV files. Designed to integrate with your Python FastAPI backend.

## 🎯 What It Does

1. **Joins Teams Meetings**: Bot joins via meeting URL
2. **Captures Audio**: Records audio from meeting participants  
3. **Saves to WAV**: Creates 16kHz PCM WAV files for each speaker
4. **REST API**: Your Python backend controls the bot via HTTP
5. **Webhooks**: Bot sends status updates and transcription data to your Python backend

## 🏗️ Architecture

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│                 │  HTTP   │                  │  Graph  │                 │
│  Python FastAPI ├────────►│  ArtyVoiceBot    ├────────►│  Teams Meeting  │
│   (Your Code)   │  API    │   (.NET 8.0)     │   API   │                 │
│                 │◄────────┤                  │◄────────┤                 │
└─────────────────┘ Webhook └──────────────────┘         └─────────────────┘
                                      │
                                      ▼
                              ┌──────────────┐
                              │  WAV Files   │
                              │  (Audio)     │
                              └──────────────┘
```

## 📋 Prerequisites

### Required

- **Windows Laptop** (you have this! ✅)
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** or **VS Code** with C# extension
- **ngrok Pro Account** - [Sign up](https://ngrok.com/) (Required for reserved domains & TCP tunneling)
- **Azure Subscription** - For bot registration
- **Microsoft 365** - Teams account with meeting creation permissions

### Optional
- **Postman** - For testing API endpoints

## 🚀 Setup Guide

### Step 1: Register Bot in Azure

1. Go to [Azure Portal](https://portal.azure.com)
2. Create a new **Azure Bot** resource
3. Note down:
   - **Application (client) ID**: `YOUR_BOT_APP_ID`
   - Generate a **client secret**: `YOUR_BOT_APP_SECRET`

4. Grant API Permissions:
   - Go to **API Permissions** → **Add permission** → **Microsoft Graph** → **Application Permissions**
   - Add these permissions:
     - `Calls.AccessMedia.All`
     - `Calls.JoinGroupCall.All`
     - `Calls.JoinGroupCallAsGuest.All`
   - Click **Grant admin consent**

5. Configure Bot Settings:
   - Go to **Configuration**
   - Set **Messaging endpoint**: `https://YOUR_NGROK_DOMAIN/api/callback/calling`
   - Enable **Microsoft Teams** channel

### Step 2: Setup ngrok

1. **Reserve a Domain**:
   - Go to [ngrok Dashboard → Domains](https://dashboard.ngrok.com/endpoints/domains)
   - Reserve a domain (e.g., `arty-bot.ngrok.io`)
   - Note: Must be **US region**

2. **Reserve TCP Address**:
   - Go to [ngrok Dashboard → TCP Addresses](https://dashboard.ngrok.com/endpoints/tcp-addresses)
   - Reserve a TCP address
   - Note the port (e.g., `12345.tcp.ngrok.io` → port is `12345`)

3. **Create ngrok config** (`ngrok.yml`):
   ```yaml
   version: "2"
   authtoken: YOUR_NGROK_AUTH_TOKEN
   tunnels:
     signaling:
       proto: http
       addr: 9441
       hostname: arty-bot.ngrok.io
     media:
       proto: tcp
       addr: 8445
       remote_addr: 12345.tcp.ngrok.io
   ```

4. **Start ngrok**:
   ```bash
   ngrok start --all --config ngrok.yml
   ```

### Step 3: Setup SSL Certificate

The bot needs a certificate for secure media communication.

#### Option A: Use ngrok's Certificate (Easiest for POC)

Since ngrok handles SSL, you can use a self-signed cert for local Windows binding:

```powershell
# Run PowerShell as Administrator
$cert = New-SelfSignedCertificate -DnsName "arty-bot.ngrok.io" -CertStoreLocation "Cert:\LocalMachine\My"
$thumbprint = $cert.Thumbprint
Write-Host "Certificate Thumbprint: $thumbprint"
```

#### Option B: Use a Real Certificate (Production)

Get a wildcard certificate for your domain from a CA and install it in the Windows certificate store.

### Step 4: Configure the Bot

1. **Copy environment template**:
   ```bash
   cp .env.template .env
   ```

2. **Edit `.env`** with your values:
   ```env
   BOT_APP_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
   BOT_APP_SECRET=your_secret_here
   BOT_NAME=Arty
   NGROK_DOMAIN=arty-bot.ngrok.io
   NGROK_TCP_PORT=12345
   CERTIFICATE_THUMBPRINT=your_thumbprint_here
   PYTHON_BACKEND_URL=http://localhost:8000
   ```

3. **Update `appsettings.json`**:
   ```json
   {
     "BotConfiguration": {
       "AadAppId": "YOUR_BOT_APP_ID",
       "AadAppSecret": "YOUR_BOT_APP_SECRET",
       "ServiceDnsName": "arty-bot.ngrok.io",
       "CertificateThumbprint": "YOUR_THUMBPRINT"
     }
   }
   ```

### Step 5: Run the Bot

1. **Restore packages**:
   ```bash
   dotnet restore
   ```

2. **Build**:
   ```bash
   dotnet build
   ```

3. **Run** (as Administrator - required for media ports):
   ```bash
   dotnet run
   ```

4. **Verify it's running**:
   - Open browser: `http://localhost:9441/swagger`
   - You should see the API documentation

## 🎮 Using the Bot

### From Python FastAPI

```python
import requests

# Join a meeting
response = requests.post(
    "http://localhost:9441/api/meeting/join",
    json={
        "joinUrl": "https://teams.microsoft.com/l/meetup-join/...",
        "displayName": None  # Don't set this to get unmixed audio
    }
)
call_data = response.json()
call_id = call_data["callId"]

# Check active calls
response = requests.get("http://localhost:9441/api/meeting/active")
print(response.json())

# Leave meeting
requests.post(
    "http://localhost:9441/api/meeting/leave",
    json={"callId": call_id}
)

# Get audio files
response = requests.get("http://localhost:9441/api/audio/files")
audio_files = response.json()
print(f"Captured {len(audio_files)} audio files")
```

### From Postman

1. **Join Meeting**:
   ```
   POST http://localhost:9441/api/meeting/join
   Content-Type: application/json
   
   {
     "joinUrl": "https://teams.microsoft.com/l/meetup-join/..."
   }
   ```

2. **Get Active Calls**:
   ```
   GET http://localhost:9441/api/meeting/active
   ```

3. **Get Audio Files**:
   ```
   GET http://localhost:9441/api/audio/files
   ```

4. **Download Audio**:
   ```
   GET http://localhost:9441/api/audio/download?filename=20241104_123456_Speaker_abc12345.wav
   ```

## 📂 Audio Output

Audio files are saved to:
```
C:\Users\<YourUser>\AppData\Local\AudioCapture\
```

File format:
- **Codec**: PCM
- **Sample Rate**: 16kHz
- **Bits**: 16-bit
- **Channels**: Mono
- **Naming**: `YYYYMMDD_HHMMSS_<SpeakerName>_<SpeakerId>.wav`

## 🔔 Webhooks to Python Backend

The bot sends webhooks to your FastAPI backend:

### Status Webhook
```
POST http://localhost:8000/api/arty/status

{
  "callId": "abc123",
  "status": "joined",
  "message": "Bot successfully joined meeting",
  "timestamp": "2024-11-04T12:34:56Z"
}
```

### Transcription Webhook (Future)
```
POST http://localhost:8000/api/arty/transcription

{
  "callId": "abc123",
  "audioFilePath": "C:\\...\\audio.wav",
  "timestamp": "2024-11-04T12:34:56Z",
  "speakerId": "user123",
  "speakerName": "John Doe",
  "durationMs": 5000
}
```

## 🐛 Debugging

### Check Bot is Running
```bash
curl http://localhost:9441/api/meeting/health
```

### Check ngrok Tunnels
```bash
curl http://127.0.0.1:4040/api/tunnels
```

### View Logs
The bot logs everything to console. Look for:
- ✅ `Arty Voice Bot initialized successfully`
- 📞 `Received request to join meeting`
- 🎤 `Received Audio: Length=...`

### Common Issues

1. **"Bot service not initialized"**
   - Check certificate thumbprint is correct
   - Verify ngrok is running
   - Check App ID/Secret are correct

2. **"Cannot join meeting"**
   - Verify bot has API permissions
   - Check admin consent was granted
   - Ensure meeting URL is valid

3. **"No audio received"**
   - Don't set `DisplayName` when joining (prevents unmixed audio)
   - Check you're not running as guest
   - Verify media ports (8445) are accessible

4. **Port already in use**
   - Change ports in `appsettings.json` and `launchSettings.json`
   - Update ngrok config to match

## 📊 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/meeting/join` | POST | Join a Teams meeting |
| `/api/meeting/leave` | POST | Leave a meeting |
| `/api/meeting/active` | GET | List active calls |
| `/api/meeting/health` | GET | Health check |
| `/api/audio/files` | GET | List captured audio files |
| `/api/audio/download` | GET | Download an audio file |
| `/api/callback/calling` | POST | Graph Communications callbacks |

## 🔗 Integration with Your Python Backend

1. **Call the bot from Python**:
   ```python
   # In your FastAPI app
   import httpx
   
   async def join_meeting(meeting_url: str):
       async with httpx.AsyncClient() as client:
           response = await client.post(
               "http://localhost:9441/api/meeting/join",
               json={"joinUrl": meeting_url}
           )
           return response.json()
   ```

2. **Receive webhooks from bot**:
   ```python
   # In your FastAPI app
   from fastapi import FastAPI, BackgroundTasks
   
   @app.post("/api/arty/status")
   async def receive_status(webhook: StatusWebhook):
       print(f"Bot status: {webhook.status}")
       # Process the status update
       return {"received": True}
   
   @app.post("/api/arty/transcription")
   async def receive_transcription(webhook: TranscriptionWebhook, bg: BackgroundTasks):
       # Send audio file to Azure Speech-to-Text or Whisper
       bg.add_task(transcribe_audio, webhook.audioFilePath)
       return {"received": True}
   ```

## 🎯 Next Steps (POC Improvements)

1. **Test it**: Join a real Teams meeting and verify audio is captured
2. **Add Transcription**: Send WAV files to Azure Speech-to-Text
3. **Speaker Diarization**: Map speaker IDs to participant names
4. **Real-time Processing**: Stream audio to transcription service
5. **Wake Word Detection**: Listen for "Arty" before processing

## 📝 Notes

- **DisplayName caveat**: If you set `DisplayName` when joining, the bot joins as a guest and **cannot** access unmixed audio (individual speakers). Leave it empty to join as application.
  
- **Performance**: The bot can handle one meeting at a time in this POC. For multiple meetings, you'd need to scale the architecture.

- **Storage**: Audio files can get large. Consider auto-cleanup or cloud storage integration.

## 🤝 Support

Issues? Check:
1. [Microsoft Graph Communications Docs](https://learn.microsoft.com/en-us/graph/api/resources/communications-api-overview)
2. [Teams Bot Calling Requirements](https://learn.microsoft.com/en-us/microsoftteams/platform/bots/calls-and-meetings/requirements-considerations-application-hosted-media-bots)
3. The original `teams-recording-bot` sample in `AksSamples(Deprecated)/`

---

**Built for Arty ARB Assistant** 🤖  
POC for voice-enabled Teams meeting participation

