#       Main function to be run by Unity when game is started.
#       Creates different threads that initialize OSC server,
#       begin YoloAI detection in background, and provides a way
#       for Unity to call for this data whenever necessary

from detect import BlockDetector
import python_osc_server
import os
import threading
import signal
import sys
import time


def log(msg):
    print(msg, flush=True)


if __name__ == "__main__":
    log("CV_BACKEND_STARTING")

    # On macOS, Unity owns the webcam (BLOCKXR_FRAME_PATH). Python only runs YOLO.
    # Elsewhere, OpenCV opens the camera directly.
    frame_path = os.environ.get("BLOCKXR_FRAME_PATH", "").strip() or None
    debug_frame_path = os.environ.get("BLOCKXR_DEBUG_FRAME", "").strip() or None

    try:
        if frame_path:
            log(f"CV_BACKEND_FRAME_SOURCE {frame_path}")
            deadline = time.time() + 60.0
            while not os.path.isfile(frame_path):
                if time.time() > deadline:
                    raise RuntimeError(
                        f"timed out waiting for Unity webcam frames at {frame_path}"
                    )
                time.sleep(0.2)
            detector = BlockDetector(
                debug=False,
                frame_path=frame_path,
                debug_frame_path=debug_frame_path,
            )
        else:
            if sys.platform == "darwin":
                from mac_camera import request_camera_access

                if not request_camera_access():
                    sys.exit(1)
            detector = BlockDetector(debug=False, debug_frame_path=debug_frame_path)
            if detector.cap is None or not detector.cap.isOpened():
                raise RuntimeError("webcam failed to open (isOpened=False)")

        if debug_frame_path:
            log(f"CV_BACKEND_DEBUG_FRAME {debug_frame_path}")

        detector.start()
        log("CV_BACKEND_CAMERA_OK")
    except Exception as e:
        log(f"CV_BACKEND_ERROR camera: {e}")
        sys.exit(1)

    server_thread = threading.Thread(
        target=python_osc_server.run_osc_server,
        args=(detector,),
        daemon=True,
    )
    server_thread.start()
    log("CV_BACKEND_READY")

    stop_event = threading.Event()

    def signal_handler(sig, frame):
        log("CV_BACKEND_SHUTDOWN")
        stop_event.set()

    signal.signal(signal.SIGINT, signal_handler)
    if hasattr(signal, "SIGTERM"):
        signal.signal(signal.SIGTERM, signal_handler)

    try:
        stop_event.wait()
    except KeyboardInterrupt:
        pass
    finally:
        detector.stop()
        sys.exit(0)
