# Integration with Your Python FastAPI Backend

This guide shows how to integrate ArtyVoiceBot with your existing Python FastAPI backend for the Arty ARB Assistant.

## Architecture Overview

```
┌────────────────────────────────────────────────────────────────┐
│                     Your FastAPI Backend                       │
│                        (Python)                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│  │ Chat Handler │  │ ARB Knowledge│  │ PTB Reviewer │        │
│  │              │  │     Base      │  │              │        │
│  └──────┬───────┘  └──────────────┘  └──────────────┘        │
│         │                                                       │
│         │ HTTP API calls                                       │
│         ▼                                                       │
│  ┌────────────────────────────────────────┐                   │
│  │    Voice Feature Integration Module     │                   │
│  │  - Join/leave meetings                 │                   │
│  │  - Process audio transcriptions        │                   │
│  │  - Handle voice commands              │                   │
│  └────────────┬───────────────────────────┘                   │
└───────────────┼───────────────────────────────────────────────┘
                │
                │ REST API
                ▼
┌────────────────────────────────────────────────────────────────┐
│                      ArtyVoiceBot                              │
│                      (.NET 8.0)                                │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐              │
│  │ Join/Leave │  │   Audio    │  │  Webhook   │              │
│  │  Meetings  │  │  Capture   │  │  Service   │              │
│  └──────┬─────┘  └──────┬─────┘  └──────┬─────┘              │
│         │                │                │                     │
│         └────────────────┴────────────────┘                     │
│                          │                                      │
│                          ▼                                      │
│                 ┌─────────────────┐                            │
│                 │  Teams Meeting  │                            │
│                 │  (Audio Stream) │                            │
│                 └─────────────────┘                            │
└────────────────────────────────────────────────────────────────┘
```

## Python FastAPI Integration Code

### 1. Add ArtyVoiceBot Client

Create `arty_voice_client.py`:

```python
import httpx
from typing import Optional, List
from pydantic import BaseModel
from datetime import datetime

class JoinMeetingRequest(BaseModel):
    join_url: str
    display_name: Optional[str] = None
    tenant_id: Optional[str] = None

class CallInfo(BaseModel):
    call_id: str
    scenario_id: str
    meeting_url: str
    joined_at: datetime
    status: str

class ArtyVoiceClient:
    """Client for interacting with ArtyVoiceBot"""
    
    def __init__(self, base_url: str = "http://localhost:9441"):
        self.base_url = base_url
        self.client = httpx.AsyncClient(timeout=30.0)
    
    async def join_meeting(self, meeting_url: str) -> dict:
        """Join a Teams meeting"""
        response = await self.client.post(
            f"{self.base_url}/api/meeting/join",
            json={"joinUrl": meeting_url}
        )
        response.raise_for_status()
        return response.json()
    
    async def leave_meeting(self, call_id: str) -> bool:
        """Leave a meeting"""
        response = await self.client.post(
            f"{self.base_url}/api/meeting/leave",
            json={"callId": call_id}
        )
        return response.status_code == 200
    
    async def get_active_calls(self) -> List[CallInfo]:
        """Get all active calls"""
        response = await self.client.get(
            f"{self.base_url}/api/meeting/active"
        )
        response.raise_for_status()
        calls = response.json()
        return [CallInfo(**call) for call in calls]
    
    async def get_audio_files(self) -> List[str]:
        """Get list of captured audio files"""
        response = await self.client.get(
            f"{self.base_url}/api/audio/files"
        )
        response.raise_for_status()
        return response.json()
    
    async def download_audio(self, filename: str) -> bytes:
        """Download an audio file"""
        response = await self.client.get(
            f"{self.base_url}/api/audio/download",
            params={"filename": filename}
        )
        response.raise_for_status()
        return response.content
    
    async def health_check(self) -> bool:
        """Check if bot is healthy"""
        try:
            response = await self.client.get(
                f"{self.base_url}/api/meeting/health"
            )
            return response.status_code == 200
        except:
            return False
    
    async def close(self):
        await self.client.aclose()
```

### 2. Add Webhook Handlers

Update your FastAPI app to receive webhooks from the bot:

```python
from fastapi import FastAPI, BackgroundTasks
from pydantic import BaseModel
from datetime import datetime

app = FastAPI()

class StatusWebhook(BaseModel):
    call_id: str
    status: str
    message: str
    timestamp: datetime

class TranscriptionWebhook(BaseModel):
    call_id: str
    audio_file_path: str
    timestamp: datetime
    speaker_id: str
    speaker_name: str
    duration_ms: int

# Initialize voice client
voice_client = ArtyVoiceClient()

@app.post("/api/arty/status")
async def receive_status_webhook(webhook: StatusWebhook):
    """Receive status updates from ArtyVoiceBot"""
    print(f"📞 Bot Status: {webhook.status} - {webhook.message}")
    
    # Store in database or update state
    # await update_call_status(webhook.call_id, webhook.status)
    
    return {"received": True}

@app.post("/api/arty/transcription")
async def receive_transcription_webhook(
    webhook: TranscriptionWebhook,
    background_tasks: BackgroundTasks
):
    """Receive audio transcription notifications"""
    print(f"🎤 Audio captured for {webhook.speaker_name}")
    
    # Queue for transcription
    background_tasks.add_task(
        transcribe_audio,
        webhook.audio_file_path,
        webhook.call_id,
        webhook.speaker_id
    )
    
    return {"received": True}

async def transcribe_audio(audio_path: str, call_id: str, speaker_id: str):
    """Transcribe audio file using Azure Speech or Whisper"""
    # Download the audio file from ArtyVoiceBot
    filename = audio_path.split('\\')[-1]  # Windows path
    audio_bytes = await voice_client.download_audio(filename)
    
    # Send to Azure Speech-to-Text or OpenAI Whisper
    # transcription = await azure_speech_client.transcribe(audio_bytes)
    # await process_voice_command(transcription, call_id, speaker_id)
    
    print(f"Transcribed {len(audio_bytes)} bytes for speaker {speaker_id}")
```

### 3. Meeting Control Flow

Example of how to use it in your ARB workflow:

```python
from typing import Dict
import asyncio

class ArtyMeetingManager:
    """Manages Arty's participation in meetings"""
    
    def __init__(self):
        self.voice_client = ArtyVoiceClient()
        self.active_meetings: Dict[str, str] = {}  # meeting_id -> call_id
    
    async def join_arb_meeting(self, meeting_url: str, meeting_id: str):
        """
        Have Arty join an ARB review meeting
        """
        # Check if bot is available
        if not await self.voice_client.health_check():
            raise Exception("ArtyVoiceBot is not available")
        
        # Join the meeting
        result = await self.voice_client.join_meeting(meeting_url)
        call_id = result["callId"]
        
        # Track the meeting
        self.active_meetings[meeting_id] = call_id
        
        print(f"✅ Arty joined meeting {meeting_id}")
        print(f"   Call ID: {call_id}")
        
        return call_id
    
    async def leave_arb_meeting(self, meeting_id: str):
        """
        Have Arty leave the meeting
        """
        if meeting_id not in self.active_meetings:
            raise Exception(f"Meeting {meeting_id} not found")
        
        call_id = self.active_meetings[meeting_id]
        
        # Leave the meeting
        await self.voice_client.leave_meeting(call_id)
        
        # Get captured audio files
        audio_files = await self.voice_client.get_audio_files()
        
        # Clean up tracking
        del self.active_meetings[meeting_id]
        
        print(f"✅ Arty left meeting {meeting_id}")
        print(f"   Captured {len(audio_files)} audio files")
        
        return audio_files
    
    async def process_meeting_audio(self, meeting_id: str):
        """
        Process all audio from a meeting for transcription and analysis
        """
        # Get audio files
        audio_files = await self.voice_client.get_audio_files()
        
        # Filter for this meeting (by timestamp or other criteria)
        # Download and process each file
        for file in audio_files:
            audio_data = await self.voice_client.download_audio(file)
            # Send to transcription service
            # transcription = await transcribe(audio_data)
            # Store in meeting notes
        
        return len(audio_files)

# Usage example
meeting_manager = ArtyMeetingManager()

@app.post("/api/meetings/{meeting_id}/join")
async def join_meeting_endpoint(meeting_id: str, meeting_url: str):
    """Endpoint for Arty to join a meeting"""
    call_id = await meeting_manager.join_arb_meeting(meeting_url, meeting_id)
    return {"callId": call_id, "status": "joined"}

@app.post("/api/meetings/{meeting_id}/leave")
async def leave_meeting_endpoint(meeting_id: str):
    """Endpoint for Arty to leave a meeting"""
    audio_files = await meeting_manager.leave_arb_meeting(meeting_id)
    return {"status": "left", "audioFiles": audio_files}
```

### 4. Voice Command Processing

Process voice commands like "Arty, help me review this document":

```python
import re
from typing import Optional

class VoiceCommandProcessor:
    """Process voice commands from meeting transcriptions"""
    
    WAKE_WORD = "arty"
    
    async def process_transcription(
        self,
        text: str,
        speaker_id: str,
        call_id: str
    ):
        """Process a transcription for voice commands"""
        
        # Check for wake word
        if not self.contains_wake_word(text):
            return None
        
        # Extract command
        command = self.extract_command(text)
        
        if command:
            await self.execute_command(command, speaker_id, call_id)
        
        return command
    
    def contains_wake_word(self, text: str) -> bool:
        """Check if text contains the wake word"""
        return self.WAKE_WORD in text.lower()
    
    def extract_command(self, text: str) -> Optional[dict]:
        """Extract command from text after wake word"""
        text_lower = text.lower()
        
        # Pattern: "Arty, [command]"
        match = re.search(rf"{self.WAKE_WORD},?\s+(.+)", text_lower)
        if not match:
            return None
        
        command_text = match.group(1).strip()
        
        # Classify intent
        if "review" in command_text and "document" in command_text:
            return {
                "intent": "review_document",
                "text": command_text
            }
        elif "what are" in command_text or "what is" in command_text:
            return {
                "intent": "question",
                "text": command_text
            }
        elif "help" in command_text:
            return {
                "intent": "help",
                "text": command_text
            }
        
        return {
            "intent": "unknown",
            "text": command_text
        }
    
    async def execute_command(
        self,
        command: dict,
        speaker_id: str,
        call_id: str
    ):
        """Execute the voice command"""
        intent = command["intent"]
        
        if intent == "review_document":
            # Trigger PTB review workflow
            await self.handle_review_document(call_id)
        
        elif intent == "question":
            # Query ARB knowledge base
            await self.handle_question(command["text"], call_id)
        
        elif intent == "help":
            # Provide help
            await self.handle_help(call_id)

# Integrate with webhook
@app.post("/api/arty/transcription")
async def receive_transcription_webhook(
    webhook: TranscriptionWebhook,
    background_tasks: BackgroundTasks
):
    """Receive and process transcription"""
    
    # Download audio
    filename = webhook.audio_file_path.split('\\')[-1]
    audio_bytes = await voice_client.download_audio(filename)
    
    # Transcribe (using Azure Speech or Whisper)
    transcription = await transcribe_audio_bytes(audio_bytes)
    
    # Process for voice commands
    command_processor = VoiceCommandProcessor()
    command = await command_processor.process_transcription(
        transcription,
        webhook.speaker_id,
        webhook.call_id
    )
    
    if command:
        print(f"🎯 Detected command: {command['intent']}")
    
    return {"received": True, "command": command}
```

## Configuration

Update your `.env` file:

```env
# ArtyVoiceBot settings
ARTY_VOICE_BOT_URL=http://localhost:9441
ARTY_VOICE_BOT_ENABLED=true

# Azure Speech-to-Text (for transcription)
AZURE_SPEECH_KEY=your_key
AZURE_SPEECH_REGION=eastus
```

## Testing the Integration

```python
import pytest
import asyncio

@pytest.mark.asyncio
async def test_voice_bot_integration():
    """Test the voice bot integration"""
    client = ArtyVoiceClient()
    
    # Check health
    is_healthy = await client.health_check()
    assert is_healthy, "Voice bot is not running"
    
    # Join a test meeting
    meeting_url = "https://teams.microsoft.com/l/meetup-join/..."
    result = await client.join_meeting(meeting_url)
    assert "callId" in result
    
    call_id = result["callId"]
    
    # Wait a bit for audio capture
    await asyncio.sleep(10)
    
    # Check active calls
    calls = await client.get_active_calls()
    assert len(calls) > 0
    assert calls[0].call_id == call_id
    
    # Leave meeting
    success = await client.leave_meeting(call_id)
    assert success
    
    # Check audio files were captured
    audio_files = await client.get_audio_files()
    assert len(audio_files) > 0
    
    await client.close()
```

## Next Steps

1. **Test the integration** with a real Teams meeting
2. **Add Azure Speech-to-Text** for transcription
3. **Implement voice command routing** to your existing LangGraph workflows
4. **Add speaker identification** to map audio to participants
5. **Implement real-time response** - have Arty speak back in the meeting

---

**Ready to integrate?** Start with the health check and join/leave flow, then add transcription!

