using System.Globalization;
using UnityEngine;
using Unity.Netcode;

public class CarGhostController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CarSnapshotReciever snapshotReceiver;
    [SerializeField] private GameObject ghostVisualPrefab;
    [SerializeField] private Material ghostMaterial;

    [Header("Ghost Settings")]
    [SerializeField, Min(0.05f)]
    private float ghostAfterSeconds = 0.25f;

    private Renderer[] originalRenderers;
    private bool[] originalRendererStates;

    private GameObject ghostInstance;
    private Renderer[] ghostRenderers;

    private void Awake()
    {
        if (snapshotReceiver == null)
            snapshotReceiver = GetComponent<CarSnapshotReciever>();

        originalRenderers = GetComponentsInChildren<Renderer>(true);

        originalRendererStates = new bool[originalRenderers.Length];

        for (int i = 0; i < originalRenderers.Length; i++)
        {
            originalRendererStates[i] = originalRenderers[i].enabled;
        }
    }

    private void Update()
    {
        if (!IsSpawned || IsServer || IsOwner)
            return;

        if (snapshotReceiver == null ||
            !snapshotReceiver.hasSnapshot)
            return;

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
        CarSnapshot snapshot = snapshotReceiver.latestSnapshot;

        // Predict position using the last known velocity.
        Vector3 predictedPosition =
            snapshot.position + snapshot.velocity * age;

        // Predict rotation using the last known angular velocity.
        Quaternion predictedRotation = snapshot.rotation;

        float angularSpeed = snapshot.angularVelocity.magnitude;

        if (angularSpeed > 0.001f)
        {
            Quaternion rotationChange = Quaternion.AngleAxis(
                angularSpeed * age * Mathf.Rad2Deg,
                snapshot.angularVelocity.normalized
            );

            predictedRotation = rotationChange * snapshot.rotation;
        }

        ghostInstance.transform.SetPositionAndRotation(
            predictedPosition,
            predictedRotation
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
