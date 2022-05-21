using System;
using System.Collections;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class LocationUpdatedEventArgs : EventArgs
{
    public LatitudeLongitudeStruct location;
    public float altitude;
    public float horizontalAccuracy;
    public float verticalAccuracy;
    public double timestamp;
}

public class LocationProvider : MonoBehaviour
{


    [SerializeField]
    [Range(0.2f, 10)]
    private float desiredAccuracyInMeters = 1f;


    // The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. 
    // Higher values like 500 imply less overhead.

    [SerializeField]
    [Range(0.2f, 30)]
    private float updateDistanceInMeters = 1f;

    Coroutine pollRoutine;


    private double lastLocationTimestamp;

    private WaitForSeconds wait;

    private LatitudeLongitudeStruct location;

    private LatitudeLongitudeStruct latestAccurateLocation;

    private LocationInfo latestLocationInfo;


    public LatitudeLongitudeStruct Location { get => location; set => location = value; }

    public double LastLocationTimestamp { get { return lastLocationTimestamp; } }

    public LocationInfo LatestLocationInfo { get { return latestLocationInfo; } }


    // Occurs when on location updates.

    public event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;


    // Start is called before the first frame update
    void Start()
    {
        wait = new WaitForSeconds(1f);

        StartPollLocationRoutine();

    }



    // Enable location and compass services.
    private IEnumerator PollLocationRoutine()
    {
#if PLATFORM_ANDROID

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }

#endif

        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location not enabled by user");
            yield break;
        }

        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        Input.compass.enabled = true;

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return wait;
            maxWait--;
        }

        if (maxWait < 1)
        {
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            yield break;
        }

        while (true)
        {
            double timestamp = Input.location.lastData.timestamp;

            if (Input.location.status == LocationServiceStatus.Running && timestamp > lastLocationTimestamp)
            {
                lastLocationTimestamp = timestamp;

                LocationInfo locationInfo = Input.location.lastData;
                latestLocationInfo = locationInfo;

                location = new LatitudeLongitudeStruct(locationInfo.latitude, locationInfo.longitude);

                if (locationInfo.horizontalAccuracy < 5f)
                {
                    latestAccurateLocation = location;
                }

                SendUpdatedLocation();
            }

            yield return null;
        }
    }


    private void StartPollLocationRoutine()
    {
        if (pollRoutine != null)
        {
            StopCoroutine(pollRoutine);
        }
        pollRoutine = StartCoroutine(PollLocationRoutine());
    }

    private void SendUpdatedLocation()
    {
        if (OnLocationUpdated != null)
        {
            LocationUpdatedEventArgs eventArgs = new LocationUpdatedEventArgs()
            {
                location = location,
                altitude = latestLocationInfo.altitude,
                horizontalAccuracy = latestLocationInfo.horizontalAccuracy,
                verticalAccuracy = latestLocationInfo.verticalAccuracy,
                timestamp = latestLocationInfo.timestamp
            };


            OnLocationUpdated(this, eventArgs);
        }
    }


}
