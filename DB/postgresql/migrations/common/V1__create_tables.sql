CREATE TABLE "User" (
  "id" UUID PRIMARY KEY,
  "email" VARCHAR(255) NOT NULL UNIQUE,
  "isActive" BOOL DEFAULT true,
  "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  "modifiedAt" TIMESTAMP WITH TIME ZONE
);

CREATE TABLE "WT" (
  "wt" VARCHAR(255) NOT NULL,
  "userId" UUID NOT NULL,
  "expiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
  "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  "isDeleted" BOOL DEFAULT FALSE,
  CONSTRAINT fk_userId FOREIGN KEY ("userId") REFERENCES "User" ("id")
);

CREATE TABLE "Password" (
  "userId" UUID NOT NULL,
  "password" BYTEA NOT NULL,
  "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  "isDeleted" BOOL DEFAULT FALSE,
  CONSTRAINT fk_userId FOREIGN KEY ("userId") REFERENCES "User" ("id")
);

-- Stores the metadata for every meeting the bot joins.
CREATE TABLE "MeetingSession" (
    "id" UUID NOT NULL,
    "meetingUrl" TEXT,
    "threadId" VARCHAR(255),
    "messageId" VARCHAR(255),
    "meetingId" VARCHAR(50),
    "tenantId" VARCHAR(50),
    "title" VARCHAR(500), -- Optional: If we extract meeting title later using AI
    "status" VARCHAR(50) NOT NULL DEFAULT 'REGISTERED', -- REGISTERED, ACTIVE, COMPLETED, FAILED

    "isDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "modifiedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT "pk_MeetingSession_id" PRIMARY KEY ("id")
);

CREATE TABLE "MeetingSessionUsers" (
    "userId" UUID NOT NULL,
    "meetingSessionId" UUID NOT NULL,

    "isDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT "pk_MeetingSessionUsers_user_meetingSession_id" PRIMARY KEY ("userId", "meetingSessionId"),
    CONSTRAINT "fk_userId" FOREIGN KEY ("userId") REFERENCES "User"("id"),
    CONSTRAINT "fk_meetingSessionId" FOREIGN KEY ("meetingSessionId") REFERENCES "MeetingSession"("id")
);

-- Stores raw text received from the C# Bot (via Deepgram).
-- This acts as the "Source of Truth" for generating summaries.
CREATE TABLE "TranscriptChunk" (
    "id" UUID NOT NULL,
    "sessionId" UUID NOT NULL,
    "text" TEXT NOT NULL,
    "speakerName" VARCHAR(255), -- Optional: If we add speaker diarization later
    "isFinal" BOOLEAN NOT NULL DEFAULT TRUE,
    "processed" BOOLEAN NOT NULL DEFAULT FALSE,

    "isDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT "pk_TranscriptChunk_id" PRIMARY KEY ("id"),
    CONSTRAINT "fk_TranscriptChunk_sessionId" FOREIGN KEY ("sessionId") REFERENCES "MeetingSession"("id")
);

-- Tracks every interaction with the LLM.
-- Stores the state *before* and *after* the update.
CREATE TABLE "AiSummaryLog" (
    "id" UUID NOT NULL,
    "sessionId" UUID NOT NULL,
    "inputData" JSONB NOT NULL, -- Contains previous summary, new transcript delta
    "response" TEXT,            -- The result from AI
    "isSuccessful" BOOLEAN NOT NULL DEFAULT FALSE,
    "errorMessage" TEXT,
    "tokenUsage" JSONB,                 -- Stores { "prompt": 100, "completion": 50 }
    "processingTimeMs" INT,

    "isDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT "pk_AiSummaryLog_id" PRIMARY KEY ("id"),
    CONSTRAINT "fk_AiSummaryLog_sessionId" FOREIGN KEY ("sessionId") REFERENCES "MeetingSession"("id")
);

CREATE TABLE "MeetingSummary" (
    "id" UUID NOT NULL,
    "sessionId" UUID NOT NULL,
    "summary" TEXT NOT NULL,
    "createdBy" UUID,  -- AI if null else user id
    "createdAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "isDeleted" BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT "pk_MeetingSummary_id" PRIMARY KEY ("id"),
    CONSTRAINT "fk_MeetingSummary_sessionId" FOREIGN KEY ("sessionId") REFERENCES "MeetingSession"("id"),
    CONSTRAINT "fk_MeetingSummary_createdBy" FOREIGN KEY ("createdBy") REFERENCES "User"("id")
);
