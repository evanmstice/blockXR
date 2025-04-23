from ultralytics import YOLO

model = YOLO("yolov8s.pt")
model.train(data="config.yaml", epochs=50)