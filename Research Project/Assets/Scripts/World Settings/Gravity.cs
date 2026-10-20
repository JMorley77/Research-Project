using UnityEngine;
public static class Gravity
{
    public static void ApplyGravity(Rigidbody rb)
    {
        Vector3 gravity = new Vector3(0, -9.81f, 0);
        rb.AddForce(gravity, ForceMode.Acceleration);
    }
}
