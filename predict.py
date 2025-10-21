from ultralytics import YOLO
import os
class Block:
    def __init__(self, name, xPos, yPosTop, yPosBottom, confidence):
        self.name = name
        self.xPos = xPos
        self.yPosTop = yPosTop
        self.yPosBottom = yPosBottom
        self.confidence = confidence

def run_predict(image_path):


    model = YOLO("block_weights.pt")

    
   

    # debug file RM

    log_path = "detections_log.txt"
    with open(log_path, "a")  as log_file:
        # Testing with a single photo
        # turning off verbose, that is ultralytics detection logs
        #raising conf threshold to from .5 to .75
        results = model.predict(source=image_path, save=True, conf=0.75, verbose=False)
        blocks = []
        # Extracting bounding box data and loading into Block class objects
        detected = results[0].boxes
        
        # min_box_area = 58000
        # max_box_area = 68000
        for block in detected:
            #coordinates = block.xyxy.tolist()[0]
            x1, y1, x2, y2 = block.xyxy.tolist()[0]
            area = (x2 - x1) * (y2 - y1)
            # if area < min_box_area:
            #     continue
            # if area > max_box_area:
            #    continue
            coordinates = (x1, y1, x2, y2)
            # get the class label cleanly RM
            class_id = int(block.cls[0].item())
            # get the confidence
            confidence = float(block.conf[0].item())
            name = model.names[class_id]

            xPos = coordinates[0] + (coordinates[2] - coordinates[0]) / 2  # centered x position of block
            blocks.append(Block(name, xPos, coordinates[1], coordinates[3]))

            msg = f"Detected: {name:<15} | Confidence: {confidence:.2f} | Box: {coordinates}\n"
            log_file.write(msg)



        blocks.sort(key=lambda b: b.yPosTop)

        toleranceX = 75
        toleranceY = 10  # Add later: auto tolerance, could start program by checking width of blocks in relation to camera view and then
                        # have tolerance be a percentage of that

        codeBlocksFinal = []
        whenClickedIndice = next((i for i, b in enumerate(blocks) if b.name == "When clicked"), None)
        if whenClickedIndice is None:
            raise KeyError("Error: When clicked block not found.")

        codeBlocksFinal.append("When clicked")
        currXPos = blocks[whenClickedIndice].xPos
        currYPos = blocks[whenClickedIndice].yPosBottom
        for block in blocks[whenClickedIndice + 1:]:
            # check if block below is snapped together
            if (currYPos - toleranceY <= block.yPosTop <= currYPos + toleranceY) and (currXPos - toleranceX <= block.xPos <= currXPos + toleranceX):
                codeBlocksFinal.append(block.name)
                currYPos = block.yPosBottom
                currXPos = block.xPos
            else:
                break

        #print("\nFinal Block Sequence:", codeBlocksFinal)
        final_msg = f"Final Block Sequence: {codeBlocksFinal}\n{'-'*80}\n"
        log_file.write(final_msg)
    

    return codeBlocksFinal


if __name__ == "__main__":
    run_predict("test.png")

# model = YOLO("block_weights.pt")

# # Testing with a single photo
# # turning off verbose, that is ultralytics detection logs
# results = model.predict(source="test.png", save=True, conf=0.5, verbose=False)

# blocks = []

# # debug file RM

# log_path = os.path.join(os.path.dirname(__file__), "detections_log.txt")
# log_file = open(log_path, "a")  

# # Extracting bounding box data and loading into Block class objects
# detected = results[0].boxes
# for block in detected:
#     coordinates = block.xyxy.tolist()[0]
#     # get the class label cleanly RM
#     class_id = int(block.cls[0].item())
#     # get the confidence
#     confidence = float(block.conf[0].item())
#     name = model.names[class_id]

#     xPos = coordinates[0] + (coordinates[2] - coordinates[0]) / 2  # centered x position of block
#     blocks.append(Block(name, xPos, coordinates[1], coordinates[3]))

#     msg = f"Detected: {name:<15} | Confidence: {confidence:.2f} | Box: {coordinates}\n"
#     log_file.write(msg)

def getBlocks(data) -> list[Block]:

    blocks = []

    # Extracting bounding box data and loading into Block class objects
    # data: detected = results[0].boxes
    for block in data:
        coordinates = block.xyxy.tolist()[0]
        xPos = coordinates[0] + (coordinates[2] - coordinates[0]) / 2  # centered x position of block
        blocks.append(Block(model.names[int(block.cls[0].item())], xPos, coordinates[1], coordinates[3], block.conf[0].item()))

    blocks.sort(key=lambda b: b.yPosTop)

    toleranceX = 200
    toleranceY = 10  # Add later: auto tolerance, could start program by checking width of blocks in relation to camera view and then
                    # have tolerance be a percentage of that

    codeBlocksFinal = []
    whenClickedIndice = next((i for i, b in enumerate(blocks) if b.name == "When clicked"), None)
    if whenClickedIndice is None:
        raise KeyError("Error: When clicked block not found.")

    codeBlocksFinal.append(["When clicked", blocks[whenClickedIndice].confidence])
    currXPos = blocks[whenClickedIndice].xPos
    currYPos = blocks[whenClickedIndice].yPosBottom
    for block in blocks[whenClickedIndice + 1:]:
        # check if block below is snapped together
        if (currYPos - toleranceY <= block.yPosTop <= currYPos + toleranceY) and (currXPos - toleranceX <= block.xPos <= currXPos + toleranceX):
            codeBlocksFinal.append([block.name, block.confidence])
            currYPos = block.yPosBottom
            currXPos = block.xPos
        else:
            break

    return codeBlocksFinal
