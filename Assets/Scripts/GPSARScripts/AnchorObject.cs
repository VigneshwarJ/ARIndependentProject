using UnityEngine;

/*
This is a custom object code written for defining the behaviors of objects attached to anchor code
*/
public class AnchorObject : AnchorObjInterface
{

    private Transform objectTransform;
    private void Awake()
    {
        objectTransform = transform;
    }

    private float CalculateHeight(float groundLevelY, float devicePosY)
    {
        float posY = 0;
        posY = groundLevelY + pointOfInterest.RelativeHeight;
        return posY;
    }

    // Initial setup for object.
    public override void Setup(LocationAnchor anchor, Camera arCamera)
    {
        this.pointOfInterest = anchor;
    }


    // Set position. Height is determined by the anchor positionMode setting which used groundLevel and devicePosY to calculate height
    public override void UpdatePosition(Vector3 pos, float groundLevelY, float devicePosY)
    {

        pos.y = CalculateHeight(groundLevelY, devicePosY);

        objectTransform.localPosition = pos;
    }

    public override void UpdatePositionY(float groundLevelY, float devicePosY)
    {
        Vector3 localPos = objectTransform.localPosition;
        localPos.y = CalculateHeight(groundLevelY, devicePosY);
        objectTransform.localPosition = localPos;
    }



    // Change object position but Y-value (height) is not changed
    public override void UpdatePositionXZ(Vector3 newPos)
    {
        newPos.y = objectTransform.localPosition.y;
        objectTransform.position = newPos;
    }
}
