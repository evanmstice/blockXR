# Solves VS Code issue with imports
import sys
import os
project_root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if project_root not in sys.path:
    sys.path.insert(0, project_root)

import cv2
import time
from ultralytics import YOLO
from predict import getBlocks

# Creates a table that updates with sanitized block data
# Overlays onto live cv2 view
def make_table(frame, blocks):
    if not blocks:
        return frame

    # Table placement
    x_start = 30
    y_start = 40
    row_height = 25
    col1_width = 150
    col2_width = 100

    # Calculate table height
    table_height = (len(blocks) + 1) * row_height + 10
    table_width = col1_width + col2_width + 20

    # Background rectangle
    overlay = frame.copy()
    cv2.rectangle(overlay, (x_start - 10, y_start - 25), (x_start + table_width, y_start + table_height), (0, 0, 0), -1)
    alpha = 0.6
    frame = cv2.addWeighted(overlay, alpha, frame, 1 - alpha, 0)

    # Table header
    cv2.putText(frame, "Block", (x_start, y_start), cv2.FONT_HERSHEY_DUPLEX, 0.6, (0, 255, 255), 2)
    cv2.putText(frame, "Confidence", (x_start + col1_width, y_start), cv2.FONT_HERSHEY_DUPLEX, 0.6, (255, 0, 255), 2)

    # Rows
    current_y = y_start
    for i, item in enumerate(blocks):
        label = str(item[0])
        confidence = f"{float(item[1]):.2f}"
        current_y = y_start + (i + 1) * row_height

        # populate row with block data
        cv2.putText(frame, label, (x_start, current_y), cv2.FONT_HERSHEY_DUPLEX, 0.6, (255, 255, 255), 2)
        cv2.putText(frame, confidence, (x_start + col1_width, current_y), cv2.FONT_HERSHEY_DUPLEX, 0.6, (255, 255, 255), 2)

    # Info screen
    info_text = "The overlay boxes represent raw YOLO data. \nThis table represents sanitized block data. \nPress 'q' to quit."
    lines = info_text.split("\n")
    for i, line in enumerate(lines):
        cv2.putText(frame, line, (x_start - 5, current_y + i * 25 + 60), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (150, 200, 255), 1)  

    return frame

# Fixes window resizing issues on higher resolution screens
def rescale_frame(frame, percent=75):
    width = int(frame.shape[1] * percent/ 100)
    height = int(frame.shape[0] * percent/ 100)
    dim = (width, height)
    return cv2.resize(frame, dim, interpolation =cv2.INTER_AREA)

# Loading the model we have trained - RM
model = YOLO("block_weights.pt")

if __name__ == "__main__":
    # Video feed initialization and cleanup
    cap = cv2.VideoCapture(0)    
    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.75) # enable auto exposure
    time.sleep(1.0)  # give the camera a moment to adjust
    auto_exposure_value = cap.get(cv2.CAP_PROP_EXPOSURE)
    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.25)  # turn off auto
    offset = 0.25
    cap.set(cv2.CAP_PROP_EXPOSURE, auto_exposure_value + offset) # setting offset exposure value

    # Camera feed updates with block detection boxes and live table
    while True:
        ret, frame = cap.read()
        if not ret:
            break
        
        # Video feed cleanup
        frame = cv2.flip(frame, -1)
        frame = rescale_frame(frame)

        results = model(frame, stream=True, verbose=False)
        blocks = []

        for r in results:
            boxes = r.boxes
            for box in boxes:
                x1, y1, x2, y2 = box.xyxy[0]
                x1, y1, x2, y2 = int(x1), int(y1), int(x2), int(y2)

                conf = box.conf[0].item()
                cls = int(box.cls[0].item())
                label = model.names[cls]

                cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
                cv2.putText(frame, f"{label} {conf:.2f}", (x1, y1-10), cv2.FONT_HERSHEY_PLAIN, 0.7, (0, 255, 0), 2)

            try:
                blocks = getBlocks(boxes)
            except KeyError:
                pass

        frame = make_table(frame, blocks)

        cv2.imshow("Live Feed", frame)

        key = cv2.waitKey(1) & 0xFF
        if key == ord('q'):
            break

    # cleanup
    cap.release()
    cv2.destroyAllWindows()
