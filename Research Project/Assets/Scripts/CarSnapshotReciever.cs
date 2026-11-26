using Unity.Netcode;
using UnityEngine;

public class CarSnapshotReciever : NetworkBehaviour
{
    public CarSnapshot latestSnapshot { get; private set; }

    public bool hasSnapshot { get; private set; }

    public double lastReceiveTime { get; private set; }

    private bool hasReceivedSnapshot;


    public void ReceiveSnapshot(CarSnapshot snapshot)
    {

        // Ignore older snapshots
        if (hasSnapshot && snapshot.timeStamp <= latestSnapshot.timeStamp)
            return;

        latestSnapshot = snapshot;

        lastReceiveTime = Time.realtimeSinceStartupAsDouble;

        hasSnapshot = true;
        hasReceivedSnapshot = true;
    }

    public double GetSnapshotAge()
    {
        if (!hasReceivedSnapshot)
            return 0;

        return Time.realtimeSinceStartupAsDouble - lastReceiveTime;
    }

    public float GetTimeSinceLastReceive()
    {
        if (!hasReceivedSnapshot)
            return float.MaxValue;

        return (float)(Time.realtimeSinceStartupAsDouble - lastReceiveTime);
    }
    public bool HasReceivedSnapshot()
    {
        return hasReceivedSnapshot;
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
