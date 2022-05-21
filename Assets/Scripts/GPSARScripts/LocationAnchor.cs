using UnityEngine;
using UnityEngine.Serialization;


// anchor Tracking state.
// </summary>
public enum AnchorTrackingState
{

    // anchor is not being tracked (it isn't in the tracking radius)

    NotTracking = 0,

    // anchor is being tracked but the anchor is still "far" away

    FarTracking = 1,

    // anchor is being tracked, and it is inside the close tracking radius

    CloseTracking = 2
}


[CreateAssetMenu(fileName = "New Anchor", menuName = "anchor", order = 1)]
public class LocationAnchor : ScriptableObject
{
    #region Variables

    [Header("Information")]
    [SerializeField]
    private string poiName;

    [SerializeField]
    [TextArea(2, 20)]
    private string description;


    [SerializeField]
    private GameObject objectPrefab;

    [SerializeField]
    [FormerlySerializedAs("model")]
    private GameObject modelPrefab;

    [SerializeField]
    private GameObject canvasPrefab;

    [Header("Location")]
    [SerializeField]
    private LatitudeLongitudeStruct coordinates;

    [Header("Far Tracking Radiuses (meters)")]
    [SerializeField]
    private int trackingRadius = 100;

    [SerializeField]
    private int trackingExitMargin = 20;

    [Header("Close Tracking Radiuses (meters)")]
    [SerializeField]
    private int closeTrackingRadius = 20;

    [SerializeField]
    private int closeTrackingExitMargin = 10;


    [SerializeField]
    [Tooltip("In what compass heading direction should the model face. 45 = 45 deg = North-East, 180 South")]
    [Range(0, 360)]
    private float facingDirectionHeading = 0f;

    [SerializeField]
    [Tooltip("This is only used when PositionMode is either RelativeToGround or RelativeToDevice")]
    private float relativeHeight = 0f;

    private AnchorTrackingState trackingState;

    #endregion

    #region Properties

    public string PoiName { get { return poiName; } set { poiName = value; } }
    public string Description { get { return description; } set { description = value; } }
    public GameObject ObjectPrefab { get { return objectPrefab; } set { objectPrefab = value; } }
    public GameObject ModelPrefab { get { return modelPrefab; } set { modelPrefab = value; } }
    public GameObject CanvasPrefab { get { return canvasPrefab; } set { canvasPrefab = value; } }

    public LatitudeLongitudeStruct Coordinates { get { return coordinates; } set { coordinates = value; } }

    public int TrackingRadius { get { return trackingRadius; } set { trackingRadius = value; } }
    public int TrackingExitMargin { get { return trackingExitMargin; } set { trackingExitMargin = value; } }
    public int TrackingExitRadius { get { return trackingRadius + trackingExitMargin; } }

    public int CloseTrackingRadius { get { return closeTrackingRadius; } set { closeTrackingRadius = value; } }
    public int CloseTrackingExitMargin { get { return closeTrackingExitMargin; } set { closeTrackingExitMargin = value; } }
    public int CloseTrackingExitRadius { get { return closeTrackingRadius + closeTrackingExitMargin; } }


    public AnchorTrackingState TrackingState { get { return trackingState; } set { trackingState = value; } }
    public bool Tracking { get { return trackingState == AnchorTrackingState.FarTracking || trackingState == AnchorTrackingState.CloseTracking; } }
    public bool CloseTracking { get { return trackingState == AnchorTrackingState.CloseTracking; } }
    public bool FarTracking { get { return trackingState == AnchorTrackingState.FarTracking; } }

    public float FacingDirectionHeading { get { return facingDirectionHeading; } set { facingDirectionHeading = value; } }
    public float RelativeHeight { get { return relativeHeight; } set { relativeHeight = value; } }

    #endregion


    #region Public Methods


    #endregion

}
