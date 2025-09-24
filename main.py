from ultralytics import YOLO

model = YOLO("yolo11n_custom_new.pt")

model.train(data="data_custom.yaml", epochs=50)
