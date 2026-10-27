using UnityEngine;
using Unity.Netcode;

public class CarSnapshotSender : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;

    [SerializeField, Min(0.02f)] private float snapshotInterval;

    [SerializeField] private float elapsed;

    private void Awake()
    {
        if(rb == null)
        {
            rb.GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || rb == null)
            return;

        elapsed += Time.deltaTime;

        if (elapsed < snapshotInterval)
            return;

        elapsed = 0f;

        var snapshot = new CarSnapshot(rb, Time.realtimeSinceStartupAsDouble);
        RecieveSnapshotRpc(snapshot);
    }

    [Rpc(SendTo.Server)]
    private void RecieveSnapshotRpc(CarSnapshot snapshot)
    {
        Debug.Log("snapshot Recieved, " +
            "TimeStamp: " + $"{snapshot.timeStamp}, " +
            "Position:" + $" {snapshot.position} " +
            "Rotation: " + $"{snapshot.rotation}");

    }

}
