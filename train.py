from ultralytics import YOLO

model = YOLO("yolo11n.pt")
results = model.train(data="data_custom.yaml", epochs=75, imgsz=640, device="mps", batch=0.7, val=False)