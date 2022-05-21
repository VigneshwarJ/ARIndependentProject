using UnityEngine;
using System;


public class TryeHeadingUpdatedEventArgs : EventArgs
{
    public float heading;
    public float filteredHeading;
    public float filteredHeadingVelocity;
    public double timestamp;
}


public class TrueHeadingProvider : MonoBehaviour
{
    // #region Variables

    double lastHeadingTimestamp;

    private float headingSmooth = 0.3f;

    private float headingSmoothVelocity = 0.0f;

    private float heading = 0;
    private float filteredHeading = -1f;

    public event EventHandler<TryeHeadingUpdatedEventArgs> OnHeadingUpdated;


    public float Heading { get { return heading; } }
    public double LastHeadingTimestamp { get { return lastHeadingTimestamp; } }
    public float FilteredHeading { get { return filteredHeading; } }
    public float HeadingSmoothVelocity { get { return headingSmoothVelocity; } }
    

    private void Start() {
            Input.compass.enabled = true;
    }

    void Update()
    {
           PollHeading();
    }

    private float CalculateFilteredHeading(float rawHeading) {
        if (filteredHeading < 0) {
            filteredHeading = rawHeading;
        }
        else {
            //Filter out rapid small changes
            filteredHeading = Mathf.SmoothDampAngle(filteredHeading, rawHeading, ref headingSmoothVelocity, headingSmooth);

            filteredHeading = MathTools.DegAngle0To360(filteredHeading);
        }

        return filteredHeading;
    }

    private void PollHeading() {
        double timestamp = Input.compass.timestamp;

        if (Input.compass.enabled && timestamp > lastHeadingTimestamp)
        {
            heading = Input.compass.trueHeading;
            lastHeadingTimestamp = timestamp;

            CalculateFilteredHeading(heading);
            SendUpdatedHeading();
        }
    }

 
    private void SendUpdatedHeading()
    {
        if (OnHeadingUpdated != null)
        {
            TryeHeadingUpdatedEventArgs eventArgs = new TryeHeadingUpdatedEventArgs()
            {
                heading = heading,
                filteredHeading = filteredHeading,
                timestamp = lastHeadingTimestamp,
                filteredHeadingVelocity = headingSmoothVelocity
            };

            OnHeadingUpdated(this, eventArgs);
        }
    }
    
}
