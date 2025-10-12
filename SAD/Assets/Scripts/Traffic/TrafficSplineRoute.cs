using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class TrafficSplineRoute : MonoBehaviour
{
    [Header("Spline")]
    public SplineContainer container;
    [Range(32, 1024)] public int samples = 256; // resolução da LUT
    public bool loop = true;
    [Tooltip("Qual spline do container usar (0 = primeira)")]
    public int splineIndex = 0;

    // LUT
    [System.NonSerialized] public List<float> cumulLengths = new List<float>();
    [System.NonSerialized] public List<float> ts = new List<float>();
    [System.NonSerialized] public float totalLength;

    void OnEnable() { RebuildLUT(); }
    void OnValidate() { RebuildLUT(); }

    public void RebuildLUT()
    {
        cumulLengths.Clear();
        ts.Clear();
        totalLength = 0f;

        if (container == null) return;
        var count = Mathf.Max(2, samples);

        Vector3 prev = EvaluatePosition(0f);
        cumulLengths.Add(0f);
        ts.Add(0f);

        for (int i = 1; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector3 p = EvaluatePosition(t);
            totalLength += Vector3.Distance(prev, p);
            cumulLengths.Add(totalLength);
            ts.Add(t);
            prev = p;
        }
        if (totalLength <= Mathf.Epsilon)
            totalLength = 0.001f;
    }

    // Avalia posição no SplineContainer, independente da versão (fallback: derivar tangente por delta t)
    public Vector3 EvaluatePosition(float tNorm)
    {
        tNorm = Mathf.Repeat(tNorm, 1f);
        // API recente expõe EvaluatePosition/Rotation em SplineContainer; fallback por delta-t
        try
        {
            return container.EvaluatePosition(splineIndex, tNorm);
        }
        catch
        {
            // fallback simples: aproximar com delta
            float dt = 0.001f;
            Vector3 a = container.EvaluatePosition(tNorm);
            return a;
        }
    }

    public Vector3 EvaluateTangent(float tNorm)
    {
        tNorm = Mathf.Repeat(tNorm, 1f);
        try
        {
            Vector3 tan = container.EvaluateTangent(splineIndex, tNorm);
            if (tan.sqrMagnitude < 1e-4f)
            {
                float dt = 1f / samples;
                Vector3 p0 = EvaluatePosition(tNorm);
                Vector3 p1 = EvaluatePosition(tNorm + dt);
                tan = (p1 - p0).normalized;
            }
            return tan;
        }
        catch
        {
            float dt = 1f / samples;
            Vector3 p0 = EvaluatePosition(tNorm);
            Vector3 p1 = EvaluatePosition(tNorm + dt);
            return (p1 - p0).normalized;
        }
    }

    // Converte distância [0..totalLength] para t [0..1] via busca binária na LUT
    public float DistanceToT(float distance)
    {
        if (cumulLengths.Count == 0) RebuildLUT();
        if (loop)
            distance = Mathf.Repeat(distance, totalLength);
        else
            distance = Mathf.Clamp(distance, 0f, totalLength);

        int lo = 0, hi = cumulLengths.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (cumulLengths[mid] < distance) lo = mid + 1; else hi = mid;
        }
        int i1 = lo;
        int i0 = Mathf.Max(0, i1 - 1);

        float d0 = cumulLengths[i0];
        float d1 = cumulLengths[i1];
        float t0 = ts[i0];
        float t1 = ts[i1];

        float f = (d1 - d0) > 0f ? (distance - d0) / (d1 - d0) : 0f;
        return Mathf.Lerp(t0, t1, f);
    }
}