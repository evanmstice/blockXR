from ultralytics import YOLO

# Load your trained model


model = YOLO("yolo11n_custom_new.pt")  # Adjust path to your trained model

# Run inference on a video
results = model.predict(source="/Users/reham/Downloads/test_all.mp4", save=True, conf=0.5)