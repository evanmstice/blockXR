from pythonosc.dispatcher import Dispatcher
from pythonosc import osc_server
from pythonosc.udp_client import SimpleUDPClient

def run_osc_server(detector):

    # sends data to unity
    def req_handler(addr, *args):
        blocks = detector.getBlocks()
        client = SimpleUDPClient('127.0.0.1', 7001)
        client.send_message("/program", blocks)

    dispatcher = Dispatcher()
    # if an osc message is received on address /req, call req_handler
    dispatcher.map("/req", req_handler)

    # creates a server that listens for message on the given address on the given port
    server = osc_server.ThreadingOSCUDPServer(("127.0.0.1", 31415), dispatcher)
    print("Serving on {}".format(server.server_address))
    server.serve_forever()
