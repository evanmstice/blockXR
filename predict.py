from ultralytics import YOLO

class Block:
    def __init__(self, name, xPos, yPosTop, yPosBottom):
        self.name = name
        self.xPos = xPos
        self.yPosTop = yPosTop
        self.yPosBottom = yPosBottom

model = YOLO("block_weights.pt")

# Testing with a single photo
results = model.predict(source="test.png", save=True, conf=0.5)

blocks = []

# Extracting bounding box data and loading into Block class objects
detected = results[0].boxes
for block in detected:
    coordinates = block.xyxy.tolist()[0]
    xPos = coordinates[0] + (coordinates[2] - coordinates[0]) / 2  # centered x position of block
    blocks.append(Block(model.names[int(block.cls[0].item())], xPos, coordinates[1], coordinates[3]))

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

print(codeBlocksFinal)