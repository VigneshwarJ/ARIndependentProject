using UnityEngine;
using System;

public class MathTools
{
     public static float DegToRad(float degrees) {
        return degrees * Mathf.Deg2Rad;
    }

    public static float RadToDeg(float rad) {
        return rad * Mathf.Rad2Deg;
    }

    public static float DegAngleToPositive(float degrees) {
        if (degrees < 0) {
            degrees += 360f;
        }

        return degrees;
    }
    
    public static float FlipDegAngleDirection(float degrees) {
        degrees = DegAngleToPositive(degrees);
        degrees = 360f - degrees;

        return degrees;
    }

    public static float DegAngle0To360(float angle) {
        if (angle < 0) {
            angle += 360f;
        } else if (angle >= 360f) {
            angle -= 360f;
        }

        return angle;
    }
    public static double DegToRad(double degrees) {
        return degrees/180 * System.Math.PI;
    }

    public static double RadToDeg(double rad) {
        return rad * 180 / System.Math.PI;
    }

    const double R = 6371000f; // radius of earth meters

    public static float DistanceBetweenPoints(LatitudeLongitudeStruct startPoint, LatitudeLongitudeStruct endPoint)
    {
        return DistanceBetweenPoints(startPoint.Lat, startPoint.Lon, endPoint.Lat, endPoint.Lon);
    }

    //https://www.igismap.com/haversine-formula-calculate-geographic-distance-earth/
    public static float DistanceBetweenPoints(double lat1, double long1, double lat2, double long2)
    {
        double startLatRad = DegToRad(lat1);
        double endLatRad = DegToRad(lat2);

        double deltaLatRad = DegToRad(lat2 - lat1);
        double deltaLonRad = DegToRad(long2 - long1);

        double a = System.Math.Sin(deltaLatRad / 2) * System.Math.Sin(deltaLatRad / 2) +
                    System.Math.Cos(startLatRad) * System.Math.Cos(endLatRad) *
                    System.Math.Sin(deltaLonRad / 2) * System.Math.Sin(deltaLonRad / 2);

        double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));

        float d = (float)(R * c);

        return d;
    }


    public static float BearingFromPointAToB(LatitudeLongitudeStruct position1, LatitudeLongitudeStruct position2)
    {

        return BearingFromPointAToB(position1.Lat, position1.Lon, position2.Lat, position2.Lon);
    }

    // <source>https://www.movable-type.co.uk/scripts/latlong.html</source>
    public static float BearingFromPointAToB(double lat1, double long1, double lat2, double long2)
    {
        double startPointLatRad = DegToRad(lat1);
        double startPointLonRad = DegToRad(long1);
        double endPointLatRad = DegToRad(lat2);
        double endPointLonRad = DegToRad(long2);

        double deltaLonRad = endPointLonRad - startPointLonRad;

        double y = System.Math.Sin(deltaLonRad) * System.Math.Cos(endPointLatRad);
        double x = System.Math.Cos(startPointLatRad) * System.Math.Sin(endPointLatRad) - System.Math.Sin(startPointLatRad) * System.Math.Cos(endPointLatRad) * System.Math.Cos(deltaLonRad);

        float bearing = (float)(System.Math.Atan2(y, x));

        return bearing;
    }

}
