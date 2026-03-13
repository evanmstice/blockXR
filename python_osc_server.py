from pythonosc.dispatcher import Dispatcher
from pythonosc.osc_server import BlockingOSCUDPServer
from pythonosc.udp_client import SimpleUDPClient

def run_osc_server(detector):
    unity_client = SimpleUDPClient("127.0.0.1", 7001)

    def request_handler(address, *args):
        print("\nUnity requested block data")
        
        blocks = detector.get_blocks() 
        
        if not blocks:
            print("No blocks detected - sending empty list")
            unity_client.send_message("/program", [])
            return

        # block[0] is the name, block[1] is the confidence
        block_names = [block[0] for block in blocks]
        
        print(f"[OSC] Sending to Unity: {block_names}")
        unity_client.send_message("/program", block_names)

    dispatcher = Dispatcher()
    dispatcher.map("/req", request_handler)

    print("OSC Server Active on Port 31415")
    server = BlockingOSCUDPServer(("127.0.0.1", 31415), dispatcher)
    server.serve_forever()
