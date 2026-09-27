#       Main function to be run by Unity when game is started.
#       Creates different threads that initialize OSC server,
#       begin YoloAI detection in background, and provides a way
#       for Unity to call for this data whenever necessary

from detect import BlockDetector
import python_osc_server
import threading
import signal
import sys

if __name__ == "__main__":
    detector = BlockDetector(debug=False)
    detector.start()

    # Run OSC server in main thread
    server_thread = threading.Thread(target=python_osc_server.run_osc_server, args=(detector,), daemon=True)
    server_thread.start()

    stop_event = threading.Event()

    def signal_handler(sig, frame):
        print("\nShutting down...")
        stop_event.set()

    signal.signal(signal.SIGINT, signal_handler)
    if hasattr(signal, "SIGTERM"):
        signal.signal(signal.SIGTERM, signal_handler)

    # keep main thread alive
    try:
        stop_event.wait()
    except KeyboardInterrupt:
        pass
    finally:
        detector.stop()
        sys.exit(0)
