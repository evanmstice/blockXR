from pythonosc.dispatcher import Dispatcher
from pythonosc.osc_server import BlockingOSCUDPServer
from pythonosc.udp_client import SimpleUDPClient


class ReusableOSCUDPServer(BlockingOSCUDPServer):
    # Must be set on the class before bind (happens in __init__).
    allow_reuse_address = True


def run_osc_server(detector):
    unity_client = SimpleUDPClient("127.0.0.1", 7001)

    def request_handler(address, *args):
        print("\nUnity requested block data", flush=True)

        blocks = detector.get_blocks()

        if not blocks:
            print("No blocks detected - sending empty list", flush=True)
            unity_client.send_message("/program", [])
            return

        # block[0] is the name, block[1] is the confidence
        block_names = [block[0] for block in blocks]

        print(f"[OSC] Sending to Unity: {block_names}", flush=True)
        unity_client.send_message("/program", block_names)

    dispatcher = Dispatcher()
    dispatcher.map("/req", request_handler)

    print("OSC Server Active on Port 31415", flush=True)
    server = ReusableOSCUDPServer(("127.0.0.1", 31415), dispatcher)
    server.serve_forever()
