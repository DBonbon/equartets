using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;

    private void Awake()
    {
        serverBtn.onClick.AddListener(ToggleServer);
        hostBtn.onClick.AddListener(ToggleHost);
        clientBtn.onClick.AddListener(ToggleClient);
    }

    private void ToggleServer()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            CleanupAndShutdown();
        }
        else
        {
            NetworkManager.Singleton.StartServer();
        }
    }

    private void ToggleHost()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            CleanupAndShutdown();
        }
        else
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    private void ToggleClient()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            CleanupAndShutdown();
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    private void CleanupAndShutdown()
    {
        PlayerManager.Instance?.CleanupPlayers();
        NetworkManager.Singleton.Shutdown();
    }
}
