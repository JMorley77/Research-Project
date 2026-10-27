using UnityEngine;
using Unity.Netcode;
using System;

[Serializable]
public struct CarSnapshot : INetworkSerializable
{
    public double timeStamp;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;

    public CarSnapshot(Rigidbody rb, double timestamp)
    {
        timeStamp = timestamp;
        position = rb.position;
        rotation = rb.rotation;
        velocity = rb.linearVelocity;
        angularVelocity = rb.angularVelocity;
    }

    public void NetworkSerialize<t>(BufferSerializer<t> serializer) where t : IReaderWriter
    {
        serializer.SerializeValue(ref timeStamp);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
        serializer.SerializeValue(ref velocity);
        serializer.SerializeValue(ref angularVelocity);
    }
}
