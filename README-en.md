# A2DP Commander

A free Windows utility for managing Bluetooth audio profiles (A2DP/HFP). Solves the problem of incorrect automatic profile switching in Windows.

**Version:** 1.0.1 | **Languages:** Русский, English

---

## Table of Contents

- [The Problem](#the-problem)
- [The Solution](#the-solution)
- [AAC Codec Issues on Intel Adapters](#aac-codec-issues-on-intel-adapters)
- [Important: Wait After Connection](#important-wait-after-connection)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Building from Source](#building-from-source)
- [Usage](#usage)
- [Settings](#settings)
- [License](#license)
- [Authors](#authors)
- [Support the Project](#support-the-project)

---

## The Problem

When you connect Bluetooth headphones to Windows, the operating system creates **two separate audio devices**:

1. **A2DP (Advanced Audio Distribution Profile)** - High quality stereo audio
   - 44.1/48 kHz sample rate
   - Stereo sound
   - Supports high-quality codecs (AAC, aptX, LDAC)
   - **No microphone support**

2. **HFP (Hands-Free Profile)** - Low quality audio with microphone
   - 8/16 kHz sample rate
   - Mono sound
   - Uses basic CVSD/mSBC codec
   - **Includes microphone support**

### Why This Is a Problem

Windows automatically switches between these profiles, and it often makes the wrong decision:

- You start listening to music in high-quality A2DP mode
- An application requests microphone access (even if you don't need it)
- Windows silently switches to HFP mode
- Your music suddenly sounds terrible - muffled, mono, low quality
- You have no idea why this happened

This happens constantly with:
- Video conferencing apps (Zoom, Teams, Discord) that request mic access
- Games with voice chat features
- Any application that might potentially use a microphone
- Windows system sounds triggering unnecessary profile switches

---

## The Solution

A2DP Commander solves this problem by **disabling the HFP device in Windows Device Manager**. This forces Windows to use only the A2DP profile, ensuring consistently high audio quality.

### How It Works

1. The program detects your connected Bluetooth audio device
2. It identifies both the A2DP and HFP endpoints
3. Using Windows SetupAPI, it **programmatically disables the HFP device**
4. Windows can now only use A2DP, guaranteeing high-quality audio

### Two Operating Modes

| Mode | A2DP | HFP | Use Case |
|------|------|-----|----------|
| **Music** | Enabled | Disabled | Listening to music, watching videos |
| **Calls** | Enabled | Enabled | Voice calls, video conferencing |

You can switch between modes:
- Automatically based on running applications
- Manually via system tray menu

---

## AAC Codec Issues on Intel Adapters

Many Intel Bluetooth adapters have problems with the AAC codec, causing:

- Audio stuttering and dropouts
- Crackling and popping sounds
- Intermittent connection issues
- Audio desynchronization

### The Fix

A2DP Commander allows you to **disable AAC codec via Windows Registry**:

**Registry Path:**
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BthA2dp\Parameters
```

**Value:**
```
BluetoothAacEnable = 0 (DWORD)
```

When AAC is disabled, Windows will fall back to SBC codec, which may have slightly lower quality but provides stable, stutter-free playback.

### IMPORTANT: Restart Required

**Changing the AAC setting requires a full computer restart to take effect.** The Bluetooth stack reads this registry value only during system startup. Simply reconnecting your headphones or restarting the Bluetooth service will NOT apply the change.

In version 1.0.1, the "Restart Bluetooth" feature was removed as it could cause the Bluetooth adapter to become disabled. The program now offers a safe system reboot to apply AAC changes.

---

## Important: Wait After Connection

After your Bluetooth device connects, **wait 3-5 seconds** before the automatic mode switch takes effect.

This delay is necessary because:
1. Windows needs time to fully enumerate the audio endpoints
2. The Bluetooth stack needs to complete the connection handshake
3. The audio drivers need to initialize both A2DP and HFP endpoints

If you try to play audio immediately after connection, you might experience a brief switch as A2DP Commander applies your preferred mode.

---

## System Requirements

### Operating System
- Windows 10 version 1903 (build 18362) or newer
- Windows 11 (any version)

### Runtime
- .NET 8 Desktop Runtime ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))

### Hardware
- Bluetooth adapter (built-in or USB)
- Bluetooth headphones or speakers with A2DP support

### Permissions
- **Administrator rights** are required to enable/disable audio devices via SetupAPI

---

## Installation

### From Release (Recommended)

1. Download the latest release from the [Releases](https://github.com/Yumash/A2DP-Commander/releases) page
2. Choose your version:
   - `A2DP-Commander-vX.X.X-win-x64.zip` - Requires .NET 8 Runtime installed (~9 MB)
   - `A2DP-Commander-vX.X.X-win-x64-self-contained.zip` - Includes .NET Runtime (~66 MB)
3. Extract the archive to your preferred location
4. Run `A2DP-Commander.exe`
5. (Optional) Enable "Start with Windows" in settings

---

## Building from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10 or Windows 11 (WPF requires Windows)
- Git

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/Yumash/A2DP-Commander.git
cd A2DP-Commander

# Build (Debug)
dotnet build src/A2DPCommander/A2DPCommander.csproj

# Run
dotnet run --project src/A2DPCommander/A2DPCommander.csproj
```

### Release Build

```bash
# Standard release build
dotnet build src/A2DPCommander/A2DPCommander.csproj -c Release

# Self-contained single-file executable (includes .NET Runtime, ~66 MB)
dotnet publish src/A2DPCommander/A2DPCommander.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Framework-dependent single-file executable (requires .NET Runtime, ~9 MB)
dotnet publish src/A2DPCommander/A2DPCommander.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## Usage

### Main Interface

After launch, the program minimizes to the system tray. Available actions:

- **Left click** on icon - open main window
- **Right click** on icon - context menu with quick mode switching
- **Double click** - open main window

### Tray Icon

The icon changes based on state:
- Blue circle - "Music" mode, A2DP active
- Green circle - "Calls" mode, HFP active
- Gray circle - device not connected

### Automatic Switching

The program can automatically switch to "Calls" mode when certain applications are running:
- Zoom
- Microsoft Teams
- Discord
- Skype
- Google Meet
- And others (configurable list)

---

## Settings

Settings are stored in `settings.json` file in the same folder as the program.

### Main Parameters

| Parameter | Description |
|-----------|-------------|
| Default device | Bluetooth device to manage |
| Default mode | Music or Calls on startup |
| Auto-start | Launch program on Windows startup |
| Notifications | Show notifications on mode switch |
| App-based switching | Switch mode when Zoom etc. starts |
| Disable AAC | Disable AAC codec (requires restart) |

### Application Rules

You can configure which mode to use when specific applications are running. Rules have priorities - if multiple applications are running, the rule with highest priority applies.

---

## License

This project is licensed under the **MIT License**.

You are free to use, modify, and distribute this software. See the [LICENSE](LICENSE) file for details.

---

## Authors

- **Andrey Yumashev** - [github.com/Yumash](https://github.com/Yumash)
- **Claude** (Anthropic)

---

## Support the Project

If you find A2DP Commander useful, consider supporting the development:

**BTC:** `1BkYvFT8iBVG3GfTqkR2aBkABNkTrhYuja`

Any support helps develop the project and add new features!

---

**Thank you for using A2DP Commander!**
