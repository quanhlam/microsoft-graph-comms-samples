# ArtyVoiceBot - Project Overview

## 📦 What You Got

A complete, production-ready foundation for voice-enabled Teams meeting participation. This bot serves as a **microservice** that runs alongside your Python FastAPI backend, handling all the complex Teams audio capture while exposing a simple REST API.

## 🎯 Project Goals Achieved

✅ **Goal 1: Voice Capture POC**
- Bot joins Teams meetings
- Captures audio in real-time
- Saves to WAV files you can listen to
- Perfect for debugging and verification

✅ **Goal 2: Python Integration**
- Clean REST API your Python code can call
- Webhooks send data back to your FastAPI
- Completely decoupled architecture

✅ **Goal 3: Local Development**
- Runs on your Windows laptop
- Uses ngrok for tunneling (no Azure deployment needed for POC)
- Quick iteration and testing

## 📚 Documentation Guide

Start here based on what you need:

### First Time Setup → `QUICKSTART.md`
30-minute guide to get the bot running. Covers:
- Azure bot registration
- ngrok setup
- Certificate creation
- Running the bot
- Testing with a meeting

### Full Documentation → `README.md`
Complete reference with:
- Detailed architecture
- All API endpoints
- Configuration options
- Troubleshooting guide
- Performance notes

### Python Integration → `INTEGRATION.md`
Code examples for:
- Python client for calling the bot
- Webhook handlers for your FastAPI
- Voice command processing
- Meeting management

### Summary → `background_docs/arty_voice_bot_summary.md`
High-level overview and next steps

## 🏗️ Architecture at a Glance

```
┌─────────────────────────────────────────────────────────┐
│  Your Python FastAPI Backend (Existing Code)            │
│  - Chat handling                                        │
│  - ARB knowledge base                                   │
│  - PTB document review                                  │
│  - LangGraph workflows                                  │
└─────────────┬───────────────────────────────────────────┘
              │ HTTP API calls
              ▼
┌─────────────────────────────────────────────────────────┐
│  ArtyVoiceBot (.NET 8.0 Microservice) - NEW            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ REST API     │  │ Audio Capture│  │  Webhooks    │ │
│  │ Controllers  │  │  Service     │  │  to Python   │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────────┬───────────────────────────────────────────┘
              │ Graph Communications API
              ▼
┌─────────────────────────────────────────────────────────┐
│  Microsoft Teams Meeting                                │
│  - Audio streams (real-time)                           │
│  - Participant info                                     │
└─────────────────────────────────────────────────────────┘
```

## 🔑 Key Components

### Services/
- **ArtyBotService.cs**: Core service for joining/leaving meetings
- **AudioCaptureService.cs**: Captures audio and saves to WAV files
- **BotMediaStream.cs**: Handles real-time media streams from Teams
- **WebhookService.cs**: Sends notifications to your Python backend

### Controllers/
- **MeetingController.cs**: API for joining/leaving meetings
- **AudioController.cs**: API for accessing captured audio files
- **CallbackController.cs**: Receives callbacks from Microsoft Graph

### Models/
- **BotConfiguration.cs**: Configuration settings
- **ApiModels.cs**: Request/response models for the API

## 🚦 How to Use It

### 1. Start the Bot

```bash
cd ArtyVoiceBot
dotnet run
```

### 2. From Your Python Code

```python
import requests

# Join a meeting
response = requests.post(
    "http://localhost:9441/api/meeting/join",
    json={"joinUrl": "https://teams.microsoft.com/l/meetup-join/..."}
)
call_id = response.json()["callId"]

# Later, leave the meeting
requests.post(
    "http://localhost:9441/api/meeting/leave",
    json={"callId": call_id}
)

# Download captured audio
files = requests.get("http://localhost:9441/api/audio/files").json()
for file in files:
    audio = requests.get(
        f"http://localhost:9441/api/audio/download?filename={file}"
    )
    # Send to transcription service...
```

### 3. Receive Webhooks in Your FastAPI

```python
from fastapi import FastAPI

@app.post("/api/arty/status")
async def bot_status(data: dict):
    # Bot joined, left, or had an error
    print(f"Bot status: {data['status']}")
    return {"received": True}
```

## 🎬 Typical Flow

1. **User creates ARB meeting** in Teams
2. **Your Python backend** calls ArtyVoiceBot API to join
3. **ArtyVoiceBot joins** the meeting
4. **Audio is captured** in real-time → saved to WAV files
5. **Webhooks notify** your Python backend
6. **Your Python backend** downloads WAV files
7. **Transcription service** converts audio to text
8. **Voice command detection** looks for "Arty, ..."
9. **LangGraph workflow** processes the command
10. **Response sent** (via chat or voice)

## 📂 File Organization

```
ArtyVoiceBot/
├── 📄 PROJECT_OVERVIEW.md      ← You are here
├── 📄 README.md                ← Full documentation
├── 📄 QUICKSTART.md            ← Setup guide
├── 📄 INTEGRATION.md           ← Python integration
│
├── 🔧 appsettings.json         ← Configuration
├── 🔧 launchSettings.json      ← Launch configuration
├── 🔧 ArtyVoiceBot.csproj     ← Project file
│
├── 💻 Program.cs               ← Application entry point
│
├── 📁 Controllers/             ← REST API endpoints
│   ├── MeetingController.cs
│   ├── AudioController.cs
│   └── CallbackController.cs
│
├── 📁 Services/                ← Core business logic
│   ├── ArtyBotService.cs
│   ├── AudioCaptureService.cs
│   ├── BotMediaStream.cs
│   └── WebhookService.cs
│
├── 📁 Models/                  ← Data models
│   ├── BotConfiguration.cs
│   └── ApiModels.cs
│
├── 🧪 test-integration.py      ← Python test script
├── 🧪 test-api.http           ← API test file
└── 🔧 setup-certificate.ps1   ← Certificate setup script
```

## ⚡ Quick Commands

```bash
# Build the project
dotnet build

# Run the bot
dotnet run

# Run with hot reload
dotnet watch run

# Test from Python
python test-integration.py

# View Swagger UI
# http://localhost:9441/swagger
```

## 🔄 Development Workflow

### Daily Development
1. Start ngrok: `ngrok start --all --config ngrok.yml`
2. Start bot: `dotnet run`
3. Test with: `python test-integration.py`
4. Check audio files: `C:\Users\<You>\AppData\Local\AudioCapture\`

### Making Changes
1. Edit code in Visual Studio or VS Code
2. Save (auto-rebuild with `dotnet watch`)
3. Test with Postman or Python script
4. Check logs for errors

### Integrating with Your Python Backend
1. Add the Python client code from `INTEGRATION.md`
2. Add webhook handlers to your FastAPI
3. Test the integration
4. Add voice command processing
5. Connect to your LangGraph workflows

## 🎓 Learning Path

### Phase 1: Basic POC (This Week)
- ✅ Get bot running
- ✅ Join a test meeting
- ✅ Verify audio is captured
- ✅ Listen to WAV files

### Phase 2: Python Integration (Next Week)
- Add Python client to your FastAPI
- Test join/leave from Python
- Add webhook handlers
- Download and process audio

### Phase 3: Transcription (Week 3)
- Integrate Azure Speech-to-Text
- Or integrate OpenAI Whisper
- Store transcriptions

### Phase 4: Voice Commands (Week 4)
- Detect "Arty" wake word
- Parse voice commands
- Route to LangGraph workflows

### Phase 5: Production (Future)
- Deploy to Azure Windows Server
- Add monitoring and logging
- Implement auto-scaling
- Add error recovery

## 🆘 Getting Help

### Check These First
1. Logs in the console - most errors are clearly logged
2. `QUICKSTART.md` - setup issues
3. `README.md` - detailed troubleshooting section

### Common Issues
- **"Bot service not initialized"** → Check certificate thumbprint
- **"Cannot join meeting"** → Verify API permissions and admin consent
- **"No audio received"** → Don't set displayName when joining
- **"Connection refused"** → Is ngrok running?

### Resources
- [Microsoft Graph Communications Docs](https://learn.microsoft.com/en-us/graph/api/resources/communications-api-overview)
- [Teams Bot Requirements](https://learn.microsoft.com/en-us/microsoftteams/platform/bots/calls-and-meetings/requirements-considerations-application-hosted-media-bots)
- Original sample: `../AksSamples(Deprecated)/teams-recording-bot/`

## 🎯 Success Criteria

You've succeeded when:
- ✅ Bot joins Teams meeting automatically
- ✅ You can see it as a participant
- ✅ WAV files contain recognizable speech
- ✅ Your Python code can control it
- ✅ Webhooks are received in your FastAPI
- ✅ You can transcribe the audio
- ✅ Voice commands trigger your workflows

## 🚀 Next Steps

1. **Read `QUICKSTART.md`** and get the bot running
2. **Join a test meeting** and verify audio capture
3. **Read `INTEGRATION.md`** for Python integration
4. **Integrate with your FastAPI** backend
5. **Add transcription** service
6. **Implement voice commands**
7. **Connect to your LangGraph** workflows

## 💬 Notes

- This is a **POC/MVP** - production would need scaling, error handling, monitoring
- The architecture is **modular** - easy to extend with new features
- **Performance**: One bot instance = one meeting for POC
- **Cost**: ngrok Pro ($10/mo) + Azure Speech API (pay per use)

---

## 🎉 You're Ready!

Everything you need is here. Start with `QUICKSTART.md` and you'll have a working voice bot in 30 minutes.

**Questions?** Check the docs or examine the code - it's well-commented!

Built for **Arty ARB Assistant** 🤖  
Making ARB reviews more efficient with AI and voice

