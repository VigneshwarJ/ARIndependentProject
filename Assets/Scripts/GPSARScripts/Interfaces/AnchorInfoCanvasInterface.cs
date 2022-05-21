using UnityEngine;

/*
This interface is used to add your own canvas functionality for each object
*/
public abstract class AnchorCanvasInterface : MonoBehaviour
{
    protected LocationAnchor pointOfInterest;
    public abstract void Setup(LocationAnchor anchor, Camera arCamera);
    public abstract void UpdateDistance(float distance);
    public abstract void UpdatePosition(Vector3 worldPosition, float groundLevel, float devicePosY);
    public abstract void UpdatePositionXZ(Vector3 newPos);
    public abstract void UpdatePositionY(float groundLevelY, float devicePosY);

    public LocationAnchor PointOfInterest { get { return pointOfInterest; } set { pointOfInterest = value; } }

}
