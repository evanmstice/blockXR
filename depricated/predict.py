from ultralytics import YOLO
import os
class Block:
    def __init__(self, name, xPos, yPosTop, yPosBottom, confidence):
        self.name = name
        self.xPos = xPos
        self.yPosTop = yPosTop
        self.yPosBottom = yPosBottom
        self.confidence = confidence

# model is imported only to access the class names (it is not run in this file)
model = YOLO("block_weights.pt")

def updateToleranceY(blocks) -> int:
    if not blocks:
        height = blocks[0].yPosTop - blocks[0].yPosBottom
        return int(height * 1.1)  # 10% larger than one block height tolerance [TEST]
    return 20 # default value

def updateToleranceX(blocks) -> int:  # based off height, can be calculated from ratios
    if not blocks:
        height = blocks[0].yPosTop - blocks[0].yPosBottom
        return int(height * 1.1)  # 10% larger than one block width tolerance [TEST]
    return 100 # default value
    
def getBlocks(data) -> list[Block]:

    # default values
    blocks = []
    toleranceX = 100
    toleranceY = 20 

    # Extracting bounding box data and loading into Block class objects
    for block in data:
        coordinates = block.xyxy.tolist()[0]
        xPos = coordinates[0] + (coordinates[2] - coordinates[0]) / 2  # centered x position of block
        blocks.append(Block(model.names[int(block.cls[0].item())], xPos, coordinates[1], coordinates[3], block.conf[0].item()))

    blocks.sort(key=lambda b: b.yPosTop)
    
    if blocks:
        toleranceY = updateToleranceY(blocks)
        toleranceX = updateToleranceX(blocks)

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
