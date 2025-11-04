#!/usr/bin/env python3
"""
Simple test script to verify ArtyVoiceBot integration
Run this to test the bot from Python
"""

import requests
import time
import sys

VOICE_BOT_URL = "http://localhost:9441"

def test_health():
    """Test if bot is running"""
    print("🔍 Testing bot health...")
    try:
        response = requests.get(f"{VOICE_BOT_URL}/api/meeting/health", timeout=5)
        if response.status_code == 200:
            print("✅ Bot is healthy!")
            return True
        else:
            print(f"❌ Bot returned status {response.status_code}")
            return False
    except requests.exceptions.ConnectionError:
        print("❌ Cannot connect to bot. Is it running?")
        return False
    except Exception as e:
        print(f"❌ Error: {e}")
        return False

def join_meeting(meeting_url: str):
    """Test joining a meeting"""
    print(f"\n📞 Joining meeting...")
    print(f"   URL: {meeting_url[:50]}...")
    
    try:
        response = requests.post(
            f"{VOICE_BOT_URL}/api/meeting/join",
            json={"joinUrl": meeting_url},
            timeout=30
        )
        
        if response.status_code == 200:
            data = response.json()
            print(f"✅ Successfully joined!")
            print(f"   Call ID: {data['callId']}")
            print(f"   Scenario ID: {data['scenarioId']}")
            print(f"   Status: {data['status']}")
            return data['callId']
        else:
            print(f"❌ Failed to join: {response.status_code}")
            print(f"   {response.text}")
            return None
    except Exception as e:
        print(f"❌ Error: {e}")
        return None

def get_active_calls():
    """Get active calls"""
    print("\n📋 Getting active calls...")
    try:
        response = requests.get(f"{VOICE_BOT_URL}/api/meeting/active", timeout=5)
        if response.status_code == 200:
            calls = response.json()
            print(f"✅ Found {len(calls)} active call(s)")
            for call in calls:
                print(f"   - Call ID: {call['callId']}")
                print(f"     Status: {call['status']}")
                print(f"     Joined: {call['joinedAt']}")
            return calls
        else:
            print(f"❌ Failed: {response.status_code}")
            return []
    except Exception as e:
        print(f"❌ Error: {e}")
        return []

def leave_meeting(call_id: str):
    """Leave a meeting"""
    print(f"\n👋 Leaving meeting...")
    print(f"   Call ID: {call_id}")
    
    try:
        response = requests.post(
            f"{VOICE_BOT_URL}/api/meeting/leave",
            json={"callId": call_id},
            timeout=10
        )
        
        if response.status_code == 200:
            print("✅ Successfully left meeting!")
            return True
        else:
            print(f"❌ Failed to leave: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Error: {e}")
        return False

def get_audio_files():
    """Get captured audio files"""
    print("\n🎵 Getting audio files...")
    try:
        response = requests.get(f"{VOICE_BOT_URL}/api/audio/files", timeout=5)
        if response.status_code == 200:
            files = response.json()
            print(f"✅ Found {len(files)} audio file(s)")
            for file in files[:5]:  # Show first 5
                import os
                filename = os.path.basename(file)
                print(f"   - {filename}")
            if len(files) > 5:
                print(f"   ... and {len(files) - 5} more")
            return files
        else:
            print(f"❌ Failed: {response.status_code}")
            return []
    except Exception as e:
        print(f"❌ Error: {e}")
        return []

def main():
    print("=" * 60)
    print("ArtyVoiceBot Integration Test")
    print("=" * 60)
    
    # Test health
    if not test_health():
        print("\n⚠️  Bot is not running!")
        print("   Start it with: cd ArtyVoiceBot && dotnet run")
        sys.exit(1)
    
    # Get meeting URL from user
    print("\n" + "=" * 60)
    print("To test meeting join/leave, you need a Teams meeting URL")
    print("=" * 60)
    
    meeting_url = input("\nEnter Teams meeting URL (or press Enter to skip): ").strip()
    
    if not meeting_url:
        print("\n⏭️  Skipping meeting test")
    else:
        # Join meeting
        call_id = join_meeting(meeting_url)
        
        if call_id:
            # Wait a bit
            print("\n⏳ Waiting 10 seconds for audio capture...")
            time.sleep(10)
            
            # Check active calls
            get_active_calls()
            
            # Leave meeting
            leave_meeting(call_id)
    
    # Get audio files
    get_audio_files()
    
    print("\n" + "=" * 60)
    print("✅ Test complete!")
    print("=" * 60)

if __name__ == "__main__":
    main()

