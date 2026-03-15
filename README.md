# LiveSubs

**LiveSubs** is a real‑time subtitle and translation overlay for Windows.  
It listens to Windows **Live Captions**, translates the recognized text using your chosen API (LLM or traditional), and shows beautiful subtitles on top of any app or video.


---

## ✨ Features

- **Real‑time subtitles + translation**
  - Uses Windows **Live Captions** for speech recognition.
  - Sends recognized text to a translation API and displays the translated result.
- **Overlay window (LiveSubs bar)**
  - Always‑on‑top, resizable, draggable overlay.
  - Shows original captions and translations; you can switch order and hide one side.
- **Multiple translation backends**
  - Built‑in support for:
    - Google / Google2
    - OpenAI / OpenRouter
    - DeepL
    - Youdao
    - Baidu
    - Ollama
    - LM Studio
    - LibreTranslate
    - Custom MTranServer
- **History logging (optional)**
  - Can log translations into a local SQLite DB and browse them in a history page.
  - This is now **user‑controllable** via the “Save Translation History” toggle.
- **Appearance controls**
  - Change font size, boldness, stroke (outline), foreground color.
  - Change overlay background color and opacity.
  - Choose how many previous sentences to show.
- **Context‑aware translation**
  - Optionally send several previous sentences to the LLM for better context.
- **Per‑user settings**
  - All settings are persisted in `%AppData%\LiveSubs\setting.json` (no more `setting.json` on your desktop).

---

## 📦 Download

The official builds for **LiveSubs** are published here:

- **Releases**: https://github.com/Diva143V/LiveSubs/releases

You’ll usually find:

- A **self‑contained single `.exe`** for Windows x64 (`LiveSubs.exe`), which you can run without installing .NET.
- Release notes describing what changed.

---

## 🧰 Requirements

- **OS**: Windows 11 (Live Captions is built‑in; some features may work on newer Windows 10 builds with Live Captions).
- **Runtime**:
  - If you download a **self‑contained** `.exe` from Releases → **no additional runtime required**.
  - If you build from source as **framework‑dependent**, you need:
    - [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/)

- **Live Captions**:  
  - Windows Settings → Accessibility → **Live captions** must be available and enabled.

---

## 🚀 Quick Start

1. **Enable Windows Live Captions**
   - Open *Settings → Accessibility → Captions → Live captions*.
   - Turn **Live captions** on and choose your source language.

2. **Download LiveSubs**
   - Go to [Releases](https://github.com/Diva143V/LiveSubs/releases).
   - Download the latest `LiveSubs.exe` (self‑contained build).

3. **Run LiveSubs**
   - Double‑click `LiveSubs.exe`.
   - The main window will appear along with an optional overlay window.

4. **Configure translation**
   - Open the **Settings** page in LiveSubs.
   - Choose a **Translate API** (e.g. Google, OpenAI, etc.).
   - Configure API keys / endpoints where required.
   - Set **Target Language** (e.g. `en-US`, `ja-JP`, etc.).

5. **Use the overlay**
   - Enable the **Overlay Window** from the main UI if needed.
   - Position it at the bottom/top of your screen.
   - Use the control bar on hover to adjust font size, colors, background opacity, etc.

---

## ⚙️ Settings & Storage

LiveSubs stores per‑user configuration here:

- **Settings file**:  
  `%AppData%\LiveSubs\setting.json`

Contents include:

- Selected API (`ApiName`) and target language.
- Per‑API configurations (`Configs`, `ConfigIndices`).
- Window positions and sizes for:
  - Main window
  - Overlay window
- Overlay appearance:
  - Font size, bold mode, stroke thickness.
  - Foreground color.
  - Background color and opacity.
- Boolean flags:
  - `ContextAware`
  - `EnableTranslationHistory`
  - And others used in the main UI.

> If `EnableTranslationHistory` is turned **off**, the `translation_history.db` SQLite file will **not** be created or written.

---

## 🛠 Building from Source

1. **Clone the repo**

```bash
git clone https://github.com/Diva143V/LiveSubs.git
cd LiveSubs