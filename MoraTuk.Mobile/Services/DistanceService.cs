namespace MoraTuk.Mobile.Services;

public class DistanceService
{
    public double Calculate(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double R = 6371; // km

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRad(lat1)) *
            Math.Cos(ToRad(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return R * c;
    }


    private double ToRad(double value)
    {
        return value * Math.PI / 180;
    }
}