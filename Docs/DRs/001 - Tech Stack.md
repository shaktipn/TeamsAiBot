# Tech Stack

## Date

2025-12-04

## Context

- For this project in phase 1, we need to provide a bot that can tap into the audio of a given Teams meeting, transcribe it, and provide users with a way to access real-time summary.
- For this purpose we can go with either service hosted or application hosted media.
- If we go with service hosted media, we can keep our entire backend tech stack limited to Kotlin as Microsoft provides Java SDKs to use the APIs for this purpose.
- If we go with application hosted media, we need to use the .NET framework along with hosting on a Windows machine. [Reference](https://learn.microsoft.com/en-us/microsoftteams/platform/bots/calls-and-meetings/calls-meetings-bots-overview)
- [Official comparison](https://learn.microsoft.com/en-us/graph/cloud-communications-media) of service hosted and application hosted media.


## Decisions

- We are going with application hosted media.

## Reasons

- Application-hosted media is the appropriate choice for real-time transcription because:
    - It provides direct access to raw audio streams with minimal latency.
    - Service-hosted media adds processing delays through Microsoft's infrastructure, which conflicts with real-time requirements. (We need to call an api that gives us an audio recording file which we need to download and we need to keep doing this on loop.)
    - Microsoft explicitly recommends application-hosted for scenarios requiring immediate audio processing as mentioned in the documentation provided in [context](#context).
    - Service-hosted media is designed for simpler scenarios (IVR, recording) not real-time transcription. [API reference](https://learn.microsoft.com/en-us/graph/api/call-record?view=graph-rest-1.0&tabs=http)

## Consequences

- We will have to use the .NET framework for a part of the WS code and communicate with the Ktor server as necessary.
- We will have to use a Windows machine to run the project because the Microsoft Graph Media SDK needs the Windows Media Format specific DLL files to work as expected.
- Development can become complex as the bot can only run on a Windows machine. But we can potentially host the the entire .NET project in a windows server - SSH into it can use it from there.

## Alternatives Considered

- Using service hosted media to keep the entire web service in Ktor server.
- Using only .NET framework for the entire web service.

## People Involved

- Shakti Prasad Nanda <shakti.pn@surya-digital.com>
- Rahul J <rahul.j@surya-digital.com>
