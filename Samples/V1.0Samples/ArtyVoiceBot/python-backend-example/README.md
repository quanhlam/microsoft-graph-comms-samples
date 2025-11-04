# Arty Voice Bot - Python Backend Example

Simple FastAPI backend to receive webhooks from ArtyVoiceBot.

## Quick Start

### 1. Install Dependencies

```bash
# Create virtual environment (optional but recommended)
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate
# On Mac/Linux:
source venv/bin/activate

# Install requirements
pip install -r requirements.txt
```

### 2. Run the Server

```bash
# Option 1: Using uvicorn directly
uvicorn main:app --reload --port 8000

# Option 2: Run the Python file
python main.py
```

### 3. Test It

Open your browser to: `http://localhost:8000/docs`

You'll see the interactive API documentation (Swagger UI).

## Endpoints

### Receive Status Updates
```
POST http://localhost:8000/api/arty/status
```

Called when:
- Bot joins a meeting
- Bot leaves a meeting
- Call state changes
- Errors occur

### Receive Transcription Notifications
```
POST http://localhost:8000/api/arty/transcription
```

Called when:
- Audio file is captured and ready for transcription

### View All Events
```
GET http://localhost:8000/api/arty/events
GET http://localhost:8000/api/arty/events?call_id=abc123
```

View all received webhooks (for debugging).

### Clear Events
```
DELETE http://localhost:8000/api/arty/events
```

Clear all stored events.

## Testing

### Test from Command Line

```bash
# Test status webhook
curl -X POST http://localhost:8000/api/arty/status \
  -H "Content-Type: application/json" \
  -d '{
    "callId": "test-123",
    "status": "joined",
    "message": "Test message",
    "timestamp": "2024-11-04T12:00:00Z"
  }'

# View events
curl http://localhost:8000/api/arty/events
```

### Test from PowerShell

```powershell
# Test status webhook
Invoke-RestMethod -Uri "http://localhost:8000/api/arty/status" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    callId = "test-123"
    status = "joined"
    message = "Test message"
    timestamp = (Get-Date).ToUniversalTime().ToString("o")
  } | ConvertTo-Json)

# View events
Invoke-RestMethod -Uri "http://localhost:8000/api/arty/events"
```

## Integration with ArtyVoiceBot

Once this is running on port 8000, ArtyVoiceBot will automatically send webhooks here (configured in `appsettings.json`):

```json
{
  "PythonBackend": {
    "BaseUrl": "http://localhost:8000",
    "TranscriptionWebhookPath": "/api/arty/status",
    "StatusWebhookPath": "/api/arty/transcription"
  }
}
```

## What You'll See

When ArtyVoiceBot joins a meeting, you'll see logs like:

```
INFO: 📞 Status Update - Call: abc-123-xyz, Status: joined
INFO:    Message: Bot successfully joined meeting
INFO: ✅ Bot successfully joined meeting abc-123-xyz
```

When audio is captured:

```
INFO: 🎤 Audio Captured - Call: abc-123-xyz
INFO:    Speaker: Speaker (speaker-id-123)
INFO:    File: C:\Users\...\AudioCapture\20241104_120000_Speaker_abc.wav
INFO:    Duration: 5000ms
```

## Next Steps

1. **Add Database**: Store events in a real database (PostgreSQL, MongoDB, etc.)
2. **Add Transcription**: Integrate Azure Speech-to-Text or OpenAI Whisper
3. **Add Voice Commands**: Process transcriptions for "Arty, ..." commands
4. **Add Authentication**: Secure your endpoints with API keys or OAuth
5. **Connect to Your Workflows**: Route commands to your LangGraph agents

## Example: Adding Transcription

```python
import httpx
from azure.cognitiveservices.speech import SpeechConfig, AudioDataStream

@app.post("/api/arty/transcription")
async def receive_transcription(webhook: TranscriptionWebhook):
    logger.info(f"🎤 Audio Captured - {webhook.audioFilePath}")
    
    # Download the audio file from ArtyVoiceBot
    async with httpx.AsyncClient() as client:
        filename = webhook.audioFilePath.split('\\')[-1]
        response = await client.get(
            f"http://localhost:9441/api/audio/download?filename={filename}"
        )
        audio_bytes = response.content
    
    # Send to Azure Speech-to-Text
    transcription = await transcribe_with_azure(audio_bytes)
    
    # Process voice command
    if "arty" in transcription.lower():
        await process_voice_command(transcription, webhook.callId)
    
    return {"received": True, "transcription": transcription}
```

## Troubleshooting

**Port already in use?**
```bash
# Change the port
uvicorn main:app --reload --port 8001
```

Then update `appsettings.json`:
```json
{
  "PythonBackend": {
    "BaseUrl": "http://localhost:8001"
  }
}
```

**Not receiving webhooks?**
1. Check this Python app is running
2. Check ArtyVoiceBot is running
3. Check the logs in both applications
4. Test the endpoint manually with curl

---

**Built for Arty ARB Assistant** 🤖

