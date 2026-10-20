using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewCarPreset", menuName = "Car/Car Preset")]
public class CarPreset : ScriptableObject
{
    [Header("Powertrain:")]
    [Tooltip("Engine Power (HP)")]
    [Min(0f)]
    public float engineHP;
    [Tooltip("Maximum Revs Per Minute (RPM)")]
    [Min(0f)]
    public float maxRPM;
    [Tooltip("Differential (open = 0 or closed = 1)")]
    [Range(0f, 1f)]
    public float differential;

    [Header("Chassis:")]
    [Tooltip("Vehicle Weight (KG)")]
    [Min(0f)]
    public float weight;
    [Tooltip("Rear wing")]
    [Range(0f, 10f)]
    public int rearWing;
    [Tooltip("Camber")]
    [Range(0f, 10f)]
    public int Camber;


    [Header("Suspension")]
    [Tooltip("Ride Height (CM)")]
    [Range(0f, 100f)]
    public float rideHeight;
    [Tooltip("Spring rate in (N/M)")]
    [Range(1f, 15000f)]
    public int springRate;
    [Tooltip("Damper rate in (N.s/M)")]
    [Range(1f, 1000f)]
    public int damperRate;

    public bool drifting;
}
