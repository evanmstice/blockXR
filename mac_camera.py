# Request macOS camera permission before OpenCV opens the device.
# Status: 0=notDetermined, 1=restricted, 2=denied, 3=authorized
#
# Do NOT create NSApplication here. As a Unity child that causes macOS to
# silently deny (completion ok=False, no dialog, no System Settings row).

from __future__ import annotations

import subprocess
import sys
import time


def log(msg: str) -> None:
    print(msg, flush=True)


def _open_camera_settings() -> None:
    try:
        subprocess.run(
            [
                "open",
                "x-apple.systempreferences:com.apple.preference.security?Privacy_Camera",
            ],
            check=False,
        )
    except Exception:
        pass


def request_camera_access(timeout: float = 180.0) -> bool:
    if sys.platform != "darwin":
        return True

    try:
        from AVFoundation import (  # type: ignore
            AVAuthorizationStatusAuthorized,
            AVAuthorizationStatusDenied,
            AVAuthorizationStatusNotDetermined,
            AVAuthorizationStatusRestricted,
            AVCaptureDevice,
            AVMediaTypeVideo,
        )
        from Foundation import NSDate, NSDefaultRunLoopMode, NSRunLoop  # type: ignore
    except ImportError as e:
        log(f"CV_BACKEND_CAMERA_REQUEST_SKIP no AVFoundation module: {e}")
        return True  # let OpenCV try anyway

    status = int(AVCaptureDevice.authorizationStatusForMediaType_(AVMediaTypeVideo))
    log(f"CV_BACKEND_CAMERA_STATUS {status}")

    if status == int(AVAuthorizationStatusAuthorized):
        return True

    if status in (int(AVAuthorizationStatusDenied), int(AVAuthorizationStatusRestricted)):
        log(
            "CV_BACKEND_ERROR camera: denied. Enable blockXR (or Python) under "
            "System Settings → Privacy & Security → Camera, then relaunch "
            "(or: tccutil reset Camera)"
        )
        _open_camera_settings()
        return False

    if status != int(AVAuthorizationStatusNotDetermined):
        log(f"CV_BACKEND_ERROR camera: unexpected status {status}")
        return False

    granted = [False]
    done = [False]

    def handler(ok: bool) -> None:
        granted[0] = bool(ok)
        done[0] = True

    log("CV_BACKEND_CAMERA_PROMPT")
    AVCaptureDevice.requestAccessForMediaType_completionHandler_(
        AVMediaTypeVideo, handler
    )

    deadline = time.time() + timeout
    run_loop = NSRunLoop.currentRunLoop()
    while not done[0] and time.time() < deadline:
        run_loop.runMode_beforeDate_(
            NSDefaultRunLoopMode,
            NSDate.dateWithTimeIntervalSinceNow_(0.25),
        )

    status_after = int(AVCaptureDevice.authorizationStatusForMediaType_(AVMediaTypeVideo))
    log(f"CV_BACKEND_CAMERA_STATUS_AFTER {status_after} granted={granted[0]}")

    if not done[0]:
        log(
            "CV_BACKEND_ERROR camera: permission prompt timed out — "
            "rebuild so Info.plist is re-signed, or run: tccutil reset Camera"
        )
        _open_camera_settings()
        return False

    # Silent deny: handler says no, but TCC never recorded a decision /
    # never showed UI — classic unsigned / unsealed Info.plist.
    if not granted[0] and status_after == int(AVAuthorizationStatusNotDetermined):
        log(
            "CV_BACKEND_ERROR camera: silent deny (no dialog). Rebuild the macOS "
            "app (post-build must re-sign Info.plist), then: tccutil reset Camera"
        )
        _open_camera_settings()
        return False

    if not granted[0]:
        log(
            "CV_BACKEND_ERROR camera: user denied the prompt. Enable blockXR under "
            "System Settings → Privacy & Security → Camera, then relaunch"
        )
        _open_camera_settings()
        return False

    log("CV_BACKEND_CAMERA_GRANTED")
    return True
