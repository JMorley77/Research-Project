using Unity.Multiplayer.Center.Common;
using UnityEngine;
using Unity.Netcode;

public class CarSuspension : NetworkBehaviour
{

    public CarPreset preset;
    public float wheelRadius = 0.2f;
    public Transform Wheels;
    public Transform suspensionRay;


    private Rigidbody rb;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;
        Suspension();
    }

    private void Suspension()
    {
        float maxDistance = preset.rideHeight / 100f + wheelRadius;


        if (Physics.Raycast(suspensionRay.position, Vector3.down, out RaycastHit hit, maxDistance))
        {
            float compression = maxDistance - hit.distance;

            float springForce = compression * preset.springRate;

            float velocity = Vector3.Dot(rb.GetPointVelocity(suspensionRay.position), Vector3.up);

            float damperForce = -velocity * preset.damperRate;

            rb.AddForceAtPosition(Vector3.up * (springForce + damperForce), suspensionRay.position);


            Wheels.position = hit.point + (Vector3.up * wheelRadius);
        }
    } 

}




