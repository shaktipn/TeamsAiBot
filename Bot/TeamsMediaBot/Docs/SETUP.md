# Teams Media Bot - Setup Guide

This guide will walk you through setting up and running the Teams Media Bot locally.

## Prerequisites

- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Azure Subscription** - For Azure AD app registration
- **Microsoft Teams** - With admin access or ability to sideload apps
- **ngrok** - For tunneling (free tier works)
- **Deepgram Account** - For transcription service
- **Ktor Server** - Running locally or accessible endpoint
- **Windows Certificate** (for local testing) or valid SSL certificate

---

## Step 1: Azure AD App Registration

### [Official Doc](https://learn.microsoft.com/en-us/microsoftteams/platform/teams-ai-library/teams/configuration/manual-configuration?tabs=azure-portal)

### 1.1 Create an Azure AD Application

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations** > **New registration**
3. Fill in the details:
   - **Name**: `TeamsMediaBot`
   - **Supported account types**: `Accounts in any organizational directory (Any Azure AD directory - Multitenant)`
   - **Redirect URI**: Leave blank for now
4. Click **Register**

### 1.2 Note Down the App Details

After registration, note these values (you'll need them later):
- **Application (client) ID**
- **Directory (tenant) ID**

### 1.3 Create a Client Secret

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add a description (e.g., "Bot Secret")
4. Choose an expiration period
5. Click **Add**
6. **IMPORTANT**: Copy the secret value immediately (it won't be shown again)

### 1.4 Configure API Permissions

1. Go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Application permissions** (not Delegated)
5. Add the following permissions:
   - `Calls.AccessMedia.All`
   - `Calls.JoinGroupCall.All`
   - `OnlineMeetings.Read.All`
   - `Chat.ReadWrite.All` (for sending messages to chat)
6. Click **Grant admin consent for [Your Tenant]**

---

## Step 2: Certificate Setup (Windows)

The Media Platform requires a certificate for secure media connection.

### 2.1 Create a Self-Signed Certificate (Development Only)

Open PowerShell as Administrator and run:

```powershell
# Create a self-signed certificate
$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My" -NotAfter (Get-Date).AddYears(2)

# Note the thumbprint
$cert.Thumbprint

# Export the certificate (optional, for backup)
Export-Certificate -Cert $cert -FilePath "C:\TeamsMediaBot.cer"
```

### 2.2 Get Your Certificate Thumbprint

```powershell
# List all certificates in Local Machine store
Get-ChildItem -Path cert:\LocalMachine\My

# Find the one you just created and copy the Thumbprint
```

### 2.3 Grant Permissions to the Certificate

The bot needs permission to use the certificate's private key:

```powershell
# Replace [THUMBPRINT] with your actual thumbprint
$thumbprint = "YOUR_CERTIFICATE_THUMBPRINT"
$cert = Get-ChildItem -Path cert:\LocalMachine\My\$thumbprint

# Get the certificate's private key path
$rsaCert = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
$path = $rsaCert.Key.UniqueName

# Grant permission (replace NETWORK SERVICE with your user if running as user)
icacls "C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\$path" /grant "NETWORK SERVICE:RX"
```

---

## Step 3: Get Your Public IP Address

The Media Platform needs to know your public IP:

```powershell
# Get your public IP
Invoke-RestMethod -Uri "https://api.ipify.org"
```

Or visit [https://www.whatismyip.com](https://www.whatismyip.com)

**Note**: If you're behind a NAT/firewall, you'll need to configure port forwarding for port 8445 (or your chosen media port).

---

## Step 4: ngrok Setup

⚠️ **CRITICAL: Two-Tunnel Requirement & Dynamic Port Configuration**

Media bots require **TWO separate ngrok tunnels**:
1. **HTTP tunnel** for signaling (call notifications, webhooks)
2. **TCP tunnel** for media traffic (real-time audio streaming)

**IMPORTANT:** The TCP tunnel assigns a **dynamic remote port** each time it starts (with free plan). You MUST capture this port number and configure it in `appsettings.json` as `InstancePublicPort` (see Step 7.1). Failure to do this will result in no audio flowing during calls.

### 4.1 Install ngrok

Download from [ngrok.com](https://ngrok.com/download) and follow installation instructions.

### 4.2 Configure ngrok.yml

Create or edit your `ngrok.yml` configuration file:

**Location:**
- **Windows**: `%USERPROFILE%\.ngrok2\ngrok.yml`
- **macOS/Linux**: `~/.ngrok2/ngrok.yml`

**Configuration:**
```yaml
version: "2"
authtoken: YOUR_NGROK_AUTH_TOKEN

tunnels:
  signaling:
    addr: 5000
    proto: http
    bind_tls: true

  media:
    addr: 8445
    proto: tcp
    remote_addr: 0.tcp.ngrok.io:YOUR_RESERVED_PORT
```

**Notes:**
- Replace `YOUR_NGROK_AUTH_TOKEN` with your ngrok auth token (get it from [ngrok dashboard](https://dashboard.ngrok.com/get-started/your-authtoken))
- For TCP tunneling, you may need a **paid ngrok plan** or use the free TCP endpoint (which gives you a random port)
- If using free plan, remove the `remote_addr` line (ngrok will assign a random port)

### 4.3 Start ngrok Tunnels

Open a terminal and start both tunnels:

```bash
ngrok start --all
```

Or start them individually in separate terminals:

**Terminal 1 - HTTP Signaling:**
```bash
ngrok http 5000
```

**Terminal 2 - TCP Media:**
```bash
ngrok tcp 8445
```

### 4.4 Note Your ngrok URLs

After starting ngrok, you'll see output like this:

**HTTP Tunnel (Signaling):**
```
Forwarding   https://abc123.ngrok-free.app -> http://localhost:5000
```
- **Copy the HTTPS URL**: `https://abc123.ngrok-free.app`
- This will be your **BotBaseUrl** (full URL) in appsettings.json
- This will be your **ServiceFqdn** (domain only, without `https://`) in appsettings.json

**TCP Tunnel (Media):**
```
Forwarding   tcp://0.tcp.ngrok.io:12345 -> localhost:8445
```
- **CRITICAL: Note the remote port number**: `12345`
- This port is **randomly assigned** each time you restart ngrok (with free plan)
- This will be your **InstancePublicPort** in appsettings.json (Step 7.1)
- External clients (Microsoft Teams) connect to this remote port for media streaming

**Example Configuration Mapping:**
From the output above, you would configure:
```json
"Bot": {
  "BotBaseUrl": "https://abc123.ngrok-free.app",
  "ServiceFqdn": "abc123.ngrok-free.app",
  "InstancePublicPort": 12345  // ← From TCP tunnel output
}
```

### 4.5 Understanding ngrok TCP Port Mapping

**How TCP Tunneling Works:**

When you start `ngrok tcp 8445`, ngrok creates a tunnel like this:
```
External (Internet)          ngrok Cloud          Your Machine
    ↓                            ↓                      ↓
tcp://0.tcp.ngrok.io:12345 → ngrok server → localhost:8445
```

**Port Terminology:**
- **Local Port (InstanceInternalPort)**: `8445` - Where your bot listens locally
- **Remote Port (InstancePublicPort)**: `12345` - Where Teams connects externally
- These are **different numbers** with ngrok's free plan

**Why This Matters:**
- Microsoft Teams connects to the **remote port** (`0.tcp.ngrok.io:12345`)
- ngrok forwards that traffic to your **local port** (`localhost:8445`)
- Your bot's Media Platform SDK must know the remote port to tell Teams where to connect
- This is why you configure `InstancePublicPort: 12345` in appsettings.json

**Port Assignment:**
- With **ngrok free plan**: Remote port is randomly assigned each restart
- With **ngrok paid plan**: You can reserve a fixed remote port using `remote_addr` in ngrok.yml
- The local port (8445) always stays the same

**Example Flow:**
1. You start: `ngrok tcp 8445`
2. ngrok outputs: `tcp://0.tcp.ngrok.io:12345 -> localhost:8445`
3. You configure: `InstancePublicPort: 12345` in appsettings.json
4. Your bot tells Teams: "Connect to me at port 12345"
5. Teams sends media to: `0.tcp.ngrok.io:12345`
6. ngrok forwards to: `localhost:8445`
7. Your bot receives media on port 8445

### 4.6 Important Notes

⚠️ **For Development/Testing Only:**
- The free ngrok plan works but gives you random TCP ports each time
- You'll need to update your configuration each time you restart ngrok

⚠️ **Port Forwarding:**
- If you're behind a NAT/firewall and not using ngrok TCP tunnel, you MUST forward port 8445
- Teams needs direct TCP access to your media port
- The TCP tunnel handles this automatically

**IMPORTANT**: Keep both terminals running while testing the bot!

---

## Step 5: Deepgram Setup

### 5.1 Create a Deepgram Account

1. Go to [Deepgram](https://console.deepgram.com/signup)
2. Sign up for a free account
3. Navigate to **API Keys**
4. Create a new API key
5. Copy the API key

---

## Step 6: Ktor Server Setup

Ensure your Ktor server is running and accessible. The bot expects these endpoints:

### Required Endpoints:

1. **Session Initialization**
   - **Method**: POST
   - **URL**: `http://localhost:8280/api/sessions/init`
   - **Request Body**:
     ```json
     {
       "threadId": "string",
       "messageId": "string",
       "tenantId": "string",
       "meetingId": "string"
     }
     ```
   - **Response**:
     ```json
     {
       "sessionId": "string",
       "url": "string"
     }
     ```

2. **Recording Status (Compliance)**

   **Note:** The bot automatically calls Microsoft Graph API directly for recording status updates (compliance requirement). No Ktor endpoint is needed for this.

   **Graph API Endpoint Used:**
   - `POST https://graph.microsoft.com/v1.0/communications/calls/{id}/updateRecordingStatus`
   - **Permission Required:** `Calls.AccessMedia.All`
   - **Called automatically** before answering call (status: "recording") and after call ends (status: "notRecording")
   - **Reference:** [Microsoft Documentation](https://learn.microsoft.com/en-us/graph/api/call-updaterecordingstatus?view=graph-rest-1.0)

3. **WebSocket Connection**
   - **URL**: `ws://localhost:8280/ws/bot?sessionId={sessionId}`
   - **Incoming Messages** (from bot to Ktor):
     ```json
     {
       "type": "TRANSCRIPT",
       "sessionId": "string",
       "text": "string",
       "isFinal": boolean
     }
     ```
     ```json
     {
       "type": "MEETING_END",
       "sessionId": "string",
       "reason": "string"
     }
     ```
   - **Outgoing Messages** (from Ktor to bot):
     ```json
     {
       "type": "LIVE_SUMMARY",
       "sessionId": "string",
       "summary": "string"
     }
     ```

---

## Step 7: Configure the Bot

### 7.1 Update appsettings.json

Open `appsettings.json` and fill in your values:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Graph": "Information"
    }
  },
  "AllowedHosts": "*",

  "AzureAd": {
    "TenantId": "YOUR_AZURE_TENANT_ID",
    "ClientId": "YOUR_APP_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },

  "Bot": {
    "BotBaseUrl": "https://your-ngrok-url.ngrok-free.app",
    "PublicIpAddress": "YOUR_PUBLIC_IP_ADDRESS",
    "InstanceInternalPort": 8445,
    "InstancePublicPort": 12345,  // ← UPDATE THIS with ngrok TCP remote port from Step 4.4
    "CertificateThumbprint": "YOUR_CERTIFICATE_THUMBPRINT",
    "ServiceFqdn": "your-ngrok-url.ngrok-free.app"
  },

  "Ktor": {
    "ApiBaseUrl": "http://localhost:8280",
    "WebSocketUrl": "ws://localhost:8280/ws/bot",
    "SessionInitEndpoint": "/api/sessions/init",
    "UpdateRecordingStatusEndpoint": "/api/recording/status"
  },

  "Deepgram": {
    "ApiKey": "YOUR_DEEPGRAM_API_KEY",
    "Model": "nova-2",
    "Language": "en-US"
  }
}
```

### 7.2 Configuration Values Explained

- **AzureAd:TenantId**: From Step 1.2
- **AzureAd:ClientId**: From Step 1.2
- **AzureAd:ClientSecret**: From Step 1.3
- **Bot:BotBaseUrl**: Your ngrok HTTPS URL from Step 4.4 (e.g., `https://abc123.ngrok-free.app`)
- **Bot:PublicIpAddress**: Your public IP from Step 3
- **Bot:InstanceInternalPort**: Local port where bot listens for media (default: 8445)
- **Bot:InstancePublicPort**: **CRITICAL** - The remote port from ngrok TCP tunnel output in Step 4.4. This changes every time you restart ngrok with free plan. Example: If ngrok shows `tcp://0.tcp.ngrok.io:12345`, set this to `12345`
- **Bot:CertificateThumbprint**: From Step 2.2
- **Bot:ServiceFqdn**: Your ngrok domain (without `https://`) from Step 4.4 (e.g., `abc123.ngrok-free.app`)
- **Ktor:ApiBaseUrl**: Your Ktor server URL (update if not localhost)
- **Ktor:WebSocketUrl**: Your Ktor WebSocket URL (update if not localhost)
- **Deepgram:ApiKey**: From Step 5

### 7.3 Understanding Bot Configuration Values

#### **Port Configuration (8445)**

- **`InstanceInternalPort`**: The local port the bot listens on for media traffic (default: 8445)
- **`InstancePublicPort`**: The external port Teams connects to for media streams (default: 8445)

**Why port 8445?**
- Conventional port for Microsoft Graph Communications Media Platform
- Must be open and forwarded through NAT/firewall
- Used for secure real-time audio streaming (SRTP/DTLS)

**Can I change it?**
- Yes, but ensure both ports match and are properly configured in your firewall/NAT
- Common alternatives: 8446, 9000-9100 range

#### **Why Two Different Ports? (Understanding Port Configuration)**

When using ngrok with the free plan, you'll notice two different port numbers:
- **InstanceInternalPort (8445)**: The local port on your machine
- **InstancePublicPort (e.g., 12345)**: The external port assigned by ngrok

**Visual Explanation:**
```
┌─────────────────┐         ┌──────────────┐         ┌────────────────┐
│ Microsoft Teams │────────▶│ ngrok Cloud  │────────▶│  Your Machine  │
│                 │         │              │         │                │
│ Connects to:    │         │ TCP Tunnel:  │         │ Bot listens on:│
│ port 12345      │         │ 0.tcp.ngrok  │         │ port 8445      │
│                 │         │ .io:12345    │         │                │
└─────────────────┘         └──────────────┘         └────────────────┘
    (Public Port)              (ngrok maps)          (Internal Port)
```

**Why This Matters:**
- **Port 5000**: HTTP endpoint for call notifications/webhooks (separate concern)
- **Port 8445 (Internal)**: Where your Media Platform SDK listens locally
- **Port 12345 (Public)**: Where Teams connects for media streaming
- ngrok bridges the public port to your internal port automatically

**Configuration Requirements:**
1. Your bot binds to port 5000 for HTTP (signaling)
2. Your Media Platform SDK binds to port 8445 (internal media)
3. ngrok exposes port 8445 as a random public port (e.g., 12345)
4. You tell Teams to connect to the public port via `InstancePublicPort` configuration

#### **ServiceFqdn (Fully Qualified Domain Name)**

- **What it is:** The publicly resolvable hostname for your bot (e.g., `your-ngrok-url.ngrok-free.app`)
- **Where it's used:** Passed to Microsoft's Media Platform to validate incoming connections
- **Format:** Domain only, WITHOUT `https://` prefix
- **Example:** If your ngrok URL is `https://abc123.ngrok-free.app`, set ServiceFqdn to `abc123.ngrok-free.app`

**Why is it needed?**
- The Media Platform uses this for DNS validation
- Ensures Teams can route media streams to your bot
- Required for secure media connection establishment

**Code Reference:** These values are used in `BotMediaService.cs` when initializing `MediaPlatformInstanceSettings`.

---

## Step 8: Teams App Setup

You can set up your Teams app using either the Developer Portal (recommended) or manual manifest creation.

### Method 1: Teams Developer Portal (Recommended)

The Teams Developer Portal provides a visual interface for app configuration and makes updates easier.

#### 8.1 Access the Developer Portal

1. Go to [Teams Developer Portal](https://dev.teams.microsoft.com)
2. Sign in with your Microsoft 365 account
3. Click **Apps** in the left sidebar
4. Click **+ New app**

#### 8.2 Configure Basic Information

1. **App names:**
   - **Short name:** `Surya AI Bot`
   - **Full name:** `Teams Media Bot`
2. **Descriptions:**
   - **Short description:** `A bot that transcribes meetings`
   - **Full description:** `A bot that joins Teams meetings and provides real-time transcription with live summaries`
3. **Developer information:**
   - **Developer or company name:** Your company name
   - **Website:** Your company website
   - **Privacy policy:** Your privacy policy URL
   - **Terms of use:** Your terms of use URL
4. **App ID:** Use your **Azure AD App Client ID** from Step 1.2
5. **Version:** `1.0.0`
6. Click **Save**

#### 8.3 Configure App Icons

1. Go to **Branding** in the left menu
2. Upload icons:
   - **Color icon:** 192x192 PNG (full color logo)
   - **Outline icon:** 32x32 PNG (transparent background, white outline)
3. **Accent color:** `#FFFFFF` (or your brand color)
4. Click **Save**

#### 8.4 Configure Bot

1. Go to **App features** in the left menu
2. Click **Bot**
3. Click **+ Create a new bot** OR **Select an existing bot**
4. If creating new:
   - **Bot name:** `TeamsMediaBot`
   - **Microsoft App ID:** Use your **Azure AD App Client ID** from Step 1.2
5. If selecting existing:
   - Choose the bot with your App Client ID
6. **Scopes:** Check:
   - ☑ Team
   - ☑ Group chat
7. **Calling capabilities:**
   - ☑ Supports calling
   - ☑ Supports video
8. Click **Save**

#### 8.5 Configure Valid Domains

1. Go to **Domains** in the left menu
2. Add your ngrok domain:
   - Example: `abc123.ngrok-free.app` (without `https://`)
3. Click **Add**

#### 8.6 Configure App Permissions (RSC)

**What is RSC?** Resource-Specific Consent (RSC) allows your app to access specific Teams resources (like chat messages) without requiring tenant-wide admin consent. This provides granular permission control.

1. Go to **App features** > **Single sign-on**
2. **Application (client) ID:** Your Azure AD App Client ID
3. **Application ID URI:** `https://RscPermission`
   - This is a **standard placeholder URI** used for RSC permissions in Teams apps
   - It tells Teams this app uses resource-specific consent instead of tenant-wide consent
4. Click **Save**

5. Go to **Configure** in the left menu
6. Scroll to **Microsoft Graph Permissions**
7. Add RSC permissions:
   - `identity` - Allows the app to see user identity information
   - `messageTeamMembers` - Allows the app to send messages to team members

**Note:** These are **RSC permissions**, not the Application permissions you configured in Azure AD (Step 1). They work together:
- **Azure AD Application Permissions**: Allow the bot to call Graph APIs (Calls.AccessMedia.All, etc.)
- **RSC Permissions**: Allow the bot to access specific Teams resources without admin consent

#### 8.7 Publish to Your Organization

1. Go to **Publish** in the left menu
2. Click **Publish to org**
3. Submit for admin approval (if required)

Once approved, the app will be available in your Teams apps catalog.

#### 8.8 Test the App

1. Open Microsoft Teams
2. Go to **Apps**
3. Search for "Media Bot" in your organization's apps
4. Click **Add** to install

---

### Method 2: Manual Manifest Creation (Alternative)

If you prefer manual control or need to automate deployment, you can create the manifest manually.

#### 8.1 Create manifest.json

Create a `manifest.json` file with the following content:

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "YOUR_APP_CLIENT_ID",
  "packageName": "com.yourcompany.teamsmediabot",
  "developer": {
    "name": "Your Company",
    "websiteUrl": "https://yourcompany.com",
    "privacyUrl": "https://yourcompany.com/privacy",
    "termsOfUseUrl": "https://yourcompany.com/terms"
  },
  "name": {
    "short": "Media Bot",
    "full": "Teams Media Bot"
  },
  "description": {
    "short": "A bot that transcribes meetings",
    "full": "A bot that joins Teams meetings and provides real-time transcription"
  },
  "icons": {
    "outline": "outline.png",
    "color": "color.png"
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "YOUR_APP_CLIENT_ID",
      "scopes": [
        "team",
        "groupchat"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false,
      "supportsCalling": true,
      "supportsVideo": true
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "your-ngrok-url.ngrok-free.app"
  ],
  "webApplicationInfo": {
    "id": "YOUR_APP_CLIENT_ID",
    "resource": "https://RscPermission"
  }
}
```

**Important:** Replace `YOUR_APP_CLIENT_ID` with your **Azure AD App Registration Client ID** from Step 1.2. This same ID is used for:
- `id` (app identifier)
- `botId` (bot identifier)
- `webApplicationInfo.id` (for RSC permissions)

**Why are they the same?** The bot IS the Azure AD application, so they share the same Client ID.

---

### ⚠️ Common Confusion: Bot ID vs Azure Bot Resource

**Question:** "If the bot ID is the app registration ID, why do we need to create an Azure Bot resource in Azure Portal?"

**Answer:**
- **Bot ID in manifest** = Your **Azure AD App Registration ID** (from Step 1)
- **Azure Bot Resource** = A separate configuration resource in Azure Portal

**The Azure Bot Resource serves these purposes:**
1. **Channels Configuration**: Links your bot to Teams, Slack, Web Chat, etc.
2. **Messaging Endpoint**: Configures where Teams sends call notifications (`https://your-bot-url/api/calls`)
3. **Bot Management**: Provides analytics, testing tools, and configuration UI
4. **Bridge Between Teams and Your App**: Links your Azure AD app to Teams platform

**They work together:**
```
Azure AD App Registration (Step 1)
    ↓ (provides authentication)
Azure Bot Resource (Step 9)
    ↓ (provides messaging/calling endpoint)
Teams App Manifest (Step 8)
    ↓ (uses App Registration ID as botId)
Your Bot Code
```

**Key Point:** Yes, the `botId` in the manifest is **NOT** the Azure Bot Resource ID - it's your **Azure AD App Registration Client ID**. The Azure Bot Resource is just a configuration layer that connects everything together.

---

#### 8.2 Create Icons

Create two icon files:
- **`outline.png`**: 32x32 pixels, transparent background, white outline
- **`color.png`**: 192x192 pixels, full color logo

#### 8.3 Create App Package

1. Create a ZIP file containing:
   - `manifest.json`
   - `outline.png`
   - `color.png`
2. Name it `TeamsMediaBot.zip`

**Note:** Do NOT create a folder inside the ZIP - the three files should be at the root level.

#### 8.4 Upload to Teams

1. Open Microsoft Teams
2. Go to **Apps**
3. Click **Manage your apps** (bottom left)
4. Click **Upload an app** > **Upload a custom app**
5. Select your `TeamsMediaBot.zip` file
6. Click **Add**

---

## Step 9: Register the Bot Calling Endpoint

### Understanding Configuration Locations

Before configuring the bot calling endpoint, it's important to understand where different settings are configured:

**In `appsettings.json` (Your Bot Application):**
- All runtime configuration values:
  - `BotBaseUrl` - Used by Graph SDK at runtime
  - `PublicIpAddress` - Used by Media Platform SDK at runtime
  - `InstanceInternalPort` & `InstancePublicPort` - Used by Media Platform SDK at runtime
  - `CertificateThumbprint` - Used by Media Platform SDK at runtime
  - `ServiceFqdn` - Used by Media Platform SDK at runtime
- These values control how your bot operates and connects

**In Azure Bot Resource (Azure Portal):**
- Only the webhook endpoints:
  - `Messaging endpoint` - Where Teams sends notifications
  - `Calling webhook` - Where Teams sends call notifications
- These values tell Teams where to reach your bot

**Why Both Are Needed:**
1. Azure Bot Resource acts as a **bridge** between Microsoft Teams and your application
2. It tells Teams: "When there's a call, notify this URL"
3. Your `appsettings.json` tells your bot: "Listen for callbacks at this URL"
4. **They must match** for proper routing

**Important:** When your ngrok HTTP URL changes, you must update:
- ✅ `BotBaseUrl` and `ServiceFqdn` in `appsettings.json`
- ✅ `Calling webhook` in Azure Bot Resource
- ✅ Restart your bot application for changes to take effect

### 9.1 Update Bot Messaging Endpoint

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Bot** (create one if you haven't)
3. Go to **Configuration**
4. Set **Messaging endpoint**: `https://your-ngrok-url.ngrok-free.app/api/calls`
5. Set **Calling webhook**: `https://your-ngrok-url.ngrok-free.app/api/calls`
6. Save changes

**Note:** Replace `your-ngrok-url.ngrok-free.app` with your actual ngrok HTTP tunnel URL from Step 4.4. This should match the `BotBaseUrl` you configured in `appsettings.json`.

---

## Step 10: Run the Bot

### 10.1 Restore Dependencies

```bash
cd /path/to/TeamsMediaBot
dotnet restore
```

### 10.2 Build the Project

```bash
dotnet build
```

### 10.3 Run the Bot

```bash
dotnet run
```

The bot should start on `http://0.0.0.0:5000`

### 10.4 Verify the Bot is Running

Open a browser and go to:
```
http://localhost:5000/api/health
```

You should see:
```json
{
  "status": "Healthy",
  "activeSessions": 0,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

---

## Step 11: Test the Bot

### 11.1 Upload the App to Teams

1. Open Microsoft Teams
2. Go to **Apps**
3. Click **Manage your apps**
4. Click **Upload an app** > **Upload a custom app**
5. Select your `TeamsMediaBot.zip` file
6. Click **Add**

### 11.2 Add the Bot to a Meeting

1. Schedule a Teams meeting
2. In the meeting options, add the bot as a participant
3. Start the meeting
4. The bot should automatically join when the meeting starts

### 11.3 Monitor the Logs

Watch the console output of your running bot. You should see:
- Call received notifications
- Session initialization
- Deepgram connection
- Transcript events
- Live summaries from Ktor

---

## Troubleshooting

### ngrok URL Changed / Bot Stopped Working After Restart

**Symptoms:**
- Bot was working but stopped receiving calls after restarting ngrok
- Call notifications not reaching the bot
- Bot joins but no audio flows

**Cause:**
- ngrok assigns new URLs when restarted (especially with free plan)
- Both HTTP tunnel URL and TCP tunnel port change
- Configuration files and Azure Bot settings become outdated

**Solution:**
1. Check your current ngrok tunnel URLs:
   - HTTP tunnel: `https://abc123.ngrok-free.app`
   - TCP tunnel remote port: `tcp://0.tcp.ngrok.io:12345` (note the port: 12345)

2. Update `appsettings.json`:
   ```json
   "Bot": {
     "BotBaseUrl": "https://abc123.ngrok-free.app",  // New HTTP URL
     "ServiceFqdn": "abc123.ngrok-free.app",          // New domain
     "InstancePublicPort": 12345                       // New TCP port
   }
   ```

3. Update Azure Bot Resource (Azure Portal):
   - Go to Azure Bot → Configuration
   - Set **Calling webhook**: `https://abc123.ngrok-free.app/api/calls`
   - Save changes

4. **Restart your bot application** (required for config changes to take effect)

5. Test by joining a meeting

**Prevention:**
- Use ngrok paid plan with reserved endpoints for consistent URLs
- Document your current ngrok URLs for quick reference

### Bot Doesn't Join the Meeting

1. **Check ngrok**: Ensure ngrok is still running and the URL hasn't changed
2. **Check permissions**: Verify all Graph API permissions are granted
3. **Check logs**: Look for errors in the bot console
4. **Check calling endpoint**: Verify it's set correctly in Azure Bot configuration

### TCP Port Mismatch (No Audio During Calls)

**Symptoms:**
- Bot successfully joins the meeting
- Bot appears in the participant list
- No audio is received by the bot
- Transcription doesn't start

**Cause:**
- `InstancePublicPort` in `appsettings.json` doesn't match ngrok's TCP remote port
- Teams is trying to connect to the wrong port for media streaming

**Solution:**
1. Check your ngrok TCP tunnel output:
   ```
   Forwarding   tcp://0.tcp.ngrok.io:12345 -> localhost:8445
   ```
   The remote port is **12345** in this example

2. Verify `appsettings.json` has the correct port:
   ```json
   "Bot": {
     "InstanceInternalPort": 8445,      // Should always be 8445 (local)
     "InstancePublicPort": 12345        // Must match ngrok remote port
   }
   ```

3. **Restart your bot application**

4. Join a new meeting and test

**How to Verify:**
- Look for logs showing media platform initialization with correct port
- Check bot startup logs for: `Media platform configured. PublicIP: X.X.X.X, Port: 12345`

### Understanding Port Confusion (8445 vs ngrok Port)

**Common Confusion:**
"My ngrok shows port 12345, but my config has port 8445. Which one is correct?"

**Answer: Both are correct, but serve different purposes:**

| Port | Purpose | Where Used | Changes? |
|------|---------|------------|----------|
| **8445** | Internal/Local | `InstanceInternalPort` | No |
| **12345** | External/Public | `InstancePublicPort` | Yes (with ngrok free) |
| **5000** | HTTP Signaling | Bot startup config | No |

**Clarification:**
- **Port 8445 (Internal)**: Your Media Platform SDK listens on this port locally. This never changes.
- **Port 12345 (Public)**: Teams connects to this port externally. ngrok forwards it to 8445. This changes every ngrok restart with free plan.
- **Port 5000 (HTTP)**: Separate port for webhook notifications. Not related to media streaming.

**Example Configuration:**
```json
"Bot": {
  "InstanceInternalPort": 8445,    // ← Always 8445 (your local listener)
  "InstancePublicPort": 12345      // ← From ngrok TCP output (changes)
}
```

**Visual Flow:**
```
Teams → 0.tcp.ngrok.io:12345 → ngrok → localhost:8445 → Your Bot
        (InstancePublicPort)              (InstanceInternalPort)
```

### Media Not Flowing

1. **Check certificate**: Ensure the certificate thumbprint is correct
2. **Check IP address**: Verify your public IP is correct
3. **Check firewall**: Ensure port 8445 is open and forwarded (if not using ngrok TCP tunnel)
4. **Check certificate permissions**: Run the icacls command from Step 2.3 again
5. **Check TCP port configuration**: See "TCP Port Mismatch" section above

### Transcription Not Working

1. **Check Deepgram API key**: Verify it's correct in appsettings.json
2. **Check audio format**: The bot uses PCM 16kHz - ensure it's compatible
3. **Check Deepgram quota**: Ensure you haven't exceeded your free tier limits

### Ktor Connection Issues

1. **Check Ktor is running**: Verify the Ktor server is accessible
2. **Check endpoints**: Test the Ktor endpoints manually with Postman/curl
3. **Check WebSocket**: Ensure the WebSocket endpoint is correct

### Certificate Errors

If you see certificate-related errors:
```powershell
# Re-grant permissions
$thumbprint = "YOUR_THUMBPRINT"
$cert = Get-ChildItem -Path cert:\LocalMachine\My\$thumbprint
$rsaCert = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
$path = $rsaCert.Key.UniqueName
icacls "C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\$path" /grant "Everyone:RX"
```

---

## Production Deployment

For production deployment:

1. **Use a valid SSL certificate** instead of self-signed
2. **Deploy to Azure App Service** or another hosting platform
3. **Use Azure Key Vault** for secrets management
4. **Configure Application Insights** for monitoring
5. **Set up proper firewall rules** for media ports
6. **Use a static public IP** instead of dynamic IP
7. **Implement request validation** in ClientCredentialAuthProvider
8. **Add retry logic** for external service calls
9. **Implement circuit breakers** for fault tolerance
10. **Set up health checks** and monitoring

---

## Additional Resources

- [Microsoft Graph Calling SDK Documentation](https://docs.microsoft.com/graph/api/resources/communications-api-overview)
- [Teams Bot Documentation](https://docs.microsoft.com/microsoftteams/platform/bots/what-are-bots)
- [Deepgram Documentation](https://developers.deepgram.com/docs)
- [ngrok Documentation](https://ngrok.com/docs)

---

## Support

For issues or questions:
- Check the logs in the bot console
- Review the [README.md](README.md) for architecture details
- Open an issue in the repository
