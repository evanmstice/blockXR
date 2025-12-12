import cv2
from ultralytics import YOLO
from predict import getBlocks
from time import sleep

model = YOLO("block_weights.pt")

# this function is used to detect blocks and only runs when player hits run code button on unity

def detect_blocks():
    cap = cv2.VideoCapture(0)
    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.75)
    # sleep(5) # allow camera to adjust
    ret, frame = cap.read()
    cap.release()
    results = model(frame, stream=True, verbose=False)
    blocks =[]

    if results:
        for r in results:
            try:
                blocks = getBlocks(r.boxes)
                return blocks
            except KeyError as e:
                print(e)
                return []

    print("Blocks detected: ", blocks)
    return blocks
    