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
        Debug.Log("ClientLag START running");

        var playerTags = CurrentPlayer.Tags;

        Debug.Log("Player tags: " + string.Join(", ", playerTags));

        NetworkSimulator simulator =
            GetComponent<NetworkSimulator>();

        Debug.Log("Simulator found: " + (simulator != null));

        if (simulator == null)
        {
            Debug.LogError(
                "Network Simulator is missing from NetworkManager."
            );
            return;
        }

        bool isLaggedClient =
            playerTags.Contains("LaggedClient");

        Debug.Log("Is lagged client: " + isLaggedClient);

        if (isLaggedClient)
        {
            simulator.ConnectionPreset =
                NetworkSimulatorPresets.Mobile2_5G;

            Debug.Log("LAG SIMULATION ENABLED FOR THIS PLAYER");
        }
        else
        {
            simulator.ConnectionPreset =
                NetworkSimulatorPresets.None;

            Debug.Log("Normal network connection");
        }
    }
}