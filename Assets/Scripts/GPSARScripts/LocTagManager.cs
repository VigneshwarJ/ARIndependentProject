using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;


public class LocTagManager : MonoBehaviour
{
    public class AnchorTrackerInfo
    {
        public LocationAnchor anchor;
        public AnchorCanvasInterface anchorCanvas;
        public AnchorObjInterface anchorObject;
        public Vector3 lastUpdateCameraPosition;
        public Vector3 lastUpdatePositionRelativeToCamera;

        public AnchorTrackerInfo(LocationAnchor anchor, AnchorCanvasInterface poiCanvas, AnchorObjInterface poiObject)
        {
            this.anchor = anchor;
            this.anchorCanvas = poiCanvas;
            this.anchorObject = poiObject;
        }
    }

    // List of LocationAnchors to track
    [SerializeField]
    private List<LocationAnchor> locationAnchors = new List<LocationAnchor>();

    [SerializeField]
    private ARTrueNorthFinder arTrueNorthFinder;

    [SerializeField]
    private Camera arCamera;


    [SerializeField]
    private LocationProvider locationProvider;
    private List<AnchorTrackerInfo> poiTrackerDatas = new List<AnchorTrackerInfo>();

    private Transform arCameraTransform;

    private double previousUpdateTimestamp = 0;

    private float previousUpdateAccuracy = 999;

    private Text txt;



    private List<LocationAnchor> PointOfInterests
    {
        get
        {
            return locationAnchors;
        }
    }

    public List<AnchorTrackerInfo> PoiTrackerDatas { get { return poiTrackerDatas; } }

    private void Awake()
    {


        if (arCamera != null)
        {
            arCameraTransform = arCamera.gameObject.transform;
        }

        for (int i = 0; i < PointOfInterests.Count; i++)
        {
            PointOfInterests[i].TrackingState = AnchorTrackingState.NotTracking;
        }
    }

    private void OnEnable()
    {
        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated += OnLocationUpdated;
        }

        if (arTrueNorthFinder != null)
        {
            arTrueNorthFinder.OnHeadingUpdated += OnNorthHeadingUpdated;
        }

        ARSession.stateChanged += OnARSessionStateChange;
    }

    private void OnDisable()
    {
        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated -= OnLocationUpdated;
        }

        if (arTrueNorthFinder != null)
        {
            arTrueNorthFinder.OnHeadingUpdated -= OnNorthHeadingUpdated;
        }

        ARSession.stateChanged -= OnARSessionStateChange;
        StopTrackingPOIs();
    }


    private void Start()
    {

        txt = GameObject.Find("Info").GetComponent<Text>();
        txt.text = "HEllo";
    }

    private void Update()
    {

        for (int i = 0; i < poiTrackerDatas.Count; i++)
        {
            AnchorObjInterface poiObject = poiTrackerDatas[i].anchorObject;

            if (poiObject != null)
            {
                poiObject.SetVisibility(arCamera.farClipPlane > ((poiObject.transform.position - arCameraTransform.position).magnitude + 2f));
            }
        }
    }

    public void RefreshTrackers()
    {
        List<LocationAnchor> poisToRemove = new List<LocationAnchor>();

        for (int i = 0; i < poiTrackerDatas.Count; i++)
        {
            if (!PointOfInterests.Contains(poiTrackerDatas[i].anchor))
            {
                poisToRemove.Add(poiTrackerDatas[i].anchor);
            }
        }

        for (int i = 0; i < poisToRemove.Count; i++)
        {
            StopTrackingPOI(poisToRemove[i]);
        }
    }

    private void OnARSessionStateChange(ARSessionStateChangedEventArgs evt)
    {
        previousUpdateAccuracy = 9999f;
        if (evt.state == ARSessionState.SessionTracking)
        {
            RecheckPOITrackings();
        }
        else
        {
            StopTrackingPOIs();
        }
    }

    private void OnNorthHeadingUpdated(object sender, HeadingUpdatedEventArgs e)
    {
        if (e.isPriority || TimeSpan.FromMilliseconds(e.timestamp - previousUpdateTimestamp).TotalSeconds > 5f)
        {
            RotatePOIsRelativeToNorth(e.heading, e.isPriority);
        }
    }

    private void OnLocationUpdated(object sender, LocationUpdatedEventArgs e)
    {

        bool updatePositions = false;

        float accuracy = Mathf.Max(e.horizontalAccuracy, e.verticalAccuracy);

        float deltaAccuracy = accuracy - previousUpdateAccuracy;

        txt.text = locationProvider.Location.Lat + " " + locationProvider.Location.Lon;
        if (accuracy <= 8f || deltaAccuracy <= 5f)
        {
            updatePositions = true;
        }
        else
        {
            TimeSpan timeSinceLast = TimeSpan.FromMilliseconds(e.timestamp - previousUpdateTimestamp);
            updatePositions =
                (timeSinceLast.TotalSeconds > 10 && accuracy <= 8f)
                || (timeSinceLast.TotalSeconds > 20 && accuracy <= 15f)
                || (timeSinceLast.TotalSeconds > 30);
        }

        if (updatePositions)
        {
            previousUpdateAccuracy = accuracy;
            previousUpdateTimestamp = e.timestamp;
            RecheckPOITrackings();
        }
    }

    // Move all tracked anchor objects

    private void RecheckPOITrackings()
    {
        LatitudeLongitudeStruct currentLocation = locationProvider.Location;
        if (currentLocation == null) return;

        Vector3 currentARCameraPosition = arCameraTransform.position;
        float devicePosY = currentARCameraPosition.y;
        currentARCameraPosition.y = 0;

        float groundLevel = 0f;
        if (PointOfInterests == null || PointOfInterests.Count == 0) return;

        for (int i = 0; i < PointOfInterests.Count; i++)
        {

            LocationAnchor anchor = PointOfInterests[i];

            //Calculate the distance between the points
            float distanceBetween = currentLocation.DistanceToPoint(anchor.Coordinates);

            bool onlyUpdateHeight = false;

            if (!anchor.CloseTracking && distanceBetween <= anchor.CloseTrackingRadius)
            {
                if (!anchor.FarTracking)
                {
                    CreateObjectsForPOI(anchor);
                }

                anchor.TrackingState = AnchorTrackingState.CloseTracking;
            }
            else if (!anchor.Tracking && distanceBetween <= anchor.TrackingRadius)
            {
                anchor.TrackingState = AnchorTrackingState.FarTracking;
                CreateObjectsForPOI(anchor);
            }
            else if (anchor.Tracking && distanceBetween >= anchor.TrackingExitRadius)
            {
                StopTrackingPOI(anchor);
            }
            else if (anchor.CloseTracking)
            {
                if (distanceBetween >= anchor.CloseTrackingExitRadius)
                {
                    anchor.TrackingState = AnchorTrackingState.FarTracking;
                }
                else
                {
                    onlyUpdateHeight = true;
                }
            }


            AnchorTrackerInfo trackerData = poiTrackerDatas.Find(x => x.anchor == anchor);

            if (trackerData == null) continue;

            float trueNorthHeadingDifference = arTrueNorthFinder != null ? arTrueNorthFinder.Heading : 0;

            AnchorCanvasInterface poiCanvas = trackerData.anchorCanvas;
            AnchorObjInterface poiObject = trackerData.anchorObject;

            if (onlyUpdateHeight)
            {
                if (poiObject != null)
                {
                    poiObject.UpdatePositionY(groundLevel, devicePosY);
                }

                //Update anchor's canvas position
                if (poiCanvas != null)
                {
                    poiCanvas.UpdatePositionY(groundLevel, devicePosY);
                }

                continue;
            }

            Vector3 newPos = Vector3.zero;

            float bearing = 0;

            bearing = MathTools.BearingFromPointAToB(currentLocation, anchor.Coordinates);

            newPos = new Vector3(distanceBetween * Mathf.Sin(bearing), 0, distanceBetween * Mathf.Cos(bearing));

            //Offset new position with current camera position
            trackerData.lastUpdateCameraPosition = currentARCameraPosition;
            trackerData.lastUpdatePositionRelativeToCamera = newPos;

            //Offset with true north difference so Z-axis == AR True-North-Axis
            newPos = currentARCameraPosition + (Quaternion.AngleAxis(trueNorthHeadingDifference, Vector3.up) * newPos);

            if (poiObject != null)
            {
                poiObject.UpdatePosition(newPos, groundLevel, devicePosY);
                poiObject.UpdateRotation(trueNorthHeadingDifference);
            }

            //Update anchor's canvas position
            if (poiCanvas != null)
            {
                poiCanvas.UpdatePosition(newPos, groundLevel, devicePosY);
                poiCanvas.UpdateDistance(distanceBetween);
            }

        }

    }

    private void RotatePOIsRelativeToNorth(bool forceUpdate = false)
    {
        float trueNorthHeadingDifference = arTrueNorthFinder != null ? arTrueNorthFinder.Heading : 0;

        RotatePOIsRelativeToNorth(trueNorthHeadingDifference, forceUpdate);
    }


    // Only rotates the anchor relative to true north. Doesn't affect the positions.

    // <param name="heading"></param>
    private void RotatePOIsRelativeToNorth(float heading, bool forceUpdate = false)
    {

        for (int i = 0; i < poiTrackerDatas.Count; i++)
        {
            AnchorTrackerInfo trackerData = poiTrackerDatas[i];

            if (trackerData == null || trackerData.anchor == null || (trackerData.anchor.CloseTracking && !forceUpdate)) continue;

            Vector3 newPos = trackerData.lastUpdateCameraPosition + (Quaternion.AngleAxis(heading, Vector3.up) * trackerData.lastUpdatePositionRelativeToCamera);

            if (trackerData.anchorCanvas != null)
            {
                trackerData.anchorCanvas.UpdatePositionXZ(newPos);
            }

            if (trackerData.anchorObject != null)
            {
                trackerData.anchorObject.UpdatePositionXZ(newPos);
            }
        }
    }



    // Enable tracking for anchor. Create 3D-model and canvas objects.

    private void CreateObjectsForPOI(LocationAnchor anchor)
    {


        //Check if there is already objects for this tracker
        if (poiTrackerDatas != null && poiTrackerDatas.Count > 0)
        {
            AnchorTrackerInfo poiTracker = poiTrackerDatas.Find((x) => x.anchor == anchor);
            if (poiTracker != null) return;
        }

        GameObject objectGo = null;
        GameObject modelGo = null;
        GameObject poiCanvasGo = null;

        AnchorObjInterface poiObject = null;
        AnchorCanvasInterface poiCanvas = null;

        if (anchor.ModelPrefab != null)
        {
            if (anchor.ObjectPrefab == null)
            {
                objectGo = new GameObject($"Object tracker for {anchor.PoiName}");
            }
            else
            {
                objectGo = Instantiate(anchor.ObjectPrefab, Vector3.zero, Quaternion.identity, null);
            }

            modelGo = Instantiate(anchor.ModelPrefab, Vector3.zero, Quaternion.identity, objectGo.transform);

            poiObject = objectGo.GetComponent<AnchorObjInterface>();

            if (poiObject == null)
            {
                poiObject = objectGo.AddComponent<AnchorObject>();
            }

            poiObject.Setup(anchor, arCamera);
            poiObject.UpdateRotation(0);
        }


        if (anchor.CanvasPrefab != null)
        {
            poiCanvasGo = Instantiate(anchor.CanvasPrefab, Vector3.zero, Quaternion.identity, null);

            poiCanvas = poiCanvasGo.GetComponent<AnchorCanvasInterface>();

            poiCanvas.Setup(anchor, arCamera);
        }

        AnchorTrackerInfo trackerData = new AnchorTrackerInfo(anchor, poiCanvas, poiObject);
        poiTrackerDatas.Add(trackerData);
    }


    // Disable tracking of anchor. Destroys 3D-model and canvas objects.

    private void StopTrackingPOI(LocationAnchor anchor)
    {
        anchor.TrackingState = AnchorTrackingState.NotTracking;

        AnchorTrackerInfo trackerData = poiTrackerDatas.Find((x) => x.anchor == anchor);

        if (trackerData == null) return;


        AnchorObjInterface poiObject = trackerData.anchorObject;

        //Destroy and remove anchor 3D-model
        if (poiObject != null)
        {
            Destroy(poiObject.gameObject);
        }

        //Destroy and remove anchor canvas
        AnchorCanvasInterface poiCanvas = trackerData.anchorCanvas;

        if (poiCanvas != null)
        {
            Destroy(poiCanvas.gameObject);
        }

        poiTrackerDatas.Remove(trackerData);
    }


    // Stops tracking all the POIs, deleting all the canvases and gameobjects.

    private void StopTrackingPOIs()
    {
        if (poiTrackerDatas != null)
        {
            for (int i = 0; i < poiTrackerDatas.Count; i++)
            {
                AnchorTrackerInfo trackerData = poiTrackerDatas[i];
                if (trackerData == null) continue;

                LocationAnchor anchor = trackerData.anchor;

                if (anchor != null)
                {
                    anchor.TrackingState = AnchorTrackingState.NotTracking;
                }

                AnchorObjInterface poiObject = trackerData.anchorObject;

                //Destroy and remove anchor 3D-model
                if (poiObject != null)
                {
                    Destroy(poiObject.gameObject);
                }

                //Destroy and remove anchor canvas
                AnchorCanvasInterface poiCanvas = trackerData.anchorCanvas;

                if (poiCanvas != null)
                {
                    Destroy(poiCanvas.gameObject);
                }
            }

            //Make sure every anchor is set to "NotTracking"-state
            for (int i = 0; i < PointOfInterests.Count; i++)
            {
                PointOfInterests[i].TrackingState = AnchorTrackingState.NotTracking;
            }

            poiTrackerDatas.Clear();

        }
    }


}
