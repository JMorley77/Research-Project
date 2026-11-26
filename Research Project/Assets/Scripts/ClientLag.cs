using System;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using System.Linq;
using UnityEngine;

public class ClientLag : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("lag start running");

        var playerTags = CurrentPlayer.Tags;

        Debug.Log("Player tags: " + string.Join(", ", playerTags));

        NetworkSimulator simulator = GetComponent<NetworkSimulator>();

        Debug.Log("found: " + (simulator != null));

        if (simulator == null)
        {
            Debug.LogError("here missing.");
            return;
        }

        bool isLaggedClient = playerTags.Contains("LaggedClient");

        Debug.Log("Is lagged client: " + isLaggedClient);

        if (isLaggedClient)
        {
            simulator.ConnectionPreset = NetworkSimulatorPresets.Mobile2_5G;

            Debug.Log("here enabled");
        }
        else
        {
            simulator.ConnectionPreset = NetworkSimulatorPresets.None;

            Debug.Log("normal connection");
        }
    }
}