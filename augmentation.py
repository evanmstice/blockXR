import os
import cv2
import albumentations as A

# Input dataset root
ROOT_DIR = "data"

# Output augmented folder
AUG_DIR = os.path.join(ROOT_DIR, "augmented")
os.makedirs(os.path.join(AUG_DIR, "images"), exist_ok=True)
os.makedirs(os.path.join(AUG_DIR, "labels"), exist_ok=True)

# Define augmentation pipeline
transform = A.Compose([
    
    A.HorizontalFlip(p=0.5),
    A.VerticalFlip(p=0.2),
    A.RandomScale(scale_limit=0.2, p=0.5),
    A.Rotate(limit=15, p=0.5),
    A.RandomBrightnessContrast(p=0.5),
    A.HueSaturationValue(p=0.5),
    A.ColorJitter(p=0.4),
    A.Blur(p=0.2)
], bbox_params=A.BboxParams(format='yolo', label_fields=['class_labels'], min_visibility=0.3, clip=True))

def read_yolo_labels(label_path):
    boxes, classes = [], []
    with open(label_path, 'r') as f:
        for line in f.readlines():
            parts = line.strip().split()
            if len(parts) != 5:
                continue
            cls, x, y, w, h = parts
            boxes.append([float(x), float(y), float(w), float(h)])
            classes.append(int(cls))
    return boxes, classes

def write_yolo_labels(label_path, boxes, classes):
    with open(label_path, 'w') as f:
        for cls, (x, y, w, h) in zip(classes, boxes):
            f.write(f"{int(cls)} {x:.6f} {y:.6f} {w:.6f} {h:.6f}\n")

def augment_split(split, num_augments=3):
    image_dir = os.path.join(ROOT_DIR, split, "images")
    label_dir = os.path.join(ROOT_DIR, split, "labels")

    for img_name in os.listdir(image_dir):

        img_path = os.path.join(image_dir, img_name)
        lbl_path = os.path.join(label_dir, os.path.splitext(img_name)[0] + ".txt")

        if not os.path.exists(lbl_path):
            continue

        image = cv2.imread(img_path)
        boxes, classes = read_yolo_labels(lbl_path)

        for i in range(num_augments):
            transformed = transform(image=image, bboxes=boxes, class_labels=classes)
            aug_img = transformed['image']
            aug_boxes = transformed['bboxes']
            aug_classes = transformed['class_labels']

            if len(aug_boxes) == 0:
                continue  # skip if all boxes are lost

            base_name = os.path.splitext(img_name)[0]
            new_name = f"{split}_{base_name}_aug{i}.jpg"
            new_lbl_name = new_name.replace('.jpg', '.txt')

            cv2.imwrite(os.path.join(AUG_DIR, "images", new_name), aug_img)
            write_yolo_labels(os.path.join(AUG_DIR, "labels", new_lbl_name), aug_boxes, aug_classes)

            print(f"Saved {new_name}")

# Run on both train and val
augment_split("train", num_augments=3)
augment_split("val", num_augments=2)
