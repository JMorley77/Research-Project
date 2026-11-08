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

    public float steering;
    public float throttle;
    public bool drifting;

    public CarSnapshot(Rigidbody rb,double timestamp, CarInputNetwork input)
    {
        timeStamp = timestamp;

        position = rb.position;
        rotation = rb.rotation;

        velocity = rb.linearVelocity;
        angularVelocity = rb.angularVelocity;

        steering = input.steering;
        throttle = input.throttle;
        drifting = input.drifting;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref timeStamp);

        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);

        serializer.SerializeValue(ref velocity);
        serializer.SerializeValue(ref angularVelocity);

        serializer.SerializeValue(ref steering);
        serializer.SerializeValue(ref throttle);
        serializer.SerializeValue(ref drifting);
    }
}