using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineAnalyser : MonoBehaviour
{
    [Header("Spline Reference")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Sampling Settings")]
    [SerializeField, Range(50, 1000)]
    private int sampleCount = 300;

    [Header("Debug")]
    [SerializeField] private Transform carToDebug;

    public Vector3 nearestPoint { get; private set; }
    public Vector3 trackDirection { get; private set; }
    public float curvature { get; private set; }
    public float distanceFromRacingLine { get; private set; }
    public float nearestSplineT { get; private set; }

    public bool FindNearestPoint(Vector3 worldPosition)
    {
        if (splineContainer == null || sampleCount < 2)
            return false;

        float closestDistance = float.MaxValue;
        float closestT = 0f;
        Vector3 closestPoint = Vector3.zero;

        // Find the closest sampled point on the spline.
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;

            float3 localPosition =
                SplineUtility.EvaluatePosition(
                    splineContainer.Spline, t);

            Vector3 sampledPosition =
                splineContainer.transform.TransformPoint(
                    new Vector3(
                        localPosition.x,
                        localPosition.y,
                        localPosition.z
                    )
                );

            float distance =
                Vector3.SqrMagnitude(worldPosition - sampledPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
                closestPoint = sampledPosition;
            }
        }

        nearestPoint = closestPoint;
        nearestSplineT = closestT;

        distanceFromRacingLine =
            Vector3.Distance(worldPosition, nearestPoint);

        // Calculate the track direction.
        trackDirection = GetWorldTangent(closestT).normalized;

        // Calculate approximate track curvature.
        float sampleOffset = 1f / sampleCount;

        Vector3 beforeTangent =
            GetWorldTangent(Mathf.Repeat(
                closestT - sampleOffset, 1f)).normalized;

        Vector3 afterTangent =
            GetWorldTangent(Mathf.Repeat(
                closestT + sampleOffset, 1f)).normalized;

        float angle = Vector3.Angle(
            beforeTangent, afterTangent);

        Vector3 beforePosition =
            GetWorldPosition(Mathf.Repeat(
                closestT - sampleOffset, 1f));

        Vector3 afterPosition =
            GetWorldPosition(Mathf.Repeat(
                closestT + sampleOffset, 1f));

        float distanceBetweenSamples =
            Vector3.Distance(beforePosition, afterPosition);

        curvature = angle / Mathf.Max(
            distanceBetweenSamples, 0.001f);

        return true;
    }

    private Vector3 GetWorldPosition(float t)
    {
        float3 position =
            SplineUtility.EvaluatePosition(
                splineContainer.Spline, t);

        return splineContainer.transform.TransformPoint(
            new Vector3(position.x, position.y, position.z));
    }

    private Vector3 GetWorldTangent(float t)
    {
        float3 tangent =
            SplineUtility.EvaluateTangent(
                splineContainer.Spline, t);

        return splineContainer.transform.TransformDirection(
            new Vector3(tangent.x, tangent.y, tangent.z));
    }

    public bool PredictAlongRacingLine(
    Vector3 worldPosition,
    float speed,
    float predictionTime,
    out Vector3 predictedPosition)
    {
        predictedPosition = worldPosition;

        if (!FindNearestPoint(worldPosition))
            return false;

        // How far the car is expected to travel.
        float targetDistance = Mathf.Max(0f, speed * predictionTime);

        // Preserve the car's position relative to the racing line.
        Vector3 lateralDirection =
            Vector3.Cross(Vector3.up, trackDirection).normalized;

        Vector3 offset = worldPosition - nearestPoint;

        float lateralOffset =
            Vector3.Dot(offset, lateralDirection);

        float verticalOffset =
            Vector3.Dot(offset, Vector3.up);

        Vector3 previousPosition = nearestPoint;

        float travelledDistance = 0f;
        float currentT = nearestSplineT;

        // Walk forward along the closed spline until we reach
        // the required prediction distance.
        for (int i = 0; i < sampleCount * 2; i++)
        {
            float nextT = Mathf.Repeat(
                currentT + 1f / sampleCount, 1f);

            Vector3 nextPosition = GetWorldPosition(nextT);

            float segmentDistance =
                Vector3.Distance(previousPosition, nextPosition);

            if (travelledDistance + segmentDistance >= targetDistance)
            {
                float remainingDistance =
                    targetDistance - travelledDistance;

                float segmentFraction =
                    remainingDistance / Mathf.Max(segmentDistance, 0.001f);

                Vector3 futurePoint = Vector3.Lerp(
                    previousPosition,
                    nextPosition,
                    segmentFraction);

                Vector3 futureTangent =
                    GetWorldTangent(nextT).normalized;

                Vector3 futureLateral =
                    Vector3.Cross(Vector3.up, futureTangent).normalized;

                predictedPosition =
                    futurePoint +
                    futureLateral * lateralOffset +
                    Vector3.up * verticalOffset;

                return true;
            }

            travelledDistance += segmentDistance;
            previousPosition = nextPosition;
            currentT = nextT;
        }

        return false;
    }

    private void Update()
    {
        if (carToDebug == null)
            return;

        if (FindNearestPoint(carToDebug.position))
        {
            Debug.DrawLine(
                carToDebug.position,
                nearestPoint,
                Color.cyan
            );

            Debug.DrawRay(
                nearestPoint,
                trackDirection * 5f,
                Color.green
            );
        }
        Rigidbody debugRb = carToDebug.GetComponent<Rigidbody>();

        if (debugRb != null)
        {
            float speed = debugRb.linearVelocity.magnitude;

            if (PredictAlongRacingLine(
                carToDebug.position,
                speed,
                0.5f,
                out Vector3 predictedPosition))
            {
                Debug.DrawLine(
                    carToDebug.position,
                    predictedPosition,
                    Color.magenta
                );

                Debug.DrawLine(
                    predictedPosition - Vector3.right * 0.5f,
                    predictedPosition + Vector3.right * 0.5f,
                    Color.magenta
                );

                Debug.DrawLine(
                    predictedPosition - Vector3.forward * 0.5f,
                    predictedPosition + Vector3.forward * 0.5f,
                    Color.magenta
                );
            }
        }
    }
}