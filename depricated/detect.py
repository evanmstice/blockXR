import cv2
from ultralytics import YOLO
from predict import getBlocks

model = YOLO("block_weights.pt")

# this function is used to detect blocks and only runs when player hits run code button on unity

def detect_blocks():
    cap = cv2.VideoCapture(0)
    while True:
        ret, frame = cap.read()
        cap.release()
        results = model(frame, stream=True, verbose=False)
        blocks =[]

        for r in results:
            for box in r.boxes:
                pass
            try:
                blocks = getBlocks(r.boxes)
            except:
                blocks =[]

        print("Blocks detected: ", blocks)
        return blocks
        