using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Windows;

public class CarController : NetworkBehaviour
{
    public CarPreset preset;
    public GameObject leftBrakeLight;
    public GameObject rightBrakeLight;
    public GameObject driftSmoke;
    public Transform frontLeft;
    public Transform frontRight;
    private float steerAngle = 25f;
    public float maxSteerAngle = 50;
    public string currentSurface;

    private Rigidbody rb;
    private CarInputNetwork carInput;

    private NetworkVariable<float> networkSteering = new NetworkVariable<float>();
    private NetworkVariable<float> networkThrottle = new NetworkVariable<float>();
    private NetworkVariable<bool> networkDrifting = new NetworkVariable<bool>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carInput = GetComponent<CarInputNetwork>();

        rb.mass = preset.weight;
    }

    private void FixedUpdate()
    {
        if (!IsServer)
            return;
        Gravity.ApplyGravity(rb);
        Lights();
        checkGroundMaterial();
    }
    private void Update()
    {
        if (!IsSpawned)
            return;

        // Server updates values for everyone
        if (IsServer)
        {
            networkSteering.Value = carInput.steering;
            networkThrottle.Value = carInput.throttle;
            networkDrifting.Value = carInput.drifting;
        }

        // Everyone updates visuals
        Lights();
        DriftSmoke();
        AnimateWheels();
    }


    private void AnimateWheels()
    {
        steerAngle = networkSteering.Value * maxSteerAngle;

        frontLeft.localRotation = Quaternion.Euler(steerAngle, 0, 0);
        frontRight.localRotation = Quaternion.Euler(-steerAngle, 0, 0);
    }

    private void Lights()
    {
        bool braking = networkThrottle.Value <= 0f;

        leftBrakeLight.SetActive(braking);
        rightBrakeLight.SetActive(braking);
    }
    private void DriftSmoke()
    {
        driftSmoke.SetActive(networkDrifting.Value && preset.drifting);
    }



    private void checkGroundMaterial()
    {

        if(Physics.Raycast(transform.position,Vector3.down,out RaycastHit hit, 2f))
        {
            currentSurface = hit.collider.tag;
            if (currentSurface == "grass")
            {
                rb.mass = preset.weight + 300f;
                //make the player slide if i have time will need to link up to the car steering side.
            }
            else 
            {
                rb.mass = preset.weight;
            }
        }
    }

}
