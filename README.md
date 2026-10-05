# blockXR

BlockXR turns block programming into something you can hold and play. Guided by narrative storytelling, it supports programming education and cognitive development with a tangible interface: place physical blocks on a mat, and computer vision detects them so they drive a 2D Unity game projected onto that same surface.

We built it to study how an interactive, story-driven programming experience affects cognitive abilities — designed for low cognitive load so it stays approachable without losing engagement.

The Unity game talks to a webcam-based YOLO detector over OSC.

## Prerequisites

- [Unity](https://unity.com/download) **6000.1.3f1** (see `ProjectSettings/ProjectVersion.txt`)
- Python 3.12+ on PATH (`python3 --version`)
- Webcam + blockXR physical platform



## Setup (once per machine)

From this project folder:

```bash
python3 -m venv .venv
source .venv/bin/activate          # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

Dependencies stay in the project `.venv`. Keep the Unity build under `Builds/` inside this same folder so the app can find `main.py`.

## Build

Open the project in Unity, then:

- **Build → Build macOS** → `Builds/macOS/`
- **Build → Build Windows** → `Builds/Windows/`

Opening the built app starts the CV backend automatically and stops it on quit. Press **Esc** to quit.

## How it connects


| Direction      | Host        | Port    | Address    |
| -------------- | ----------- | ------- | ---------- |
| Unity → Python | `127.0.0.1` | `31415` | `/req`     |
| Python → Unity | `127.0.0.1` | `7001`  | `/program` |


Manual backend (optional, uses OpenCV camera in Python directly): `source .venv/bin/activate && python main.py`

## Usage

Once setup is complete, run the build you created from the `Builds` folder.

### First launch on macOS

1. If prompted, select this project folder (located in the parent directory where you cloned this repo).
2. When macOS asks for **camera** access for **blockXR**, click **Allow**.
  - Unity captures the webcam; Python only runs YOLO on those frames.
3. If you denied it earlier: **System Settings → Privacy & Security → Camera → blockXR**.

Windows: Python opens the webcam directly (no extra Camera toggle for a Unity wrapper).

### Gameplay

Currently, a mouse must be used throughout gameplay to click the UI buttons (RUN, TRY AGAIN, etc). Future development is planned to remove this dependency and instead advance the game based on gestures or other physical interface features. Clicking the run button captures the current blocks detected via the webcam and passes these to the Unity frontend.