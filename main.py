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

    # Shutdown on Ctrl+C
    def signal_handler(sig, frame):
        print("\nShutting down...")
        detector.stop()
        sys.exit(0)

    signal.signal(signal.SIGINT, signal_handler)

    # Keep main thread alive
    signal.pause()

