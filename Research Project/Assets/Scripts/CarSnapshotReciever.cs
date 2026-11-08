using Unity.Netcode;
using UnityEngine;

public class CarSnapshotReciever : NetworkBehaviour
{
    public CarSnapshot latestSnapshot { get; private set; }

    public bool hasSnapshot { get; private set; }

    public double lastReceiveTime { get; private set; }

    public void ReceiveSnapshot(CarSnapshot snapshot)
    {
        // Only process snapshots on clients
        if (IsServer)
            return;

        // Ignore older snapshots
        if (hasSnapshot && snapshot.timeStamp <= latestSnapshot.timeStamp)
            return;

        latestSnapshot = snapshot;

        lastReceiveTime = Time.realtimeSinceStartupAsDouble;

        hasSnapshot = true;
    }

    public double GetSnapshotAge()
    {
        if (!hasSnapshot)
            return 0;

        return NetworkManager.LocalTime.Time - latestSnapshot.timeStamp;
    }

    public double GetTimeSinceLastReceive()
    {
        if (!hasSnapshot)
            return 0;

        return Time.realtimeSinceStartupAsDouble- lastReceiveTime;
    }

    private void Update()
    {
        if (!IsSpawned || IsServer || !hasSnapshot)
            return;

        Debug.Log(
            "Snapshot age: " + GetSnapshotAge().ToString("F3") +
            " | Velocity: " + latestSnapshot.velocity
        );
    }
}
