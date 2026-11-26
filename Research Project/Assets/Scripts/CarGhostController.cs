using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CarGhostController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CarSnapshotReciever snapshotReceiver;
    [SerializeField] private GameObject ghostVisualPrefab;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private CarInputNetwork carInputNetwork;
    [SerializeField] private SplineAnalyser splineAnalyser;
    [SerializeField] private Rigidbody carRigidbody;

    [Header("Spline Movement")]
    [SerializeField, Min(1f)]
    private float ghostAcceleration = 12f;

    [SerializeField, Min(1f)]
    private float ghostMaxSpeed = 35f;

    [SerializeField, Min(0f)]
    private float ghostCoastDeceleration = 4f;

    private float ghostForwardSpeed;
    private float ghostThrottle;

    [Header("Ghost Settings")]
    [SerializeField, Min(0.05f)]
    private float ghostAfterSeconds = 0.25f;

    [SerializeField, Min(0.1f)]
    private float ghostRecoveryDelay = 0.5f;

    [Header("Smoothing")]
    [SerializeField, Min(0.01f)]
    private float ghostPositionSmoothTime = 0.15f;

    [SerializeField, Min(0.1f)]
    private float ghostRotationSmoothSpeed = 8f;

    [Header("Track Limits")]
    [SerializeField, Min(1f)]
    private float maxGhostDistanceFromTrack = 3f;

    [SerializeField, Min(1f)]
    private float carRecoveryDistance = 12f;

    [SerializeField, Min(0.1f)]
    private float recoveryCooldown = 2f;

    [SerializeField]
    private float recoveryHeightOffset = 0.5f;

    private float recoveryCooldownTimer;


    private float recoveryTimer;
    private bool ghostActive;

    private Vector3 smoothedPosition;
    private Vector3 positionSmoothVelocity;
    private Quaternion smoothedRotation;
    private bool hasSmoothedPosition;

    private Renderer[] originalRenderers;
    private bool[] originalRendererStates;

    private GameObject ghostInstance;
    private Renderer[] ghostRenderers;

    private void Awake()
    {
        if (carInputNetwork == null)
            carInputNetwork = GetComponent<CarInputNetwork>();

        if (snapshotReceiver == null)
            snapshotReceiver = GetComponent<CarSnapshotReciever>();

        // SplineAnalyser is on the scene's Spline object,
        // not on the car prefab.
        if (splineAnalyser == null)
            splineAnalyser = FindObjectOfType<SplineAnalyser>();

        if (carRigidbody == null)
            carRigidbody = GetComponent<Rigidbody>();

        originalRenderers =
            GetComponentsInChildren<Renderer>(true);

        originalRendererStates =
            new bool[originalRenderers.Length];

        for (int i = 0; i < originalRenderers.Length; i++)
        {
            originalRendererStates[i] =
                originalRenderers[i].enabled;
        }
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        // Server handles ghosting for remote-owned cars.
        if (IsServer)
        {
            // Don't ghost the server's own car.
            if (IsOwner)
                return;

            if (carInputNetwork == null)
                return;

            float inputAge = carInputNetwork.GetInputAge();

            if (inputAge >= ghostAfterSeconds)
            {
                ActivateGhost();
                UpdateGhostPosition(inputAge);
            }
            else
            {
                if (ghostActive)
                {
                    TransferRealCarToGhostPosition();
                }

                DeactivateGhost();
            }

            return;
        }

        // Client-side ghosting for cars owned by other players.
        if (IsOwner)
            return;

        if (snapshotReceiver == null ||
            !snapshotReceiver.hasSnapshot)
        {
            DeactivateGhost();
            return;
        }

        float snapshotAge =
            (float)snapshotReceiver.GetSnapshotAge();

        if (snapshotAge >= ghostAfterSeconds)
        {
            ActivateGhost();
            UpdateGhostPosition(snapshotAge);
        }
        else
        {
            DeactivateGhost();
        }
    }

    private void ActivateGhost()
    {
        if (ghostVisualPrefab == null)
            return;

        if (ghostInstance == null)
        {
            ghostInstance = Instantiate(ghostVisualPrefab);

            ghostRenderers =
                ghostInstance.GetComponentsInChildren<Renderer>(true);

            ApplyGhostMaterial();
        }

        if (!ghostActive)
        {
            ghostInstance.SetActive(true);
            SetOriginalRenderers(false);

            ghostActive = true;

            // The first position should be set directly.
            // Subsequent frames will be smoothed.
            hasSmoothedPosition = false;
            positionSmoothVelocity = Vector3.zero;
        }
    }

    private void UpdateGhostPosition(float age)
    {
        if (ghostInstance == null)
            return;

        Vector3 startPosition;
        Quaternion startRotation;
        Vector3 velocity;

        if (IsServer)
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            velocity = carRigidbody != null
                ? carRigidbody.linearVelocity
                : Vector3.zero;

            ghostThrottle = carInputNetwork != null
                ? carInputNetwork.CurrentThrottle
                : 0f;
        }
        else
        {
            if (snapshotReceiver == null ||
                !snapshotReceiver.hasSnapshot)
                return;

            CarSnapshot snapshot =
                snapshotReceiver.latestSnapshot;

            startPosition = snapshot.position;
            startRotation = snapshot.rotation;
            velocity = snapshot.velocity;

            ghostThrottle = snapshot.throttle;
        }

        // Initialize the ghost once when it first appears.
        if (!hasSmoothedPosition)
        {
            ghostForwardSpeed = velocity.magnitude;

            if (splineAnalyser != null &&
                splineAnalyser.PredictAlongRacingLine(
                    startPosition,
                    ghostForwardSpeed,
                    age,
                    out Vector3 initialPosition))
            {
                smoothedPosition = initialPosition;
            }
            else
            {
                smoothedPosition =
                    startPosition + velocity * age;
            }

            smoothedRotation = startRotation;
            positionSmoothVelocity = Vector3.zero;
            hasSmoothedPosition = true;
        }

        // Continue accelerating the ghost while throttle is applied.
        if (ghostThrottle > 0.05f)
        {
            ghostForwardSpeed +=
                ghostAcceleration *
                ghostThrottle *
                Time.deltaTime;
        }
        else
        {
            // Gradually slow down instead of stopping instantly.
            ghostForwardSpeed = Mathf.MoveTowards(
                ghostForwardSpeed,
                0f,
                ghostCoastDeceleration * Time.deltaTime
            );
        }

        ghostForwardSpeed = Mathf.Clamp(
            ghostForwardSpeed,
            0f,
            ghostMaxSpeed
        );

        Vector3 targetPosition = smoothedPosition;
        Quaternion targetRotation = smoothedRotation;
        targetPosition = ClampPositionToTrack(targetPosition);

        // Advance from the ghost's own current position.
        // This prevents every frame from resetting to the stale car.
        if (splineAnalyser != null &&
            splineAnalyser.PredictAlongRacingLine(
                smoothedPosition,
                ghostForwardSpeed,
                Time.deltaTime,
                out Vector3 nextSplinePosition))
        {
            targetPosition = nextSplinePosition;

            if (splineAnalyser.FindNearestPoint(targetPosition))
            {
                Vector3 direction =
                    splineAnalyser.trackDirection.normalized;

                if (direction.sqrMagnitude > 0.001f)
                {
                    targetRotation = Quaternion.LookRotation(
                        direction,
                        Vector3.up
                    );
                }
            }
        }
        else
        {
            // Fallback if spline prediction fails.
            targetPosition +=
                transform.forward *
                ghostForwardSpeed *
                Time.deltaTime;
        }
        targetPosition = ClampPositionToTrack(targetPosition);
        // Smooth position.
        smoothedPosition = Vector3.SmoothDamp(
            smoothedPosition,
            targetPosition,
            ref positionSmoothVelocity,
            ghostPositionSmoothTime
        );

        // Smooth rotation.
        float rotationLerp =
            1f - Mathf.Exp(
                -ghostRotationSmoothSpeed * Time.deltaTime
            );

        smoothedRotation = Quaternion.Slerp(
            smoothedRotation,
            targetRotation,
            rotationLerp
        );
        smoothedPosition = ClampPositionToTrack(smoothedPosition);
        ghostInstance.transform.SetPositionAndRotation(
            smoothedPosition,
            smoothedRotation
        );
    }
    private void TransferRealCarToGhostPosition()
    {
        // Only the server can change the authoritative car position.
        if (!IsServer || ghostInstance == null || !ghostActive)
            return;

        Vector3 finalPosition = ghostInstance.transform.position;
        Quaternion finalRotation = ghostInstance.transform.rotation;

        float currentSpeed = 0f;

        if (carRigidbody != null)
            currentSpeed = carRigidbody.linearVelocity.magnitude;

        // Synchronize the new position to clients.
        NetworkTransform networkTransform =
            GetComponent<NetworkTransform>();

        if (networkTransform != null)
        {
            networkTransform.Teleport(
                finalPosition,
                finalRotation,
                transform.localScale
            );
        }
        else
        {
            transform.SetPositionAndRotation(
                finalPosition,
                finalRotation
            );
        }

        // Move the physics body to the same position.
        if (carRigidbody != null)
        {
            carRigidbody.position = finalPosition;
            carRigidbody.rotation = finalRotation;

            // Continue moving in the ghost's direction.
            carRigidbody.linearVelocity =
                finalRotation * Vector3.forward * currentSpeed;

            carRigidbody.angularVelocity = Vector3.zero;
        }
    }
    private void ApplyGhostMaterial()
    {
        if (ghostMaterial == null || ghostRenderers == null)
            return;

        foreach (Renderer ghostRenderer in ghostRenderers)
        {
            if (ghostRenderer == null)
                continue;

            Material[] materials =
                ghostRenderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = ghostMaterial;
            }

            ghostRenderer.sharedMaterials = materials;
        }
    }

    private void DeactivateGhost()
    {
        recoveryTimer = 0f;
        ghostForwardSpeed = 0f;
        ghostThrottle = 0f;
        if (ghostActive)
        {
            SetOriginalRenderers(true);
            ghostActive = false;
        }

        if (ghostInstance != null)
            ghostInstance.SetActive(false);

        hasSmoothedPosition = false;
        positionSmoothVelocity = Vector3.zero;
    }

    private void SetOriginalRenderers(bool enabled)
    {
        if (originalRenderers == null)
            return;

        for (int i = 0; i < originalRenderers.Length; i++)
        {
            if (originalRenderers[i] != null)
            {
                originalRenderers[i].enabled =
                    enabled && originalRendererStates[i];
            }
        }
    }
    private Vector3 ClampPositionToTrack(Vector3 position)
    {
        if (splineAnalyser == null)
            return position;

        if (!splineAnalyser.FindNearestPoint(position))
            return position;

        Vector3 trackPoint = splineAnalyser.nearestPoint;
        Vector3 trackDirection =
            splineAnalyser.trackDirection.normalized;

        Vector3 trackRight =
            Vector3.Cross(Vector3.up, trackDirection).normalized;

        float lateralOffset =
            Vector3.Dot(position - trackPoint, trackRight);

        lateralOffset = Mathf.Clamp(
            lateralOffset,
            -maxGhostDistanceFromTrack,
            maxGhostDistanceFromTrack
        );

        float verticalOffset =
            Vector3.Dot(position - trackPoint, Vector3.up);

        return trackPoint
            + trackRight * lateralOffset
            + Vector3.up * verticalOffset;
    }
    private void CheckOffTrackRecovery()
    {
        if (!IsServer || splineAnalyser == null)
            return;

        if (recoveryCooldownTimer > 0f)
        {
            recoveryCooldownTimer -= Time.deltaTime;
            return;
        }

        if (!splineAnalyser.FindNearestPoint(transform.position))
            return;

        float distanceFromTrack =
            splineAnalyser.distanceFromRacingLine;

        if (distanceFromTrack < carRecoveryDistance)
            return;

        Vector3 recoveryPosition =
            splineAnalyser.nearestPoint
            + Vector3.up * recoveryHeightOffset;

        Vector3 trackDirection =
            splineAnalyser.trackDirection.normalized;

        Quaternion recoveryRotation =
            Quaternion.LookRotation(trackDirection, Vector3.up);

        // Use the NetworkTransform teleport so the corrected
        // position is synchronized to connected clients.
        NetworkTransform networkTransform =
            GetComponent<NetworkTransform>();

        if (networkTransform != null)
        {
            networkTransform.Teleport(
                recoveryPosition,
                recoveryRotation,
                transform.localScale
            );
        }
        else
        {
            transform.SetPositionAndRotation(
                recoveryPosition,
                recoveryRotation
            );
        }

        if (carRigidbody != null)
        {
            float currentSpeed =
                carRigidbody.linearVelocity.magnitude;

            carRigidbody.position = recoveryPosition;
            carRigidbody.rotation = recoveryRotation;

            // Keep some forward momentum after recovery.
            carRigidbody.linearVelocity =
                trackDirection * Mathf.Min(currentSpeed, 10f);

            carRigidbody.angularVelocity = Vector3.zero;
        }

        recoveryCooldownTimer = recoveryCooldown;

        // Reset the ghost's prediction after the car is recovered.
        hasSmoothedPosition = false;
        positionSmoothVelocity = Vector3.zero;
    }
    public override void OnNetworkDespawn()
    {
        DeactivateGhost();
    }

    private void OnDestroy()
    {
        if (ghostInstance != null)
            Destroy(ghostInstance);
    }
}
