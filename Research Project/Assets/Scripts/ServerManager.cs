using UnityEngine;
using Unity.Netcode;

public class ServerManager : NetworkBehaviour
{
    
    public void HostGame()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void JoinClient()
    {
        NetworkManager.Singleton.StartClient();

    }

    public void LeaveGame()
    {
        NetworkManager.Singleton.Shutdown();
    }
}
