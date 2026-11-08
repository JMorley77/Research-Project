using UnityEngine;
using Unity.Netcode;

public class CarSnapshotSender : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CarInputNetwork carInput;
    [SerializeField] private CarSnapshotReciever receiver;

    [SerializeField, Min(0.02f)]
    private float snapshotInterval = 0.05f;

    private float elapsed;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (carInput == null)
            carInput = GetComponent<CarInputNetwork>();

        if (receiver == null)
            receiver = GetComponent<CarSnapshotReciever>();
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (rb == null || carInput == null)
            return;

        elapsed += Time.fixedDeltaTime;

        if (elapsed < snapshotInterval)
            return;

        elapsed = 0f;

        CarSnapshot snapshot = new CarSnapshot(rb,NetworkManager.ServerTime.Time,carInput);

        DistributeSnapshotRpc(snapshot);
    }

    [Rpc(SendTo.NotServer)]
    private void DistributeSnapshotRpc(CarSnapshot snapshot)
    {
        if (receiver == null)
            return;

        receiver.ReceiveSnapshot(snapshot);
    }
}
