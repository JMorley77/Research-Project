using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class CarInputNetwork : NetworkBehaviour
{
    public float throttle;
    public float steering;
    public bool drifting;

    void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        float throttle = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            throttle = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            throttle = -1f;
        }

        float steering = 0f;

        if (Input.GetKey(KeyCode.D))
        {
            steering = 1f;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            steering = -1f;
        }

        bool drifting = Input.GetKey(KeyCode.Space);

        if (IsServer)
            SetInput(throttle, steering, drifting);
        else
            SendInputRpc(throttle, steering, drifting);
    }

    [Rpc(SendTo.Server)]
    void SendInputRpc(float throttle, float steering, bool drifting)
    {
        SetInput(throttle, steering, drifting);
    }

    void SetInput(float throttle, float steering, bool drifting)
    {
        this.throttle = Mathf.Clamp(throttle, -1f, 1f);
        this.steering = Mathf.Clamp(steering, -1f, 1f);
        this.drifting = drifting;
    }

    public override void OnNetworkDespawn()
    {
        throttle = 0f;
        steering = 0f;
        drifting = false;
    }
}
