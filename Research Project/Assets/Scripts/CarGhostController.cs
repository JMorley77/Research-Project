using System.Globalization;
using UnityEngine;
using Unity.Netcode;

public class CarGhostController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CarSnapshotReciever snapshotReceiver;
    [SerializeField] private GameObject ghostVisualPrefab;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private CarInputNetwork carInputNetwork;
    [SerializeField] private SplineAnalyser splineAnalyser;
    [SerializeField] private Rigidbody carRigidbody;

    [Header("Ghost Settings")]
    [SerializeField, Min(0.05f)]
    private float ghostAfterSeconds = 0.25f;

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

        if (splineAnalyser == null)
            splineAnalyser = GetComponent<SplineAnalyser>();

        if (carRigidbody == null)
            carRigidbody = GetComponent<Rigidbody>();

        originalRenderers = GetComponentsInChildren<Renderer>(true);

        originalRendererStates = new bool[originalRenderers.Length];

        for (int i = 0; i < originalRenderers.Length; i++)
        {
            originalRendererStates[i] = originalRenderers[i].enabled;
        }
    }

    private void Update()
    {
        if (!IsSpawned || IsOwner)
            return;

        if (carInputNetwork == null)
            return;

        float inputAge = carInputNetwork.GetInputAge();

        if (IsServer)
        {
            if (inputAge >= ghostAfterSeconds)
            {
                ActivateGhost();

                if (ghostInstance != null)
                {
                    UpdateGhostPosition(inputAge);
                }
            }
            else
            {
                DeactivateGhost();
            }

            return;
        }
        if (snapshotReceiver == null ||!snapshotReceiver.hasSnapshot)
        {
            DeactivateGhost();
            return;
        }

        float snapshotAge =
            (float)snapshotReceiver.GetSnapshotAge();

        if (snapshotAge >= ghostAfterSeconds)
        {
            ActivateGhost();

            if (ghostInstance != null)
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

        ghostInstance.SetActive(true);

        SetOriginalRenderers(false);
    }

    private void UpdateGhostPosition(float age)
    {
        if (ghostInstance == null)
            return;

        Vector3 startPosition;
        float speed;
        Quaternion startRotation;

        // Host/server: use the authoritative car state.
        if (IsServer)
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            if (carRigidbody != null)
                speed = carRigidbody.linearVelocity.magnitude;
            else
                speed = 0f;
        }
        // Clients: use the last received snapshot.
        else
        {
            if (snapshotReceiver == null ||
                !snapshotReceiver.hasSnapshot)
                return;

            CarSnapshot snapshot =
                snapshotReceiver.latestSnapshot;

            startPosition = snapshot.position;
            startRotation = snapshot.rotation;
            speed = snapshot.velocity.magnitude;
        }

        // Use the spline analyser to predict the ghost's position.
        if (splineAnalyser != null &&
            splineAnalyser.PredictAlongRacingLine(
                startPosition,
                speed,
                age,
                out Vector3 predictedPosition))
        {
            // Find the track direction at the predicted position.
            if (splineAnalyser.FindNearestPoint(predictedPosition))
            {
                Vector3 predictedDirection =
                    splineAnalyser.trackDirection.normalized;

                if (predictedDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion predictedRotation =
                        Quaternion.LookRotation(
                            predictedDirection,
                            Vector3.up
                        );

                    ghostInstance.transform.SetPositionAndRotation(
                        predictedPosition,
                        predictedRotation
                    );

                    return;
                }
            }
        }

        // Fallback: linear prediction if spline prediction fails.
        Vector3 fallbackPosition = startPosition;

        if (IsServer)
        {
            if (carRigidbody != null)
                fallbackPosition += carRigidbody.linearVelocity * age;
        }
        else
        {
            CarSnapshot snapshot =
                snapshotReceiver.latestSnapshot;

            fallbackPosition =
                snapshot.position + snapshot.velocity * age;
        }

        ghostInstance.transform.SetPositionAndRotation(
            fallbackPosition,
            startRotation
        );
    }

    private void ApplyGhostMaterial()
    {
        if (ghostMaterial == null)
            return;

        foreach (Renderer ghostRenderer in ghostRenderers)
        {
            Material[] materials = ghostRenderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = ghostMaterial;
            }

            ghostRenderer.sharedMaterials = materials;
        }
    }

    private void DeactivateGhost()
    {
        SetOriginalRenderers(true);

        if (ghostInstance != null)
            ghostInstance.SetActive(false);
    }

    private void SetOriginalRenderers(bool enabled)
    {
        for (int i = 0; i < originalRenderers.Length; i++)
        {
            if (originalRenderers[i] != null)
            {
                originalRenderers[i].enabled =
                    enabled && originalRendererStates[i];
            }
        }
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
