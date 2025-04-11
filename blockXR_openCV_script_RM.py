import cv2
import time
import os

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
    print("Press 'p' to capture an image, or 'q' to quit.")
    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
    cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.25) # TODO: Make manual exposure control more accessible to user
    cap.set(cv2.CAP_PROP_EXPOSURE, -7)
    make_max_res(cap)

    while True:
        file_counter = 1
        ret, frame = cap.read()
        frame = cv2.rotate(frame, cv2.ROTATE_180) # frame flip since webcam is upside down
        frame = rescale_frame(frame)
        cv2.imshow("Camera Feed", frame)

        # begin waiting for user input
        key = cv2.waitKey(1) & 0xFF

        if key == ord('q'):
            break
        elif key == ord('p'):
            if not ret or frame is None or frame.size == 0:
                print("Failed to capture image.")
                continue
            else:
                print("Frame captured successfully")

            # save frame
            while True:
                filename = "frame" + str(file_counter) + ".jpg"
                if os.path.exists(filename):
                    file_counter += 1
                else:
                    break
            cv2.imwrite(filename, frame)

    # cleanup
    cap.release()
    cv2.destroyAllWindows()
