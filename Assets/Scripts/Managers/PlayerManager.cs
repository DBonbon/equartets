using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;
    private int connectedPlayers = 0;
    private bool gameInitialized = false;

    // Server-only lists
    public List<PlayerNetwork> players = new List<PlayerNetwork>();
    public List<PlayerController> playerControllers = new List<PlayerController>();
    private List<PlayerData> playerDataList;

    [SerializeField] private Transform uiCanvas;
    private Dictionary<ulong, string> clientIdToPlayerName = new Dictionary<ulong, string>();
    private Dictionary<string, ulong> playerNameToClientId = new Dictionary<string, ulong>();

    private void Awake()
    {
        Debug.Log($"[PLAYERMANAGER-INSTANCE] Awake called - IsServer: {NetworkManager.Singleton?.IsServer ?? false}, IsClient: {NetworkManager.Singleton?.IsClient ?? false}");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DataManager.OnPlayerDataLoaded += LoadPlayerDataLoaded;
            Debug.Log($"[PLAYERMANAGER-INSTANCE] Created PlayerManager instance");
        }
        else
        {
            Debug.Log($"[PLAYERMANAGER-INSTANCE] Destroying duplicate PlayerManager instance");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        DataManager.OnPlayerDataLoaded -= LoadPlayerDataLoaded;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // PRE-SERVER: Data loading (runs on all instances before networking)
    public void LoadPlayerDataLoaded(List<PlayerData> loadedPlayerDataList)
    {
        this.playerDataList = loadedPlayerDataList;
        Debug.Log($"PlayerManager: Loaded {playerDataList?.Count ?? 0} players data");
    }

    private void Start()
    {
        Debug.Log($"[PLAYERMANAGER-INSTANCE] Start called - IsServer: {IsServer}, IsClient: {IsClient}");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            Debug.Log($"[PLAYERMANAGER-INSTANCE] Subscribed to OnClientConnected");
        }
    }

    public void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[PLAYERMANAGER-INSTANCE] OnClientConnected called with ClientId: {clientId} - IsServer: {IsServer}");

        // CRITICAL: Only server handles client connections
        if (!IsServer || gameInitialized)
        {
            Debug.Log($"[PLAYERMANAGER-INSTANCE] SKIPPED OnClientConnected - IsServer: {IsServer}, gameInitialized: {gameInitialized}");
            return;
        }

        Debug.Log($"[PLAYERMANAGER-INSTANCE] PROCESSING OnClientConnected - Server handling client {clientId}");
        connectedPlayers++;

        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (playerObject != null)
        {
            var playerNetwork = playerObject.GetComponent<PlayerNetwork>();
            if (playerNetwork != null && connectedPlayers <= playerDataList.Count)
            {
                var playerData = playerDataList[connectedPlayers - 1];

                // Add to server lists
                players.Add(playerNetwork);

                // Get or create PlayerController
                var playerController = playerObject.gameObject.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    playerController = playerObject.gameObject.AddComponent<PlayerController>();
                }

                // Initialize PlayerController on server
                playerController.Initialize(playerData);
                playerController.SetClientId(clientId);

                // SERVER CALCULATES positioning data
                int totalPlayers = playerDataList.Count;
                int connectionIndex = connectedPlayers - 1;

                playerControllers.Add(playerController);

                // SERVER SETS NetworkVariables (will sync to all clients)
                if (playerNetwork.IsServer)
                {
                    playerNetwork.ConnectionIndex.Value = connectionIndex;
                    playerNetwork.TotalPlayers.Value = totalPlayers;
                    Debug.Log($"[SERVER] Set positioning for {playerController.PlayerName}: index={connectionIndex}, total={totalPlayers}");
                }

                // Update server dictionaries
                clientIdToPlayerName[clientId] = playerController.PlayerName;
                playerNameToClientId[playerController.PlayerName] = clientId;

                Debug.Log($"[SERVER] Added player: {playerController.PlayerName} (ClientId: {clientId})");

                // Start game when all players connected
                if (players.Count == playerDataList.Count && !gameInitialized)
                {
                    gameInitialized = true;
                    StartGameLogic();
                }
            }
        }
    }

    [ClientRpc]
    private void SetCanvasReferenceForAllPlayers_ClientRpc(string canvasName)
    {
        Debug.Log($"[ClientRpc] SetCanvasReferenceForAllPlayers called, looking for canvas: {canvasName}");

        // Find the canvas by name on each client
        GameObject canvasObj = GameObject.Find(canvasName);
        if (canvasObj != null)
        {
            Transform canvasTransform = canvasObj.transform;

            var allPlayerNetworks = FindObjectsOfType<PlayerNetwork>();
            foreach (var playerNetwork in allPlayerNetworks)
            {
                var playerUI = playerNetwork.GetComponent<PlayerUI>();
                if (playerUI != null)
                {
                    Debug.Log($"[ClientRpc] Setting canvas reference for {playerNetwork.playerName.Value} to {canvasName}");
                    playerUI.SetCanvasReference(canvasTransform);
                }
            }
        }
        else
        {
            Debug.LogError($"[ClientRpc] Could not find canvas named '{canvasName}' on client");
        }
    }

    [ClientRpc]
    private void TestCanvasReference_ClientRpc()
    {
        Debug.Log("[TEST] Looking for Canvas in scene...");
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas != null)
        {
            Debug.Log($"[TEST] Found Canvas: {mainCanvas.name} at position {mainCanvas.transform.position}");

            var allPlayerNetworks = FindObjectsOfType<PlayerNetwork>();
            Debug.Log($"[TEST] Found {allPlayerNetworks.Length} PlayerNetwork objects");

            foreach (var playerNetwork in allPlayerNetworks)
            {
                var playerUI = playerNetwork.GetComponent<PlayerUI>();
                Debug.Log($"[TEST] Player: {playerNetwork.name}, has PlayerUI: {playerUI != null}");
            }
        }
        else
        {
            Debug.LogError("[TEST] No Canvas found in scene!");
        }
    }

    private void StartGameLogic()
    {
        if (!IsServer) return;

        Debug.Log("[SERVER] Starting game logic");

        TestCanvasReference_ClientRpc();
        // Set canvas reference for all players
        SetCanvasReferenceForAllPlayers_ClientRpc(uiCanvas.name);

        CardManager.Instance.DistributeCards(playerControllers);
        TurnManager.Instance.StartTurnManager();
    }

    // SERVER-ONLY: Cleanup
    public void CleanupPlayers()
    {
        if (!IsServer) return;

        players.Clear();
        playerControllers.Clear();
        clientIdToPlayerName.Clear();
        playerNameToClientId.Clear();
    }

    // UTILITY: Can be called from any instance
    public string GetPlayerNameByClientId(ulong clientId)
    {
        if (clientIdToPlayerName.TryGetValue(clientId, out string playerName))
        {
            return playerName;
        }
        else
        {
            return "Unknown Player";
        }
    }

    // conversion dbId and network id:
    public PlayerController FindPlayerByDbId(int playerDbId)
    {
        PlayerNetwork[] allPlayerNetworks = FindObjectsOfType<PlayerNetwork>();
        foreach (var playerNetwork in allPlayerNetworks)
        {
            if (playerNetwork.PlayerDbId.Value == playerDbId)
            {
                return playerNetwork.GetComponent<PlayerController>();
            }
        }
        return null;
    }

    public PlayerNetwork FindPlayerNetworkByDbId(int playerDbId)
    {
        PlayerNetwork[] allPlayerNetworks = FindObjectsOfType<PlayerNetwork>();
        foreach (var playerNetwork in allPlayerNetworks)
        {
            if (playerNetwork.PlayerDbId.Value == playerDbId)
            {
                return playerNetwork;
            }
        }
        return null;
    }

    private PlayerController FindPlayerControllerByClientId(ulong clientId)
    {
        Debug.Log($"[CLIENT-ANIMATION] Step 2A: Looking for ClientId: {clientId}");

        // Find PlayerNetwork with matching OwnerClientId, then get its PlayerController
        PlayerNetwork[] allPlayerNetworks = FindObjectsOfType<PlayerNetwork>();
        Debug.Log($"[CLIENT-ANIMATION] Step 2B: Found {allPlayerNetworks.Length} PlayerNetworks in scene");

        foreach (var playerNetwork in allPlayerNetworks)
        {
            Debug.Log($"[CLIENT-ANIMATION] Step 2C: PlayerNetwork {playerNetwork.playerName.Value} has OwnerClientId: {playerNetwork.OwnerClientId}");
            if (playerNetwork.OwnerClientId == clientId)
            {
                PlayerController playerController = playerNetwork.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    Debug.Log($"[CLIENT-ANIMATION] Step 2D: MATCH! Found PlayerController for: {playerNetwork.playerName.Value}");
                    return playerController;
                }
            }
        }

        return null;
    }

    private CardNetwork FindCardNetworkByIdOnClient(int cardId)
    {
        CardNetwork[] allCardNetworks = FindObjectsOfType<CardNetwork>();
        Debug.Log($"[CLIENT-ANIMATION] Found {allCardNetworks.Length} CardNetworks in scene");

        foreach (var cardNetwork in allCardNetworks)
        {
            if (cardNetwork.cardId.Value == cardId)
            {
                Debug.Log($"[CLIENT-ANIMATION] MATCH! Found card: {cardNetwork.cardName.Value}");
                return cardNetwork;
            }
        }
        return null;
    }

}