# blockXR

Unity game that reads physical coding blocks via a webcam (YOLO) over OSC.

## Prerequisites

- [Unity](https://unity.com/download) **6000.1.3f1** (see `ProjectSettings/ProjectVersion.txt`)
- Python 3
- blockXR physical platform

## Setup

Install Python dependencies (from the repo root):

```bash
pip install -r requirements.txt
```

## Build

Open the project in Unity, then:

- **Build → Build macOS** → writes to `Builds/macOS/`
- **Build → Build Windows** → writes to `Builds/Windows/`

Or use **File → Build Settings** and set the output folder to `Builds/macOS` or `Builds/Windows`.

Keep the build inside this project folder so the app can find `main.py`. Opening the built executable starts the CV backend automatically and stops it when you quit.

## How it connects


| Direction      | Host        | Port    | Address    |
| -------------- | ----------- | ------- | ---------- |
| Unity → Python | `127.0.0.1` | `31415` | `/req`     |
| Python → Unity | `127.0.0.1` | `7001`  | `/program` |


Manual backend (optional): `python main.py`