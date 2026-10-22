using UnityEngine;
using Unity.Netcode;

public class CarMovement : NetworkBehaviour
{
    public CarPreset carPreset;
    public float speed;
    Vector3 lastPosition;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsOwner)
            return;
        float distanceTravelled = Vector3.Distance(lastPosition,transform.position);
        speed = Mathf.RoundToInt(distanceTravelled / Time.deltaTime);
        lastPosition = transform.position;
    }
    private void FixedUpdate()
    {
        if (!IsOwner)
            return;
        Movement();
        Downforce();
    }
       
    private void Movement()
    {
        float throttle = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            throttle = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            throttle = -1f;
        }
        Vector3 velocity = rb.linearVelocity;
        Vector3 forwards = rb.rotation * Vector3.forward;
        
        float speed = Vector3.Dot(forwards, velocity);
        float driveForce = (carPreset.engineHP * carPreset.maxRPM / 10f) * 100f;

        driveForce = RetunDFImpact(driveForce);

        if(Mathf.Abs(throttle)> 0.01f)
        {
            rb.AddForce(forwards * driveForce * throttle * Time.fixedDeltaTime, ForceMode.Force);
        }
        

    }

    
    private void Downforce()
    {
        //if rearwing increases by 1
        //then
        //enginehp increase by 1
        //if(carPreset.rearWing)
        //{
        //    carPreset.engineHP++;
        //}
        rb.AddForce(carPreset.rearWing * rb.linearVelocity.magnitude * -transform.up);
    }
    float RetunDFImpact(float df)
    {
        float result = df + (df * (carPreset.rearWing / 100f));

        return result;
    }
    // this definately increases the speed but im not too sure about the downforce levels its got too much grip when turning
    // if the rear wing increases i want it so that it adds downforce to the car but if i decrease the rear wing it has less drag (increased top speed)

}
