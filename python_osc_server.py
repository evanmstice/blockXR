from pythonosc.dispatcher import Dispatcher
from pythonosc import osc_server
from pythonosc.udp_client import SimpleUDPClient

import cv2
from detect import detect_blocks

def req_handler(addr, *args):
    
    blocks = detect_blocks()
    blockNames = [block[0] for block in blocks]
    print(blocks)
    # blocks = ["move_forward", "turn_left", "turn_right"]

    client = SimpleUDPClient('127.0.0.1', 7001)
    client.send_message("/program", blockNames)


dispatcher = Dispatcher()
# if an osc message is received on address /req, call req_handler
dispatcher.map("/req", req_handler)

# creates a server that listens for message on the given address on the given port
server = osc_server.ThreadingOSCUDPServer(("127.0.0.1", 31415), dispatcher)
print("Serving on {}".format(server.server_address))
server.serve_forever()
