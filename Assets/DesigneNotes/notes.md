# Game Development Notes

## Player Management
- Data is loaded by `DataManager`.
- `PlayerManager` assigns `PlayerData`.

## Network Synchronization
- Players send actions to the server using `[ServerRpc]`.
- Server validates and processes actions.
- State updates are broadcasted to all clients.


[DataManager Loads PlayerData] -- Assigns Data Sequentially --> |PlayerManager|
[PlayerPrefab with Player script] -- Registers --> |PlayerManager|
PlayerManager -- Assigns Sequential ID and PlayerData --> [Player]
[Player] -- Triggers --> |Instantiation of PlayerUIPrefab| -- UI Setup -->
[Player Performs Action] -- Sends to Server --> |Server Validates and Processes|
Server -- Broadcasts Updated State --> |All Clients| -- Update Local Game State -->


current player updates:

PlayerUI Script

    Current State: Handles UI setup, adjustment for local and remote players, and positioning based on the player's sequential ID.
    Required Changes: No changes needed. This script appears to be working as intended.

PlayerManager Script

    Current State: Manages player UI instantiation and maintains mappings between network IDs and player data.
    Required Changes:
        Implement a method to assign a sequential ID to a player.
        Modify InitializePlayerUI to use the sequential ID for UI setup.
        Update the logic to ensure that each player is assigned the correct player data based on their sequential ID.

Player Script

    Current State: Initializes player UI and applies positioning logic.
    Required Changes:
        Ensure that the player receives a sequential ID from the PlayerManager.
        Modify the script to use the sequential ID for UI positioning and setup.
        Implement logic to handle game actions and synchronize game state across the network.

Gradual Implementation Plan

    PlayerManager Modifications:
        Implement a method to assign sequential IDs to players.
        Adjust the player data assignment logic to use sequential IDs.
        Ensure that InitializePlayerUI is called with the correct sequential ID.

    Player Script Modifications:
        Modify the Start method to receive a sequential ID from PlayerManager.
        Use the sequential ID for UI initialization and positioning logic.

    Testing and Debugging:
        Implement debug logs to ensure that sequential IDs are assigned correctly and that player data is mapped accurately.
        Test the player spawning and UI setup process in the game environment.

    Implementing Game Actions and Synchronization (Steps 5 and 6 in the Data Flow Diagram):
        Implement logic in the Player script to handle game actions (like selecting a card) and send these actions to the server.
        Develop server-side logic to process and validate these actions.
        Broadcast updated game state to all clients and implement logic in the Player script to update the local game state based on the server's broadcast.

First Step - PlayerManager Modifications

Let's focus on implementing the first step. We will modify the PlayerManager script to assign sequential IDs to players and ensure that the player data is mapped correctly. Here's a conceptual outline of the changes:

    Assign Sequential IDs: Implement a method to assign a unique sequential ID to each player as they join the game.
    Map Player Data: Modify the existing logic to map player data from playerDataList to players based on their sequential IDs.
    Update InitializePlayerUI: Adjust the InitializePlayerUI method to initialize the player UI using the assigned sequential ID and corresponding player data.


    chatGOT notes:
     focusing strictly on your requirements and ensuring the context of your project is fully considered.

    ensure that each management system (like PlayerManager) can uniquely identify and handle its objects (players in this case), irrespective of the network ID values.

    If the network IDs are assigned to other objects (like cards) before players, it won't conflict with the player management system, as each type of object (player, card, etc.) would be handled within its respective management system.

     the PlayerUI script to position and rotate remote player UIs based on their network IDs. The positioning is determined by the ascending order of these IDs relative to the local player's network ID and the total number of players.

while local player is always or top, these are the positionand rotaati0on of remote players:

     if 2 players
     remote1- top

     if 3 players-
     remote 1 - right
     remote 2 - top

     if 4 players
     remote1 - right
     remote2- top
     remote3 - left

     the remote1-3 is relative to the player.networkID, i.e ascending and cyclic, i.e. when after the highest networkID appears the first player.networkID



     Network: notes

     network variables:

    1. public NetworkVariable<FixedString32Bytes> CardName = new NetworkVariable<FixedString32Bytes>("CardName");
    2. // Set initial data for CardUI
            cardUIInstance.SetCardData(CardName.Value.ToString(), Suit.Value.ToString());
    3. using Unity.Collections;



    CardManager:
        AllCards: This list acts as a registry of all card instances created in the game. Once a card is created, it's added to this list and stays here for the duration of the game. It's like a master list of all cards regardless of their current state (whether they are in the deck, in a player's hand, or discarded).

    DeckCards: This list represents the subset of cards that are currently in the deck. Initially, it will be identical to AllCards, but as cards are distributed to players, they will be removed from DeckCards. This list diminishes over time as cards are dealt.

    No Duplicates: It's important to manage these lists carefully to avoid duplicate instances. When a card is moved from the deck to a player's hand, it should be removed from DeckCards but remains in AllCards.


    System.Collections.IEnumerator 

    when action is added:
    using System;


    for using image add:
    using UnityEngine.UI;

    
    sync:

    game objects with networkobject and transform components will be sync with the server when spawned.

    the playerprefab should be attached to the networkmanager playerprefab which should also be added to prefabs list.

    any other networkobject should be spawned with: public override void OnNetworkSpawn()



    position:
    When a GameObject is parented to another GameObject in Unity, it maintains its world position by default unless specified otherwise. You want to reset their local position to (0,0,0) relative to the parent (the player object) so that they appear at the player's location.

    in 2D to display object on the Z, i.e. above/below use Sprite Renderer/sorting layer. 


    Network, sync a player.attribte.value that from:

    on the player.cs:

    1. public NetworkVariable<int> Numi = new NetworkVariable<int>(3);
    2. public override void OnNetworkSpawn()
    {
        //call to make it a server side element
        if (IsServer)
        {
            Numi.Value = 3; // Initialize or re-assign to ensure it's set on all clients
        }

        // Subscribe to Numi value changes to update UI accordingly
        Numi.OnValueChanged += OnNumiChanged;
        OnNumiChanged(0, Numi.Value); // Manually trigger the update to set initia UI state
    }

    3. private void OnNumiChanged(int oldValue, int newValue)
    {
        // This method is called whenever Numi changes
        UpdateNumiUI(newValue);
    }

    private void UpdateNumiUI(int numi)
    {
        if (playerUI != null)
        {
            playerUI.UpdateNumiUI(numi);
        }
    }

    5. then in playerUI;

    [SerializeField] private TextMeshProUGUI numiText;

    public void UpdateNumiUI(int numi)
    {
        if (numiText != null)
        {
            numiText.text = "Numi: " + numi.ToString();
        }
    }


    Rest connect to DB:
    private void LoadCardsFromJson()
    {
        string apiUrl = "http://localhost:8081/wt/api/nextjs/v1/page_by_path/?html_path=authors/yoga/unity";
        StartCoroutine(LoadCardsDataFromJson(apiUrl));
    }

    private IEnumerator LoadCardsDataFromJson(string apiUrl)
    {
        UnityWebRequest www = UnityWebRequest.Get(apiUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to load card data from API. Error: {www.error}");
            yield break;
        }

        string json = www.downloadHandler.text;
        CardsDataWrapper dataWrapper = JsonUtility.FromJson<CardsDataWrapper>(json);

        List<CardData> cardDataList = dataWrapper?.component_props?.cards;

        if (cardDataList != null)
        {
            /*foreach (CardData cardData in cardDataList)
            {
                cardData.PopulateSiblings(cardDataList);
            }*/

            OnCardDataLoaded?.Invoke(cardDataList);
            cardDataLoaded = true;
            CheckStartManagers();
        }
        else
        {
            Debug.LogError("No card data found in the API response.");
        }
    }



    To make a simple networkobnject:

    1. under canvas - create empty
    2. add canvas to it (verify it's inherit)
    3. add image to canvas - a simple visible object.
    4. add networkobject componenet
    5. add it in assets/prefabs
    6. add to networkmanager.prefabs_list
    7. create a script and add it to managers game object, or any other gameobject. 

    this script, start it when server.start:

    using UnityEngine;
    using Unity.Netcode;

    public class ServerStartSpawner : MonoBehaviour
    {
        public GameObject prefabToSpawn;

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += SpawnPrefab;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= SpawnPrefab;
            }
        }

        private void SpawnPrefab()
        {
            if (NetworkManager.Singleton.IsServer && prefabToSpawn != null)
            {
                GameObject spawnedObject = Instantiate(prefabToSpawn);
                spawnedObject.GetComponent<NetworkObject>().Spawn();
                Debug.Log("Prefab spawned on server start.");
            }
        }
    }


    8. go play mode, start network, and test.
    