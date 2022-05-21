using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
This is a simple canvas I use to show the distance and name with description of the place.
You could write a custom canvas as per need.
*/
public class AnchorInfoCanvas : AnchorCanvasInterface, IPointerClickHandler
{
    [SerializeField]
    private GameObject descriptionObject;

    [SerializeField]
    private Text descriptionText;

    private Camera arCamera;

    private Transform cameraTransform;

    private Vector3 worldPosition;

    [SerializeField]
    private Text anchorNameText;


    [SerializeField]
    private Text anchorDistanceText;

    [SerializeField]
    private RectTransform panelTransform;


    private void Start()
    {
        if (descriptionObject != null)
        {
            descriptionObject.SetActive(false);
        }
    }

    private void Update()
    {

        if (panelTransform != null)
        {
            Vector3 screenPosition = arCamera.WorldToScreenPoint(worldPosition);

            panelTransform.gameObject.SetActive(screenPosition.z > 0);

            panelTransform.position = screenPosition;
        }

        SetDistanceText((cameraTransform.position - worldPosition).magnitude);
    }

    private void SetDistanceText(float distance)
    {
        if (anchorDistanceText != null)
        {
            anchorDistanceText.text = $"{(int)distance} m";
        }
    }


    public override void Setup(LocationAnchor anchor, Camera arCamera)
    {
        this.pointOfInterest = anchor;
        this.arCamera = arCamera;
        this.cameraTransform = arCamera.transform;
        UpdateInfo();
    }

    public void UpdateInfo()
    {
        if (pointOfInterest != null)
        {
            if (anchorNameText != null)
            {
                anchorNameText.text = pointOfInterest.PoiName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = pointOfInterest.Description;
            }

        }
    }

    public override void UpdatePosition(Vector3 worldPosition, float groundLevelY, float devicePosY)
    {
        this.worldPosition = worldPosition;
        UpdatePositionY(groundLevelY, devicePosY);
    }

    public override void UpdatePositionXZ(Vector3 newPos)
    {
        newPos.y = worldPosition.y;
        worldPosition = newPos;
    }

    public override void UpdatePositionY(float groundLevelY, float devicePosY)
    {
        float posY = 0;
        posY = groundLevelY + pointOfInterest.RelativeHeight + 1f;
        this.worldPosition.y = posY;
    }

    public override void UpdateDistance(float distance)
    {
        SetDistanceText(distance);
    }

    public void ToggleDescriptionVisibility(bool isVisible)
    {
        if (descriptionObject != null && descriptionText != null && !descriptionText.text.Equals(""))
        {
            descriptionObject.SetActive(isVisible);
        }
    }

    public void ToggleDescriptionVisibility()
    {
        if (descriptionObject != null)
        {
            ToggleDescriptionVisibility(!descriptionObject.activeSelf);
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleDescriptionVisibility();
    }

}
