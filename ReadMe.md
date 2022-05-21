# Independent Project

Project is divided into indoor and outdoor AR
- Indoor AR uses Yolov3 and a custom trained model to detect and place markers


# Indoor
 To train custom model and use in indoor AR project
   - Train a custom model for Yolo V2 or Yolov3 and save as TFlite format
   - Convert the Tflite model to onnx
   - add the onnx model to project 
   - plug the onnx model to YOLOv3 or YOLOV2 detection gameobject and give the names of input and output of model in the same object's field 

# Outdoor
 To change and add anchors for outdoor AR
  - create a LocationAnchor scriptable object by right clicking in assets folder
  - Add the gps co ordinates in appropriate field and you can also play with tracking distance.
  - add this locationanchor object to the locationanchor list in the PosAnchorObject gameobject. 