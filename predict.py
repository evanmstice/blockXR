from ultralytics import YOLO

# Load your trained model


model = YOLO("/Users/reham/PycharmProjects/TrainingForwardANDClicked/runs/detect/train/weights/best.pt")  # Adjust path to your trained model

# Run inference on a video
results = model.predict(source="/Users/reham/Downloads/two_blocks.MOV", save=True, conf=0.5)