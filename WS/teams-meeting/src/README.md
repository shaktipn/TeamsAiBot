# Teams Meeting WebSocket API

## Overview

The Teams Meeting WebSocket API enables real-time communication between meeting bots, the Ktor server, and client applications. The system provides:

- **Live Transcription**: Receive real-time transcriptions from meeting bots
- **AI Summaries**: Automatically generate and broadcast AI-powered meeting summaries
- **Multi-Client Support**: Multiple clients can connect to the same meeting session

## WebSocket Endpoints

### Transcription WebSocket

**URL**: `ws://host/ws/transcription?sessionId=<uuid>`

**Required Query Parameters**:
- `sessionId` (UUID): Unique identifier for the meeting session. Must be a valid UUID that is registered in the database.

**Connection Validation**:
- The `sessionId` must exist and be registered before connection
- Invalid or missing `sessionId` will result in connection rejection with `CANNOT_ACCEPT` error code

## Message Formats

All messages are exchanged in JSON format with a `type` field that identifies the message kind.

### Incoming Messages (Client → Server)

#### 1. TRANSCRIPT Message

Sent by the bot to deliver meeting transcription text.

```json
{
  "type": "TRANSCRIPT",
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "text": "Hello everyone, welcome to today's meeting.",
  "isFinal": true
}
```

**Fields**:
- `type` (string, required): Must be `"TRANSCRIPT"`
- `sessionId` (string, required): UUID string matching the WebSocket connection sessionId
- `text` (string, required): The transcribed text content
- `isFinal` (boolean, required):
  - `true` = Final transcription (saved to database)
  - `false` = Interim transcription result (not saved)

#### 2. MEETING_END Message

Sent by the bot to signal that the meeting has concluded.

```json
{
  "type": "MEETING_END",
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "reason": "Meeting concluded by host"
}
```

**Fields**:
- `type` (string, required): Must be `"MEETING_END"`
- `sessionId` (string, required): UUID string matching the WebSocket connection sessionId
- `reason` (string, required): Description of why the meeting ended

### Outgoing Messages (Server → Client)

#### 1. LIVE_SUMMARY Message

Sent by the server to all connected clients with an AI-generated summary of the meeting transcriptions.

```json
{
  "type": "LIVE_SUMMARY",
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "summary": "The meeting discussed Q4 budget planning and marketing strategies. Key decisions included approving the $50K marketing campaign and scheduling follow-up for next week."
}
```

**Fields**:
- `type` (string): Always `"LIVE_SUMMARY"`
- `sessionId` (string): UUID string of the meeting session
- `summary` (string): AI-generated text summary of processed transcriptions

**Frequency**: Summaries are generated and sent every **30 seconds** while:
- At least one client is connected to the session
- New unprocessed transcriptions are available

## Connection Flow

### 1. Initial Connection

1. Client connects to `ws://host/ws/transcription?sessionId=<uuid>`
2. Server validates the `sessionId` (must be registered in database)
3. If valid, connection is accepted and client is added to the session
4. If invalid, connection is closed with `CANNOT_ACCEPT` code

### 2. First Client Connects

When the **first client** connects to a session:
- Server starts periodic summary generation (every 30 seconds)
- Summary generation continues as long as at least one client remains connected

### 3. Active Session

While clients are connected:
- Clients send `TRANSCRIPT` messages → Server saves final transcriptions to database
- Server periodically (every 30 seconds):
  - Fetches unprocessed transcriptions from database
  - Generates AI summary using transcription text
  - Broadcasts `LIVE_SUMMARY` message to **all clients** in the session
  - Marks transcriptions as processed

### 4. Client Disconnects

- Client WebSocket disconnects
- Server removes client from the session
- If this was the **last client** in the session:
  - Server stops periodic summary generation for that session
  - Session resources are cleaned up

### 5. Message Validation

- All incoming messages must have a `sessionId` that matches the connection's `sessionId`
- Messages with mismatched `sessionId` are ignored and logged
- Malformed JSON messages are logged but do not close the connection

## Session Management

**Multi-Client Support**:
- Multiple clients (bots + React apps) can connect to the same `sessionId`
- All clients in a session receive the same `LIVE_SUMMARY` messages
- Transcriptions from any client are processed for all clients

**State Persistence**:
- Transcriptions are stored in the database
- Summaries are saved to the `MeetingSummary` table
- Processed transcriptions are marked with `processed = true`

**Lifecycle Management**:
- Summary generation is tied to client presence (not session creation)
- Sessions automatically clean up when all clients disconnect
- No manual session termination required
