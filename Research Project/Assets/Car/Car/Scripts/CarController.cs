using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = preset.weight;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;
        Gravity.ApplyGravity(rb);
        Lights();
        checkGroundMaterial();
    }
     private void Update()
    {
        if (!IsOwner)
            return;
        DriftSmoke();
        AnimateWheels();
    }


    private void AnimateWheels()
    {
        float input = Input.GetAxis("Horizontal");
        steerAngle = input * maxSteerAngle;
        frontLeft.localRotation = Quaternion.Euler(steerAngle, 0, 0);
        frontRight.localRotation = Quaternion.Euler(-steerAngle, 0, 0);
    }
        
    private void Lights()
    {
        if( Input.GetKey(KeyCode.W))
        {
            leftBrakeLight.SetActive(false);
            rightBrakeLight.SetActive(false);
        }
        else
        {
            leftBrakeLight.SetActive(true);
            rightBrakeLight.SetActive(true);
        }
    }
    private void DriftSmoke()
    {
        if (Input.GetKey(KeyCode.Space) && preset.drifting)
        {
            driftSmoke.SetActive(true);
        }
        else
        {
            driftSmoke.SetActive(false);
        }
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
