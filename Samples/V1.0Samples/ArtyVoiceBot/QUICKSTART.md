# 🚀 Quick Start Guide

Get Arty Voice Bot running in **30 minutes**!

## Prerequisites Checklist

- [ ] Windows laptop (you have this!)
- [ ] .NET 8.0 SDK installed
- [ ] ngrok Pro account
- [ ] Azure subscription
- [ ] Teams account

## Setup in 6 Steps

### 1️⃣ Register Bot (10 min)

```bash
# Go to Azure Portal
https://portal.azure.com

# Create Azure Bot:
1. Search "Azure Bot"
2. Create new bot
3. Copy App ID and Secret
4. Add these API permissions:
   - Calls.AccessMedia.All
   - Calls.JoinGroupCall.All
   - Calls.JoinGroupCallAsGuest.All
5. Grant admin consent
```

### 2️⃣ Setup ngrok (5 min)

```bash
# Reserve domain at https://dashboard.ngrok.com/endpoints/domains
# Example: arty-bot.ngrok.io

# Reserve TCP at https://dashboard.ngrok.com/endpoints/tcp-addresses
# Example: 12345.tcp.ngrok.io

# Create ngrok.yml:
version: "2"
authtoken: YOUR_AUTH_TOKEN
tunnels:
  signaling:
    proto: http
    addr: 9441
    hostname: arty-bot.ngrok.io
  media:
    proto: tcp
    addr: 8445
    remote_addr: 12345.tcp.ngrok.io

# Start ngrok:
ngrok start --all --config ngrok.yml
```

### 3️⃣ Create Certificate (2 min)

```powershell
# Run PowerShell as Admin:
$cert = New-SelfSignedCertificate -DnsName "arty-bot.ngrok.io" -CertStoreLocation "Cert:\LocalMachine\My"
$thumbprint = $cert.Thumbprint
Write-Host "Thumbprint: $thumbprint"
# Copy the thumbprint!
```

### 4️⃣ Configure Bot (3 min)

Edit `appsettings.json`:

```json
{
  "BotConfiguration": {
    "AadAppId": "YOUR_APP_ID",
    "AadAppSecret": "YOUR_APP_SECRET",
    "ServiceDnsName": "arty-bot.ngrok.io",
    "CertificateThumbprint": "YOUR_THUMBPRINT"
  },
  "PythonBackend": {
    "BaseUrl": "http://localhost:8000"
  }
}
```

### 5️⃣ Build & Run (5 min)

```bash
# In ArtyVoiceBot directory:
dotnet restore
dotnet build
dotnet run

# Should see:
# ✅ Arty Voice Bot initialized successfully
```

### 6️⃣ Test It! (5 min)

```bash
# Option A: Use Postman
POST http://localhost:9441/api/meeting/join
{
  "joinUrl": "https://teams.microsoft.com/l/meetup-join/..."
}

# Option B: Use curl
curl -X POST http://localhost:9441/api/meeting/join \
  -H "Content-Type: application/json" \
  -d '{"joinUrl": "YOUR_TEAMS_MEETING_URL"}'

# Check audio files:
# C:\Users\<You>\AppData\Local\AudioCapture\
```

## ✅ Verification

1. **Bot running?**
   ```bash
   curl http://localhost:9441/api/meeting/health
   # Should return: {"status":"healthy"}
   ```

2. **ngrok working?**
   ```bash
   curl http://127.0.0.1:4040/api/tunnels
   # Should show 2 tunnels
   ```

3. **Bot joined meeting?**
   - You'll see a participant named "Arty" (or your bot name)
   - Logs show: `Successfully initiated join to meeting`

4. **Audio capturing?**
   - Logs show: `Received Audio: Length=...`
   - Files appear in `AudioCapture` folder

## 🐛 Troubleshooting

**"Bot service not initialized"**
- Check certificate thumbprint
- Verify App ID/Secret
- Ensure ngrok is running

**"Cannot join meeting"**
- Grant admin consent for API permissions
- Check meeting URL is valid
- Verify bot is registered in Azure

**"No audio received"**
- Don't set DisplayName (prevents unmixed audio)
- Check meeting has active speakers
- Verify media ports accessible

## 📞 From Your Python Code

```python
import requests

# Join meeting
r = requests.post(
    "http://localhost:9441/api/meeting/join",
    json={"joinUrl": "https://teams.microsoft.com/..."}
)
call = r.json()
print(f"Joined! Call ID: {call['callId']}")

# Wait a bit...

# Leave meeting
requests.post(
    "http://localhost:9441/api/meeting/leave",
    json={"callId": call["callId"]}
)

# Get audio files
r = requests.get("http://localhost:9441/api/audio/files")
files = r.json()
print(f"Captured: {files}")
```

## Next: See Full README

For detailed docs, integration guide, and advanced features, see [README.md](README.md)

---

**Ready? Let's go!** 🚀

