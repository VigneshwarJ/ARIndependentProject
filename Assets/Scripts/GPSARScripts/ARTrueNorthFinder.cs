/******************************************************************************
* Lisence      : BSD 3-Clause License
* Copyright    : Lapland University of Applied Sciences
* Authors      : Arto Söderström
* BSD 3-Clause License
*
* Copyright (c) 2019, Lapland University of Applied Sciences
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
* 
* 1. Redistributions of source code must retain the above copyright notice, this
*  list of conditions and the following disclaimer.
*
* 2. Redistributions in binary form must reproduce the above copyright notice,
*  this list of conditions and the following disclaimer in the documentation
*  and/or other materials provided with the distribution.
*
* 3. Neither the name of the copyright holder nor the names of its
*  contributors may be used to endorse or promote products derived from
*  this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
* AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
* FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
* DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
* SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
* CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
* OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
* OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*****************************************************************************/

/*
This file contains some code from abovementioned Authors code
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Linq;
using UnityEngine.UI;

public class HeadingUpdatedEventArgs : EventArgs
{
    public float heading;
    public double timestamp;
    public bool isPriority;
}


// AR True North Calculation phase changed event args
// </summary>
public class pLab_CalculationPhaseChangedEventArgs : EventArgs
{
    public ARTrueNorthFinder.CalculationPhase calculationPhase;
}

public class HeadingFromMovementUpdatedEventArgs : EventArgs
{
    public float arHeading;
    public float gpsHeading;
    public float headingDifference;
    public float medianHeadingDifference;
    public float averageHeadingDifference;
}

public class ARTrueNorthFinder : MonoBehaviour
{
    // In which state is the calculation. Used for Compass-mode

    public enum CalculationPhase
    {
        Stopped = 0,

        // Low intervals to get the original heading fast

        FastInterval = 1,

        // Higher interval to modify original heading

        SlowInterval = 2
    }


    [SerializeField]
    private bool isEnabled = false;

    [SerializeField]
    private bool resetAfterTrackingLost = true;

    private bool isSessionTracking = false;

    [SerializeField]
    private Camera arCamera;


    private Transform arCameraTransform;

    [Header("GPS-AR Mode")]
    [SerializeField]
    private LocationProvider locationProvider;

    [SerializeField]
    private float minDistanceBeforeUpdate = 10f;

    [SerializeField]
    private float movementMinimumGPSAccuracy = 10f;

    [SerializeField]
    private int maxLastMovementHeadings = 5;

    private bool hasGPSARHeading = false;

    [Header("Compass Mode")]
    [SerializeField]
    private TrueHeadingProvider headingProvider;


    // How much time between recordings/readings

    private float updateIntervalCompass = 0f;


    // Initial time between compass-mode readings

    [SerializeField]
    private float updateIntervalCompassInitial = 0.25f;


    // How much time between recordings/readings when the first phase (half of max size) has been reached

    [SerializeField]
    private float updateIntervalCompassDelayed = 3f;


    // Max size of readings before we start removing old values

    [SerializeField]
    private int maxCompassModeSampleSize = 50;

    [SerializeField]
    private Text CalibrationWarning;
    private bool hasCompassHeading = false;

    private CalculationPhase calculationPhase = CalculationPhase.Stopped;

    private float heading = 0;

    private float compassModeHeading = 0;

    private double headingTimestamp;

    private Vector3 previousARCameraPosition = Vector3.zero;

    private LatitudeLongitudeStruct previousGPSCoordinates;

    private float previousGPSAccuracy = 9999f;

    private List<float> lastGPSARHeadingDeltas = new List<float>();

    private float initialGPSARDifference = 0;

    private float arHeading;
    private float gpsHeading;
    private float medianGPSARHeadingDifference;
    private float averageGPSARHeadingDifference;
    private float distance;

    private float updateTimerCompass = 0;

    private float initialReading = -1f;

    private List<float> readingDeltas = new List<float>();

    private float manualOffset = 0f;

    public bool IsEnabled
    {
        get { return isEnabled; }
        set
        {
            bool previousVal = isEnabled;
            isEnabled = value;

            if (isEnabled != previousVal && !isEnabled)
            {
                ResetBothModes();
            }

        }
    }

    public bool FastIntervalPhaseSizeReached { get { return readingDeltas.Count >= (maxCompassModeSampleSize / 2); } }

    public bool HeadingSampleSizeReached { get { return readingDeltas.Count >= maxCompassModeSampleSize; } }
    public float Heading { get { return heading; } }
    public float ARHeading { get { return arHeading; } }
    public float GPSHeading { get { return gpsHeading; } }

    public CalculationPhase CalculationPhaseProp
    {
        get { return calculationPhase; }

        private set
        {
            CalculationPhase prevPhase = calculationPhase;

            calculationPhase = value;

            switch (calculationPhase)
            {
                //For Stopped-phase it doesn't really even matter which one is it
                case CalculationPhase.Stopped:
                case CalculationPhase.FastInterval:
                    updateIntervalCompass = updateIntervalCompassInitial;
                    break;
                case CalculationPhase.SlowInterval:
                    updateIntervalCompass = updateIntervalCompassDelayed;
                    break;
            }

            //If the phase actually was changed
            if (calculationPhase != prevPhase)
            {
                if (OnCalculationPhaseChanged != null)
                {
                    pLab_CalculationPhaseChangedEventArgs args = new pLab_CalculationPhaseChangedEventArgs()
                    {
                        calculationPhase = calculationPhase
                    };

                    OnCalculationPhaseChanged(this, args);
                }
            }
        }
    }


    // Triggered when CalculationPhase has been changed
    public event EventHandler<pLab_CalculationPhaseChangedEventArgs> OnCalculationPhaseChanged;


    // Triggered for new true north from compass
    public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;


    // Triggered for new true north from GPSAR calculation
    public event EventHandler<HeadingFromMovementUpdatedEventArgs> OnHeadingFromMovementUpdated;


    private void Awake()
    {
        CalculationPhaseProp = CalculationPhase.Stopped;
        updateIntervalCompass = updateIntervalCompassInitial;
        if (arCamera != null)
        {
            arCameraTransform = arCamera.transform;
        }
    }

    private void OnEnable()
    {

        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated += OnLocationUpdated;
        }

        isSessionTracking = ARSession.state == ARSessionState.SessionTracking;
        ResetBothModes();
        ARSession.stateChanged += OnARSessionStateChange;

    }

    private void OnDisable()
    {
        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated -= OnLocationUpdated;
        }

        ARSession.stateChanged -= OnARSessionStateChange;
    }

    private void Update()
    {
        //FastInterval calculation
        if (calculationPhase == CalculationPhase.Stopped)
        {
            CalculationPhaseProp = CalculationPhase.FastInterval;
            CalibrationWarning.text = "Calibrating Compass ......";
        }

        updateTimerCompass += Time.deltaTime;

        if (updateTimerCompass >= updateIntervalCompass)
        {

            //If RecordHeading returns true -> new record was recorded
            if (RecordCompassHeading())
            {
                updateTimerCompass = 0;

                if (CalculationPhaseProp == CalculationPhase.FastInterval && FastIntervalPhaseSizeReached)
                {
                    CalibrationWarning.text = "";
                    CalculationPhaseProp = CalculationPhase.SlowInterval;
                    RecalculateMedianCompassHeading();
                    RecalculateHeading();
                    TriggerNorthHeadingUpdatedEvent(true);
                }
            }
        }
    }
    // We calculate GPS based Heading for extra accuracy
    private void OnLocationUpdated(object sender, LocationUpdatedEventArgs e)
    {

        CalculateGPSARHeadings(e);

    }


    // Event handler for ARSessionStateChangedEvent

    // <param name="e"></param>
    private void OnARSessionStateChange(ARSessionStateChangedEventArgs e)
    {
        if (e.state == ARSessionState.SessionTracking)
        {
            isSessionTracking = true;
            ResetCompassMode();
            ResetGPSARMode();
        }
        else
        {
            isSessionTracking = false;
        }
    }



    // Calculates GPS-AR Mode headings

    // <param name="e"></param>
    private void CalculateGPSARHeadings(LocationUpdatedEventArgs e)
    {

        if (previousGPSCoordinates == null)
        {
            previousGPSCoordinates = e.location;
            previousARCameraPosition = arCameraTransform.position;
            return;
        }

        bool isHeadingUsable = false;
        bool isGPSHeadingUsable = false;
        float newARHeading = 0;
        float newGPSHeading = 0;

        Vector3 newPosition = arCameraTransform.position;

        Vector3 positionDeltaVector = newPosition - previousARCameraPosition;

        distance = positionDeltaVector.magnitude;

        if (distance > minDistanceBeforeUpdate)
        {

            newARHeading = Vector3.SignedAngle(Vector3.forward, positionDeltaVector, Vector3.up);
            newARHeading = MathTools.DegAngleToPositive(newARHeading);
            isHeadingUsable = true;
        }

        LatitudeLongitudeStruct newGpsCoordinates = e.location;
        float deltaAccuracy = e.horizontalAccuracy - previousGPSAccuracy;

        if (e.horizontalAccuracy < movementMinimumGPSAccuracy || deltaAccuracy <= 5f)
        {

            double distanceBetween = MathTools.DistanceBetweenPoints(previousGPSCoordinates, newGpsCoordinates);

            if (distanceBetween > minDistanceBeforeUpdate)
            {
                newGPSHeading = Mathf.Rad2Deg * MathTools.BearingFromPointAToB(previousGPSCoordinates, newGpsCoordinates);
                //Convert to 0 - 360
                newGPSHeading = MathTools.DegAngle0To360(newGPSHeading);
                isGPSHeadingUsable = true;
            }
        }

        if (isHeadingUsable && isGPSHeadingUsable)
        {
            arHeading = newARHeading;
            gpsHeading = newGPSHeading;
            previousGPSCoordinates = newGpsCoordinates;
            previousARCameraPosition = newPosition;
            previousGPSAccuracy = e.horizontalAccuracy;

            float diff = CalculateGPSARHeadingDifference(arHeading, gpsHeading);

            if (lastGPSARHeadingDeltas.Count == 0)
            {
                initialGPSARDifference = diff;
                lastGPSARHeadingDeltas.Add(0);
            }
            else
            {
                if (lastGPSARHeadingDeltas.Count >= maxLastMovementHeadings)
                {
                    lastGPSARHeadingDeltas.RemoveAt(0);
                }

                float readingDelta = diff - initialGPSARDifference;

                if (readingDelta > 180)
                {
                    readingDelta -= 360;
                }
                else if (readingDelta < -180)
                {
                    readingDelta += 360;
                }

                lastGPSARHeadingDeltas.Add(readingDelta);
            }

            medianGPSARHeadingDifference = GetMedian(lastGPSARHeadingDeltas) + initialGPSARDifference;
            averageGPSARHeadingDifference = lastGPSARHeadingDeltas.Average() + initialGPSARDifference;

            hasGPSARHeading = true;

            RecalculateHeading();
            TriggerNorthHeadingUpdatedEvent(false);


            if (OnHeadingFromMovementUpdated != null)
            {
                HeadingFromMovementUpdatedEventArgs eventArgs = new HeadingFromMovementUpdatedEventArgs()
                {
                    arHeading = arHeading,
                    gpsHeading = gpsHeading,
                    headingDifference = diff,
                    medianHeadingDifference = medianGPSARHeadingDifference,
                    averageHeadingDifference = averageGPSARHeadingDifference
                };

                OnHeadingFromMovementUpdated(this, eventArgs);
            }
        }
    }


    // Calculates difference between given ARHeading and GPSHeading
    private float CalculateGPSARHeadingDifference(float arHeading, float gpsHeading)
    {
        return MathTools.DegAngleToPositive(arHeading - gpsHeading);
    }


    // Resets GPS-AR and Compass-mode datas and readings
    private void ResetBothModes()
    {
        ResetGPSARMode();
        ResetCompassMode();
    }


    // Reset GPS-AR -mode related variables
    private void ResetGPSARMode()
    {
        if (resetAfterTrackingLost)
        {
            hasGPSARHeading = false;
            manualOffset = 0;
            previousGPSCoordinates = null;
            previousGPSAccuracy = 9999f;
            initialGPSARDifference = 0;
            lastGPSARHeadingDeltas.Clear();
        }
    }


    // Reset Compass-mode related variables to default values
    private void ResetCompassMode()
    {
        if (resetAfterTrackingLost)
        {
            ResetCompassReadings();
            hasCompassHeading = false;
            manualOffset = 0;
            CalculationPhaseProp = CalculationPhase.Stopped;
        }
    }


    // Delete all previous Compass-mode readings
    private void ResetCompassReadings()
    {
        initialReading = 0;
        readingDeltas.Clear();
    }




    // Take a new compass true north heading recording.
    private bool RecordCompassHeading()
    {
        if (Mathf.Abs(headingProvider.HeadingSmoothVelocity) > 10f) return false;

        float reading = GetAngleFromCompassHeading(headingProvider.FilteredHeading);

        if (readingDeltas.Count == 0)
        {
            initialReading = reading;
            readingDeltas.Add(0f);
        }
        else
        {

            //Sample size reached, start removing old values
            if (HeadingSampleSizeReached)
            {
                readingDeltas.RemoveAt(0);
            }

            float readingDelta = reading - initialReading;

            if (readingDelta > 180)
            {
                readingDelta -= 360;
            }
            else if (readingDelta < -180)
            {
                readingDelta += 360;
            }

            readingDeltas.Add(readingDelta);


        }

        return true;
    }


    // Recalculate the new heading based on CalculationMode and calculated Compass- and GPS-AR -headings.
    private void RecalculateHeading()
    {
        float recalculatedHeading = 0;


        //If both headings are actually calculated
        if (hasCompassHeading && hasGPSARHeading)
        {
            //Average of two angles with Lerp
            recalculatedHeading = MathTools.DegAngle0To360(Mathf.LerpAngle(averageGPSARHeadingDifference, compassModeHeading, 0.5f));
        }
        //If we only have the compass heading calculated
        else if (hasCompassHeading)
        {
            recalculatedHeading = compassModeHeading;
        }
        //If we only have the GPS-AR -heading calculated
        else if (hasGPSARHeading)
        {
            recalculatedHeading = averageGPSARHeadingDifference;
        }



        heading = recalculatedHeading + manualOffset;

        headingTimestamp = DateTime.Now.Millisecond;

    }


    // Recalculates median Compass-heading from readings.

    private void RecalculateMedianCompassHeading()
    {
        if (readingDeltas != null && readingDeltas.Count > 0)
        {
            float median = GetMedian(readingDeltas);
            compassModeHeading = initialReading + median;

            hasCompassHeading = true;
        }
    }


    // Get the difference between compass heading and AR-camera rotation to indicate the difference.
    private float GetAngleFromCompassHeading(float heading)
    {
        float newRotY = 0;

        //Relative to Z-axis in Unity-coordinate system
        float cameraYRot = arCamera.transform.localRotation.eulerAngles.y;

        newRotY = cameraYRot - heading;
        newRotY = MathTools.DegAngleToPositive(newRotY);

        return newRotY;

    }


    // Trigger the NorthHeadingUpdatedEvent
    private void TriggerNorthHeadingUpdatedEvent(bool isPriority = false)
    {
        if (OnHeadingUpdated != null)
        {
            HeadingUpdatedEventArgs args = new HeadingUpdatedEventArgs()
            {
                heading = heading,
                timestamp = headingTimestamp,
                isPriority = isPriority
            };

            OnHeadingUpdated(this, args);
        }
    }


    public static float GetMedian(List<float> list)
    {
        float median = 0;

        List<float> sortedList = new List<float>(list);

        sortedList.Sort();

        if (sortedList.Count > 0)
        {
            if (sortedList.Count % 2 == 0)
            {
                int listMidIndex = sortedList.Count / 2;
                median = (sortedList[listMidIndex] + sortedList[listMidIndex - 1]) / 2f;
            }
            else
            {
                median = sortedList[Mathf.FloorToInt(sortedList.Count / 2)];
            }
        }

        return median;
    }

}
