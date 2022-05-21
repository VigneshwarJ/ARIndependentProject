using UnityEngine;

[System.Serializable]
public class LatitudeLongitudeStruct
{
    [SerializeField]
    private double lat;

    [SerializeField]
    private double lon;

    public double Lat { get { return lat; } set { lat = value; } }
    
    public double Lon { get { return lon; } set { lon = value; } }

    public LatitudeLongitudeStruct() {
        
    }

    public LatitudeLongitudeStruct(double lat, double lon) {
        this.lat = lat;
        this.lon = lon;
    }

    public override string ToString() {
        return string.Format("Lat: {0}, Lon: {1}", lat, lon);
    }
    
    /// Haversine distance in meters.
    public float DistanceToPoint(LatitudeLongitudeStruct otherPoint) {
        return MathTools.DistanceBetweenPoints(this, otherPoint);
    }
    
}
