"""
Simple FastAPI backend to receive webhooks from ArtyVoiceBot
Run with: uvicorn main:app --reload --port 8000
"""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from datetime import datetime
from typing import Optional
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="Arty Voice Bot Python Backend",
    description="Receives webhooks from ArtyVoiceBot",
    version="1.0.0"
)

# Pydantic models matching ArtyVoiceBot's webhook data
class StatusWebhook(BaseModel):
    callId: str
    status: str
    message: str
    timestamp: datetime

class TranscriptionWebhook(BaseModel):
    callId: str
    audioFilePath: str
    timestamp: datetime
    speakerId: str
    speakerName: str
    durationMs: int


# Storage for received events (in-memory for POC)
status_events = []
transcription_events = []


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "service": "Arty Voice Bot Python Backend",
        "status": "running",
        "endpoints": {
            "status": "/api/arty/status",
            "transcription": "/api/arty/transcription",
            "events": "/api/arty/events"
        }
    }


@app.post("/api/arty/status")
async def receive_status(webhook: StatusWebhook):
    """
    Receive status updates from ArtyVoiceBot
    
    Called when:
    - Bot joins a meeting
    - Bot leaves a meeting
    - Call state changes
    - Errors occur
    """
    logger.info(f"📞 Status Update - Call: {webhook.callId}, Status: {webhook.status}")
    logger.info(f"   Message: {webhook.message}")
    
    # Store the event
    status_events.append({
        "callId": webhook.callId,
        "status": webhook.status,
        "message": webhook.message,
        "timestamp": webhook.timestamp.isoformat(),
        "received_at": datetime.utcnow().isoformat()
    })
    
    # Here you can add your custom logic:
    # - Store in database
    # - Send notifications
    # - Trigger workflows
    # - etc.
    
    if webhook.status == "joined":
        logger.info(f"✅ Bot successfully joined meeting {webhook.callId}")
        # TODO: Update your database, notify users, etc.
    
    elif webhook.status == "left":
        logger.info(f"👋 Bot left meeting {webhook.callId}")
        # TODO: Finalize meeting records, etc.
    
    elif webhook.status == "error":
        logger.error(f"❌ Error in call {webhook.callId}: {webhook.message}")
        # TODO: Handle errors, alert admins, etc.
    
    return {"received": True, "callId": webhook.callId}


@app.post("/api/arty/transcription")
async def receive_transcription(webhook: TranscriptionWebhook):
    """
    Receive audio transcription notifications from ArtyVoiceBot
    
    Called when:
    - Audio file is captured and ready for transcription
    """
    logger.info(f"🎤 Audio Captured - Call: {webhook.callId}")
    logger.info(f"   Speaker: {webhook.speakerName} ({webhook.speakerId})")
    logger.info(f"   File: {webhook.audioFilePath}")
    logger.info(f"   Duration: {webhook.durationMs}ms")
    
    # Store the event
    transcription_events.append({
        "callId": webhook.callId,
        "speakerId": webhook.speakerId,
        "speakerName": webhook.speakerName,
        "audioFilePath": webhook.audioFilePath,
        "durationMs": webhook.durationMs,
        "timestamp": webhook.timestamp.isoformat(),
        "received_at": datetime.utcnow().isoformat()
    })
    
    # Here you can add your transcription logic:
    # 1. Download the audio file from ArtyVoiceBot
    # 2. Send to Azure Speech-to-Text or OpenAI Whisper
    # 3. Process the transcription
    # 4. Store results in your database
    
    # Example:
    # audio_data = await download_audio_from_bot(webhook.audioFilePath)
    # transcription = await transcribe_audio(audio_data)
    # await process_voice_command(transcription, webhook.callId)
    
    return {
        "received": True,
        "callId": webhook.callId,
        "message": "Audio will be processed"
    }


@app.get("/api/arty/events")
async def get_events(call_id: Optional[str] = None):
    """
    Get all received events (for debugging/testing)
    
    Query params:
    - call_id: Filter events by call ID (optional)
    """
    if call_id:
        filtered_status = [e for e in status_events if e["callId"] == call_id]
        filtered_transcription = [e for e in transcription_events if e["callId"] == call_id]
        
        return {
            "callId": call_id,
            "status_events": filtered_status,
            "transcription_events": filtered_transcription
        }
    
    return {
        "total_status_events": len(status_events),
        "total_transcription_events": len(transcription_events),
        "status_events": status_events[-10:],  # Last 10 status events
        "transcription_events": transcription_events[-10:]  # Last 10 transcription events
    }


@app.delete("/api/arty/events")
async def clear_events():
    """Clear all stored events (for testing)"""
    global status_events, transcription_events
    
    count_status = len(status_events)
    count_transcription = len(transcription_events)
    
    status_events = []
    transcription_events = []
    
    logger.info(f"🗑️ Cleared {count_status} status events and {count_transcription} transcription events")
    
    return {
        "cleared": True,
        "status_events_cleared": count_status,
        "transcription_events_cleared": count_transcription
    }


@app.get("/health")
async def health():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "Arty Voice Bot Python Backend",
        "timestamp": datetime.utcnow().isoformat()
    }


if __name__ == "__main__":
    import uvicorn
    
    print("=" * 60)
    print("🚀 Starting Arty Voice Bot Python Backend")
    print("=" * 60)
    print()
    print("📡 Webhook endpoints:")
    print("   - Status:        POST http://localhost:8001/api/arty/status")
    print("   - Transcription: POST http://localhost:8001/api/arty/transcription")
    print("   - Events:        GET  http://localhost:8001/api/arty/events")
    print()
    print("🌐 Interactive docs: http://localhost:8001/docs")
    print("=" * 60)
    print()
    
    uvicorn.run(app, host="0.0.0.0", port=8001, log_level="info")

