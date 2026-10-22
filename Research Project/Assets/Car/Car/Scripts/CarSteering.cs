using UnityEngine;
using Unity.Netcode;

public class CarSteering : NetworkBehaviour
{
    private float steeringTorque = 1.5f;
    private float driftTorque = 2f;
    private float steerControl = 0.95f;
    private float ackermann = 2f;
    private float roll = 2f;
    private bool isDrifting;
    private float driftGrip;
    private float origionalHP;
    private float origionalDiff;
    public Transform chassis;
    private Rigidbody rb;
    public CarPreset carPreset;

    private CarInputNetwork carInput;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        carInput = GetComponentInParent<CarInputNetwork>();
        origionalHP = carPreset.engineHP;
        origionalDiff = carPreset.differential;
    }


    private void FixedUpdate()
    {
        if (!IsServer)
            return;
        SideVelocity();
        Steering();
        BodyRoll();
    }


    private void Steering()
    {
        float steer = carInput.steering;
        float ackermannSteering = steer* (1f + Mathf.Abs(steer)*(ackermann - 1f));
        rb.angularVelocity *= steerControl;

        if (carInput.drifting && carPreset.drifting)
        {            
            isDrifting = true;
            steeringTorque = driftTorque;
            steerControl = 0.96f;
            rb.linearDamping = 0.9f;
            driftGrip = 0.05f;
            carPreset.engineHP = origionalHP * 0.97f;
            carPreset.differential = origionalDiff * 0.0f;
        }
        else
        {
            isDrifting = false;
            steeringTorque = 1.5f;
            steerControl = 0.95f;
            rb.linearDamping = 1f;
            driftGrip = 0.1f;
            carPreset.engineHP = origionalHP;
            carPreset.differential = origionalDiff;
        }


            rb.AddTorque(transform.up * steeringTorque * ackermannSteering, ForceMode.Acceleration);
    }
    private void BodyRoll()
    {
        float yaw = rb.angularVelocity.y;
        chassis.localEulerAngles = new Vector3(0, 0, -yaw * roll);
    }
    private void SideVelocity()
    {
        Vector3 right = rb.rotation * Vector3.right;
        Vector3 side = Vector3.Project(rb.linearVelocity, right) * carPreset.differential;
        rb.linearVelocity -= side * driftGrip;
    }

}
// if the rear wing increases i want it so that it adds downforce to the car but if i decrease the rear wing it has less drag (increased top speed)
//variables should change based on preset fix hard coding values and let them be modular