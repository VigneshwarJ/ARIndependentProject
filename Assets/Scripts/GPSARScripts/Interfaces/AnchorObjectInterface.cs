using UnityEngine;

public abstract class AnchorObjInterface : MonoBehaviour
{

    protected LocationAnchor pointOfInterest;


    public LocationAnchor PointOfInterest { get { return pointOfInterest; } set { pointOfInterest = value; } }

    public abstract void Setup(LocationAnchor anchor, Camera arCamera);
    public abstract void UpdatePosition(Vector3 pos, float groundLevelY, float devicePosY);

    public abstract void UpdatePositionY(float groundLevelY, float devicePosY);
    public abstract void UpdatePositionXZ(Vector3 newPos);
    public virtual void SetVisibility(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    // Update object's rotation based on true north heading and anchor facing direction
    public virtual void UpdateRotation(float trueNorthHeading)
    {
        Vector3 rotEuler = transform.rotation.eulerAngles;

        rotEuler.y = trueNorthHeading + pointOfInterest.FacingDirectionHeading;

        transform.rotation = Quaternion.Euler(rotEuler);
    }


}
