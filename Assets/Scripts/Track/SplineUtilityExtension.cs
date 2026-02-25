using Unity.Mathematics;
using UnityEngine.Splines;

public static class SplineUtilityExtension
{
    public static float GetNearestPoint<T>(
      T spline,
      float3 point,
      out float3 nearest,
      out float t,
      float searchMin = 0,
      float searchMax = 1,
      int resolution = SplineUtility.PickResolutionDefault,
      int iterations = 2
  ) where T : ISpline
    {
        float distance = float.PositiveInfinity;
        nearest = float.PositiveInfinity;
        searchMin = math.max(0, math.min(searchMin, 1));
        searchMax = math.max(searchMin, math.min(searchMax, 1));
        float2 segment = new float2(searchMin, math.max(0, searchMax - searchMin)); //  new float2(searchMin, searchMax)
        t = 0f;
        int res = math.min(math.max(SplineUtility.PickResolutionMin, resolution), SplineUtility.PickResolutionMax);

        for (int i = 0, c = math.min(10, iterations); i < c; i++)
        {
            int segments = SplineUtility.GetSubdivisionCount(spline.GetLength() * segment.y, res); // without * segment.y
            segment = GetNearestPointInternal(spline, point, segment, out distance, out nearest, out t, segments);
        }

        return distance;
    }

    static float2 GetNearestPointInternal<T>(
        T spline,
        float3 point,
        float2 range,
        out float distance,
        out float3 nearest,
        out float time,
        int segments
    ) where T : ISpline
    {
        distance = float.PositiveInfinity;
        nearest = float.PositiveInfinity;
        time = float.PositiveInfinity;
        float2 segment = new float2(-1f, 0f);

        float t0 = range.x;
        float3 a = SplineUtility.EvaluatePosition(spline, t0);

        for (int i = 1; i < segments; i++)
        {
            float t1 = range.x + (range.y * (i / (segments - 1f)));
            float3 b = SplineUtility.EvaluatePosition(spline, t1);
            var p = SplineMath.PointLineNearestPoint(point, a, b, out var lineParam);
            float dsqr = math.distancesq(p, point);

            if (dsqr < distance)
            {
                segment.x = t0;
                segment.y = t1 - t0;
                time = segment.x + segment.y * lineParam;
                distance = dsqr;
                nearest = p;
            }

            t0 = t1;
            a = b;
        }

        distance = math.sqrt(distance);
        return segment;
    }
}
