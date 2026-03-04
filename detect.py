import cv2
import time
import threading
from ultralytics import YOLO

model = YOLO("block_weights.pt")
confidence = 0.7

"""CLASSES + THREADING"""

class Block:
    def __init__(self, name, xPos, yPosTop, yPosBottom, confidence):
        self.name = name
        self.xPos = xPos
        self.yPosTop = yPosTop
        self.yPosBottom = yPosBottom
        self.confidence = confidence

class BlockDetector:
    def __init__(self, debug=False):
        self.debug = debug
        self.cap = cv2.VideoCapture(0)
        configure_camera(self.cap)

        self.running = False
        self.latest_blocks = []
        self.latest_frame = None
        self.lock = threading.Lock()

    def start(self):
        self.running = True
        threading.Thread(target=self._run, daemon=True).start()

    def stop(self):
        self.running = False
        self.cap.release()
        cv2.destroyAllWindows()

    def get_blocks(self):
        with self.lock:
            return self.latest_blocks.copy()

    def get_frame(self):
        # Return the latest frame
        with self.lock:
            return self.latest_frame.copy() if self.latest_frame is not None else None

    def _run(self):
        while self.running:
            ret, frame = self.cap.read()
            if not ret:
                continue

            results = model(frame, verbose=False, conf=confidence)

            blocks = []
            for r in results:
                try:
                    blocks = getBlocks(r.boxes)
                except KeyError:
                    blocks = []

            with self.lock:
                self.latest_blocks = blocks

                if self.debug:
                    # Make a copy of capture for drawing
                    frame_copy = frame.copy()

                    # Draw raw YOLO boxes
                    for r in results:
                        for box in r.boxes:
                            x1, y1, x2, y2 = map(int, box.xyxy[0])
                            conf = box.conf[0].item()
                            cls = int(box.cls[0].item())
                            label = model.names[cls]

                            cv2.rectangle(frame_copy, (x1, y1), (x2, y2), (0, 255, 0), 2)
                            cv2.putText(frame_copy,
                                        f"{label} {conf:.2f}",
                                        (x1, y1 - 10),
                                        cv2.FONT_HERSHEY_PLAIN,
                                        0.7,
                                        (0, 255, 0),
                                        1)

                    # Overlay sanitized table
                    frame_copy = make_table(frame_copy, blocks)
                    frame_copy = rescale_frame(frame_copy, 75)

                    self.latest_frame = frame_copy

""" DEBUG WINDOW FUNCTIONS """

def configure_camera(cap):
    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.75)
    time.sleep(1.0)

    auto_exposure_value = cap.get(cv2.CAP_PROP_EXPOSURE)

    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.25)
    offset = 0.25
    cap.set(cv2.CAP_PROP_EXPOSURE, auto_exposure_value + offset)

# Fixes window resizing issues on higher resolution screens
def rescale_frame(frame, percent=75):
    width = int(frame.shape[1] * percent/ 100)
    height = int(frame.shape[0] * percent/ 100)
    dim = (width, height)
    return cv2.resize(frame, dim, interpolation =cv2.INTER_AREA)

def make_table(frame, blocks):

    if not blocks:
        return frame

    x_start = 30
    y_start = 40
    row_height = 25
    col1_width = 150
    col2_width = 100

    table_height = (len(blocks) + 1) * row_height + 10
    table_width = col1_width + col2_width + 20

    overlay = frame.copy()
    cv2.rectangle(
        overlay,
        (x_start - 10, y_start - 25),
        (x_start + table_width, y_start + table_height),
        (0, 0, 0),
        -1
    )

    frame = cv2.addWeighted(overlay, 0.6, frame, 0.4, 0)

    cv2.putText(frame, "Block", (x_start, y_start),
                cv2.FONT_HERSHEY_DUPLEX, 0.6, (0, 255, 255), 1)

    cv2.putText(frame, "Confidence", (x_start + col1_width, y_start),
                cv2.FONT_HERSHEY_DUPLEX, 0.6, (255, 0, 255), 1)

    for i, item in enumerate(blocks):
        current_y = y_start + (i + 1) * row_height
        label = str(item[0])
        confidence = f"{float(item[1]):.2f}"

        cv2.putText(frame, label, (x_start, current_y),
                    cv2.FONT_HERSHEY_DUPLEX, 0.6, (255, 255, 255), 1)

        cv2.putText(frame, confidence, (x_start + col1_width, current_y),
                    cv2.FONT_HERSHEY_DUPLEX, 0.6, (255, 255, 255), 1)

    info_lines = [
        "The camera feed shows raw YOLO AI data",
        "This table show sanitized block data",
        "Press 'q' to quit"
    ]

    for i, line in enumerate(info_lines):
        cv2.putText(frame, line,
                    (x_start - 5, y_start + table_height + 30 + i * 20),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5,
                    (150, 200, 255), 1)

    return frame

"""BLOCK DATA ORDERING"""

def updateToleranceY(blocks):
    if blocks:
        height = blocks[0].yPosBottom - blocks[0].yPosTop
        return int(height * 1.1)
    return 20


def updateToleranceX(blocks):
    if blocks:
        height = blocks[0].yPosBottom - blocks[0].yPosTop
        return int(height * 1.1)
    return 100

def getBlocks(data):
    blocks = []

    for block in data:
        coordinates = block.xyxy.tolist()[0]
        xPos = coordinates[0] + (coordinates[2] - coordinates[0]) / 2

        blocks.append(
            Block(
                model.names[int(block.cls[0].item())],
                xPos,
                coordinates[1],
                coordinates[3],
                block.conf[0].item()
            )
        )

    if not blocks:
        return []

    blocks.sort(key=lambda b: b.yPosTop)

    toleranceY = updateToleranceY(blocks)
    toleranceX = updateToleranceX(blocks)

    codeBlocksFinal = []
    
    currXPos = blocks[0].xPos
    currYPos = blocks[0].yPosBottom

    for block in blocks:

        if (
            (currYPos - toleranceY <= block.yPosTop <= currYPos + toleranceY)
            and
            (currXPos - toleranceX <= block.xPos <= currXPos + toleranceX)
        ):
            codeBlocksFinal.append([block.name, block.confidence])
            currYPos = block.yPosBottom
            currXPos = block.xPos
        else:
            break

    return codeBlocksFinal

# Running this script directly will enter the visual debugger
if __name__ == "__main__":
    detector = BlockDetector(debug=True)
    detector.start()

    while detector.running:
        frame = detector.get_frame()
        if frame is not None:
            cv2.imshow("Live Feed", frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            detector.stop()
            break
