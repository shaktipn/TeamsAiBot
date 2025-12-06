# Teams Media Bot

## What Is This Project?

This is a **Microsoft Teams real-time media bot** built with **.NET 8** that joins Teams meetings, captures live audio streams, transcribes them in real-time using **Deepgram**, and sends transcripts to a **Ktor backend server** for AI-powered live summarization. The bot handles multiple concurrent meetings independently and complies with recording disclosure requirements.

## Technical Overview

### Architecture

The bot implements a **three-tier real-time communication pipeline**:

```
Microsoft Teams ←→ Teams Media Bot ←→ Deepgram (Transcription)
                         ↓
                    Ktor Server (AI Processing)
```

### Core Technologies

1. **Microsoft Graph Communications SDK** (`Microsoft.Graph.Communications.Client`)
   - Handles Teams call signaling and control
   - Manages call lifecycle (answer, reject, terminate)
   - Requires Windows (native audio processing libraries)

2. **Microsoft Skype Bots Media SDK** (`Microsoft.Skype.Bots.Media`)
   - Processes real-time audio streams from Teams meetings
   - Delivers PCM 16kHz audio buffers
   - **Windows-only** - contains native DLLs for real-time media processing

3. **Deepgram SDK** (v6.6.1)
   - Real-time speech-to-text via WebSocket
   - Receives raw audio buffers, returns transcripts (interim and final)
   - Model: `nova-2` with `en-US` language

4. **ASP.NET Core 8**
   - Hosts webhook endpoint for Teams notifications
   - Dependency injection for service management
   - Kestrel web server on port 5000

5. **Ktor Server Integration**
   - HTTP API for session initialization
   - WebSocket for bidirectional real-time communication
   - Receives transcripts, sends AI-generated live summaries

### Project Structure

```
TeamsMediaBot/
├── Controllers/
│   └── BotController.cs          # Webhook endpoint for Teams notifications
├── Services/
│   ├── BotMediaService.cs        # Main orchestrator, Graph Communications Client
│   ├── MeetingSessionManager.cs  # Individual meeting session lifecycle
│   ├── KtorService.cs            # Ktor HTTP & WebSocket communication
│   └── DeepgramService.cs        # Real-time transcription handling
├── Models/
│   └── KtorModels.cs             # Type-safe DTOs (C# records)
├── Configuration/
│   └── BotConfiguration.cs       # Strongly-typed config classes
├── Program.cs                     # Application entry point
├── Startup.cs                     # DI configuration, middleware pipeline
└── appsettings.json              # Configuration (Azure AD, Bot, Ktor, Deepgram)
```

## Detailed Technical Flow

### 1. Application Startup

**Program.cs** → **Startup.cs**:
- Registers all services in dependency injection container
- **BotMediaService** registered as **Singleton** (maintains Graph Communications Client)
- **KtorService**, **DeepgramService** registered as **Transient** (fresh instances per meeting)
- **MeetingSessionManager** created manually (not via DI)

**BotMediaService initialization**:
- Creates `ICommunicationsClient` via `CommunicationsClientBuilder`
- Configures authentication (Azure AD client credentials flow via MSAL)
- Sets up **MediaPlatformSettings** with:
  - Certificate thumbprint (for secure media connection)
  - Public IP address and ports (for media streaming)
  - Service FQDN
- Subscribes to `OnIncoming` call events
- **Platform check**: Exits gracefully on non-Windows systems (macOS/Linux not supported)

### 2. Incoming Call Reception

**Microsoft Teams** sends webhook notification → **BotController.HandleIncomingCall()**:
- Converts ASP.NET Core `HttpRequest` to `HttpRequestMessage` (Graph SDK requirement)
- Copies request body and all headers (critical for authentication validation)
- Calls `Client.ProcessNotificationAsync()` to deserialize Teams notification
- Returns HTTP 202 Accepted (prevents Teams from retrying)

**BotMediaService.OnIncomingCallReceived()**:
- Receives `ICall` object from Graph SDK
- Spawns async task via `Task.Run()` for concurrent handling
- Creates new `MeetingSessionManager` via `CreateSessionManager()`
- Gets fresh service instances: `KtorService`, `DeepgramService`, `ILogger`
- Tracks session in `ConcurrentDictionary<string, MeetingSessionManager>` by call ID

### 3. Call Validation & Session Initialization

**MeetingSessionManager.HandleIncomingCallAsync()**:

**Step 1: Call Type Validation**
- Checks `call.Resource?.MeetingInfo` (non-null = meeting call)
- If null (1-to-1 call): Reject with `call.RejectAsync(RejectReason.None)`
- Only group/meeting calls proceed

**Step 2: Extract Meeting Metadata**
```csharp
var threadId = call.Resource.ChatInfo.ThreadId;        // Teams chat thread
var messageId = call.Resource.ChatInfo.MessageId;      // Meeting message
var tenantId = call.Resource.TenantId;                  // Azure AD tenant
var meetingId = call.Id;                                // Call/meeting ID
```

**Step 3: Ktor Session Registration**
- POST to `{KtorBaseUrl}/api/sessions/init`
- Request body (C# record):
  ```json
  {
    "threadId": "19:meeting_xxx",
    "messageId": "1234567890",
    "tenantId": "tenant-guid",
    "meetingId": "call-guid"
  }
  ```
- Response (C# record):
  ```json
  {
    "sessionId": "unique-session-id",
    "url": "https://app.example.com/session/xyz"
  }
  ```

### 4. Real-Time Services Initialization

**Step 4: Deepgram Initialization**
- Creates `ListenWebSocketClient` with API key
- Configures streaming settings:
  - Model: `nova-2`
  - Language: `en-US`
  - Smart formatting enabled
  - Interim results enabled (for live updates)
- Subscribes to transcript events via callback: `OnTranscriptReceived(string text, bool isFinal)`
- Opens WebSocket connection to Deepgram

**Step 5: Ktor WebSocket Connection**
- Connects to `{KtorWebSocketUrl}/ws/bot?sessionId={sessionId}`
- Subscribes to incoming messages via callback: `OnKtorMessageReceived(string message)`
- Listens for live summaries from Ktor's AI processing

**Step 6: Recording Status Compliance**
- POST to `{KtorBaseUrl}/api/recording/status`
- Request body:
  ```json
  {
    "callId": "call-guid",
    "status": "recording"
  }
  ```
- **Required by law**: Disclose when accessing meeting media

### 5. Answering the Call & Media Setup

**Step 7: Create Media Session**
- `CreateMediaSession()` via `ICommunicationsClient`
- Configure audio socket:
  ```csharp
  new AudioSocketSettings {
      StreamDirections = StreamDirection.Recvonly,  // Only receive audio
      SupportedAudioFormat = AudioFormat.Pcm16K     // 16kHz PCM
  }
  ```
- Configure video socket:
  ```csharp
  new VideoSocketSettings {
      StreamDirections = StreamDirection.Inactive   // No video processing
  }
  ```
- Subscribe to `AudioSocket.AudioMediaReceived` event

**Step 8: Answer Call**
- `call.AnswerAsync(mediaSession, CancellationToken.None)`
- Teams starts streaming audio to bot
- Media Platform SDK delivers audio buffers via event handler

**Step 9: Send URL to Meeting Chat**
- **TODO**: Use Microsoft Graph API to send chat message
- Message content: `"Meeting transcription is active. View live updates here: {url}"`
- Requires `Chat.ReadWrite` permission

**Step 10: Monitor Call State**
- Subscribe to `call.OnUpdated` event
- Listen for `CallState.Terminated`

### 6. Real-Time Audio Processing Pipeline

**Audio Flow**:
```
Teams Meeting → Media Platform SDK → OnAudioMediaReceived()
    → Marshal audio buffer to byte[] → Deepgram.SendAudio()
    → Deepgram processes → OnTranscriptReceived()
    → Send to Ktor WebSocket
```

**OnAudioMediaReceived()**:
- Receives `AudioMediaReceivedEventArgs` with native buffer (`e.Buffer.Data`)
- Buffer specs: PCM 16kHz, variable length
- Marshal unmanaged memory to managed byte array:
  ```csharp
  byte[] audioData = new byte[e.Buffer.Length];
  Marshal.Copy(e.Buffer.Data, audioData, 0, (int)e.Buffer.Length);
  ```
- Send to Deepgram: `_deepgramService.SendAudio(audioData)`
- **Always dispose buffer**: `e.Buffer.Dispose()` (critical to prevent memory leaks)

**OnTranscriptReceived(string text, bool isFinal)**:
- Receives transcripts from Deepgram (interim or final)
- Creates `TranscriptMessage` record:
  ```json
  {
    "type": "TRANSCRIPT",
    "sessionId": "session-id",
    "text": "transcribed text",
    "isFinal": true/false
  }
  ```
- Sends to Ktor via WebSocket: `_ktorService.SendTranscriptAsync()`
- Fire-and-forget pattern (async without await)

### 7. Live Summary Reception

**OnKtorMessageReceived(string message)**:
- Receives JSON messages from Ktor via WebSocket
- Parses message type:
  ```json
  {
    "type": "LIVE_SUMMARY",
    "sessionId": "session-id",
    "summary": "AI-generated summary text"
  }
  ```
- Console logs with formatted output:
  ```
  =================================================
  [LIVE SUMMARY] SessionId: xyz
  Summary: Meeting is discussing Q4 budget allocations...
  =================================================
  ```
- Also logs via `ILogger` for persistence

### 8. Meeting End & Cleanup

**OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)**:
- Monitors `args.NewResource.State`
- When `CallState.Terminated`: Trigger cleanup

**HandleCallEndAsync(string reason)**:

**Step 1: Update Recording Status**
- POST to `{KtorBaseUrl}/api/recording/status`
- Request body:
  ```json
  {
    "callId": "call-guid",
    "status": "notRecording"
  }
  ```
- **Compliance requirement**: Disclose when media access ends

**Step 2: Send Meeting End Notification**
- Send message to Ktor WebSocket:
  ```json
  {
    "type": "MEETING_END",
    "sessionId": "session-id",
    "reason": "Terminated"
  }
  ```

**Step 3: Disconnect Services**
- Close Ktor WebSocket connection
- Close Deepgram WebSocket connection
- Dispose session manager resources

**Step 4: Remove from Active Sessions**
- Remove from `BotMediaService._activeSessions` ConcurrentDictionary
- Dispose `MeetingSessionManager`

### 9. Concurrent Meeting Handling

**Concurrency Strategy**:
- Each meeting gets **isolated service instances**:
  - Fresh `KtorService` (new HTTP client, new WebSocket)
  - Fresh `DeepgramService` (new WebSocket connection)
  - Fresh `MeetingSessionManager`
- Shared `ICommunicationsClient` (designed for multiple simultaneous calls)
- Tracked in `ConcurrentDictionary<string, MeetingSessionManager>` (thread-safe)
- Each meeting runs in separate `Task` (parallel processing)

**Service Lifetime Management**:
- `BotMediaService`: **Singleton** (lives for entire application lifetime)
- `KtorService`: **Transient** (new instance per `GetRequiredService` call)
- `DeepgramService`: **Transient** (new instance per call)
- `MeetingSessionManager`: **Not registered in DI** (created manually)

**Why this works**:
- Service provider passed to `BotMediaService` constructor
- Each meeting calls `_serviceProvider.GetRequiredService<KtorService>()`
- DI creates new instance due to Transient lifetime
- Complete isolation between meetings

### 10. Authentication & Security

**Azure AD Authentication** (Client Credentials Flow):
- Uses MSAL (`Microsoft.Identity.Client`)
- `ConfidentialClientApplication` with client secret
- Acquires token for `https://graph.microsoft.com/.default` scope
- Adds `Authorization: Bearer {token}` header to all Graph API calls

**Inbound Request Validation**:
- **TODO**: Implement JWT validation from Teams webhooks
- Currently trusts all inbound requests (development only)
- Production requires validating Teams-signed JWT tokens

**Certificate-Based Media Connection**:
- Media Platform requires X.509 certificate
- Certificate must be installed in `LocalMachine\My` store (Windows)
- Private key permissions required for application identity
- Used for TLS/DTLS media channel encryption

### 11. Configuration Management

**Strongly-Typed Configuration** (IOptions pattern):
```csharp
AzureAdConfiguration:
  - TenantId, ClientId, ClientSecret (Azure AD app registration)

BotConfiguration:
  - BotBaseUrl (ngrok URL for webhooks)
  - PublicIpAddress, InstancePublicPort (for media streaming)
  - CertificateThumbprint, ServiceFqdn

KtorConfiguration:
  - ApiBaseUrl, WebSocketUrl
  - SessionInitEndpoint, UpdateRecordingStatusEndpoint

DeepgramConfiguration:
  - ApiKey, Model, Language
```

All config classes use `required` properties (null safety).

### 12. Platform Requirements

**Windows-Only Requirement**:
- Media Platform SDK contains native Windows DLLs
- Runtime check: `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`
- On macOS/Linux: Displays error and exits gracefully with code 1
- Error message explains requirement and current platform

### 13. Error Handling & Resilience

**Call-Level Error Handling**:
- Exceptions during call handling → reject call and cleanup
- Always return HTTP 202 (prevents Teams retry storms)
- Dispose resources in finally blocks

**Audio Buffer Management**:
- Wrap buffer processing in try-catch
- Always dispose buffers in finally block
- Prevent memory leaks from unmanaged memory

**Service Isolation**:
- One meeting failure doesn't affect others
- Each session manager independently disposable
- ConcurrentDictionary provides thread-safe session tracking

## Features:

## Flow

- This bot shall receive a call from teams when a meeting that it has been invited to starts.
- If the call is a meeting call and not 1 to one then proceed else reject the call.
- This bot shall reach out to the configured ktor server endpoint with thread Id , message Id, Tenant Id and meeting id in post req - ktor should give it the session id.
- After receiving the session id answer the call.
- After answering the call the bot should open a ws connection with the ktor server with the configured endpoint with the query param ?sessionId=<session-id>
- On receiving audio from the call send them to deepgram to get the transcription from deepgram.
- After geting the transcription push them to the ws connected to ktor.
- The ktor server in the connected ws will send a live summary as text. Bot should console log everytime this is received.
- When the meeting ends the bot should send a relevant message in the ws connection close the ws connection with ktor and close deepgram and dispose other resources.
- The bot should call the update meeting status api before accessing media and after it has done accessing it.

## Error handlings / Scalability requirements

- The bot must be able to join multiple meetings, hence the code should handle this gracefully.

## Code requirements

- All helper functions should have good documentation.
- Null handling should be done properly.
- Ideally split code to multiple files that have different responsibility.

