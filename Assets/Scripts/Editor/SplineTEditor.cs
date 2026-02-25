using Codice.CM.Client.Gui;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

[CustomEditor(typeof(SplineContainer))]
public class SplineTEditor : Editor
{
    private void OnSceneGUI()
    {
        var container = (SplineContainer)target;
        var spline = container.Splines[0]; // Assumes first spline
        int knotCount = spline.Count;

        if (knotCount < 2) return;

        float totalLength = spline.GetLength();
        float cumulativeLength = 0f;

        Handles.color = Color.cyan;

        for (int i = 0; i < knotCount; i++)
        {
            Vector3 worldPos = container.transform.TransformPoint(spline[i].Position);
            float tAtKnot = cumulativeLength / totalLength;

            // Display t value at each knot
            Handles.Label(worldPos, $"t = {tAtKnot:F2}");

            // Accumulate segment length (except for last knot in open spline)
            if (!spline.Closed && i == knotCount - 1) continue;

            int nextIndex = (i + 1) % knotCount;
            BezierCurve curve = spline.GetCurve(i);
            cumulativeLength += spline.GetCurveLength(i);
        }
    }
}