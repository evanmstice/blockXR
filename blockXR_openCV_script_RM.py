import cv2
import time
import os
from ultralytics import YOLO
import predict
from rich.live import Live
from rich.table import Table
from predict import getBlocks

def make_table(blocks):
    table = Table(title="Detected Blocks")
    table.add_column("Block", justify="center", style="cyan")
    table.add_column("Confidence", justify="center", style="magenta")

    for item in blocks:
        label = str(item[0])
        confidence = f"{float(item[1]):.2f}"
        table.add_row(label, confidence)
    return table

# Loading the model we have trained - RM
model = YOLO("block_weights.pt")


# change camera resolution
def make_max_res(cap):
    cap.set(3, 1920)
    cap.set(4, 1440)

def make_1080p(cap):
    cap.set(3, 1920)
    cap.set(4, 1080)

def make_480p(cap):
    cap.set(3, 640)
    cap.set(4, 480)

def change_res(cap, width, height):
    cap.set(3, width)
    cap.set(4, height)

# adjust viewport size on high resolutions
def rescale_frame(frame, percent=75):
    width = int(frame.shape[1] * percent/ 100)
    height = int(frame.shape[0] * percent/ 100)
    dim = (width, height)
    return cv2.resize(frame, dim, interpolation =cv2.INTER_AREA)

if __name__ == "__main__":
    print("Press 'q' to quit. Press 'p' to process the current frame.")
    cap = cv2.VideoCapture(0)
    #cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.75) # Auto-exposure activation
    
    # Step 1: Enable auto exposure temporarily
    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.75)
    time.sleep(1.0)  # give the camera a moment to adjust

    # Step 2: Read the automatically determined exposure value
    auto_exposure_value = cap.get(cv2.CAP_PROP_EXPOSURE)
    print("Auto exposure value:", auto_exposure_value)

    # Step 3: Switch to manual mode and apply an offset
    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.25)  # turn off auto
    offset = 0.5
    cap.set(cv2.CAP_PROP_EXPOSURE, auto_exposure_value + offset)

    
    make_max_res(cap)

    with Live(make_table([]), refresh_per_second=4, screen=False) as live:
        while True:
            file_counter = 1
            ret, frame = cap.read()
            if not ret:
                break

            #frame = cv2.rotate(frame, cv2.ROTATE_180) # frame flip since webcam is upside down
            frame = rescale_frame(frame)

            # running yolo detection on the frame - RM
            results = model(frame, stream = True, verbose=False)

            # creates the boxes around each detected object - RM
            blocks = []
            for r in results:
                boxes = r.boxes
                for box in boxes:
                    # getting the coordinates of the box - RM
                    x1, y1, x2, y2 = box.xyxy[0]
                    x1, y1, x2, y2 = int(x1), int(y1), int(x2), int(y2)
                    # confidence of the object detection - RM
                    conf = box.conf[0].item()
                    # class of the object detected - RM
                    cls = int(box.cls[0].item())
                    # maps the class to a label we understand, forward, left, right, etc - RM
                    label = model.names[cls]
                    # this draws the boxes around the objects and labels them- RM
                    cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
                    cv2.putText(frame, f"{label}{conf:.2f}", (x1, y1-10), cv2.FONT_HERSHEY_PLAIN, 0.7, (0, 255, 0), 2)
                try:
                    blocks = getBlocks(boxes)
                except KeyError as e:
                    pass
                    
            live.update(make_table(blocks))
            # show the frame with the detection - RM
            cv2.imshow("Live Feed", frame)
            # begin waiting for user input
            key = cv2.waitKey(1) & 0xFF

            if key == ord('q'):
                break
            elif key == ord('p'):
                # this will save the current frame as an image and we can run predict.py on it - RM
                filename = "frame" + str(file_counter) + ".jpg"
                if os.path.exists(filename):
                    file_counter += 1
                else:
                    cv2.imwrite(filename, frame)
                    predict.run_predict(filename)
            

            # elif key == ord('p'):
            #     if not ret or frame is None or frame.size == 0:
            #         print("Failed to capture image.")
            #         continue
            #     else:
            #         print("Frame captured successfully")

            #     # save frame
            #     while True:
            #         filename = "frame" + str(file_counter) + ".jpg"
            #         if os.path.exists(filename):
            #             file_counter += 1
            #         else:
            #             break
            #     cv2.imwrite(filename, frame)

        # cleanup
        cap.release()
        cv2.destroyAllWindows()
