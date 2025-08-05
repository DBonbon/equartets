using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    // ========================================
    // COMPONENT REFERENCES
    // ========================================
    private PlayerState playerState;
    private PlayerNetwork playerNetwork;
    private PlayerUI playerUI;
    
    // ========================================
    // COORDINATION DATA
    // ========================================
    private ulong clientId;
    public List<PlayerController> PlayersToAsk { get; private set; } = new List<PlayerController>();

    // ========================================
    // PUBLIC DATA ACCESS (Read-Only Properties)
    // ========================================
    public PlayerData Data => playerState?.Data;
    public string PlayerName => playerState?.Data.playerName ?? "Unknown";
    public int Score => playerState?.Score ?? 0;
    public bool HasTurn => playerState?.HasTurn ?? false;
    public bool IsWinner => playerState?.IsWinner ?? false;
    public int Result => playerState?.Result ?? 0;
    public ulong ClientId => clientId;
    
    // Card access - minimal interface, no assumptions about card game logic
    public List<CardNetwork> GetHandCards() 
    { 
        return playerState?.HandCards ?? new List<CardNetwork>();
    }
    
    public List<CardNetwork> GetCardsPlayerCanAsk() 
    { 
        return playerState?.CardsPlayerCanAsk ?? new List<CardNetwork>();
    }
    
    public List<CardNetwork> GetQuartets() 
    { 
        return playerState?.Quartets ?? new List<CardNetwork>();
    }

    // ========================================
    // INITIALIZATION
    // ========================================
    public void Initialize(PlayerData data)
    {
        // 1. Create data layer
        playerState = new PlayerState(data);
        
        // 2. Get network component reference
        playerNetwork = GetComponent<PlayerNetwork>();
        
        // 3. Get UI component reference
        playerUI = GetComponent<PlayerUI>();
        
        // 4. Initialize network values (Controller decides what goes on network)
        InitializeNetworkValues();
        
        // 5. Clear coordination lists
        PlayersToAsk.Clear();
        
        Debug.Log($"PlayerController initialized for {data.playerName}");
    }
    
    private void InitializeNetworkValues()
    {
        if (playerNetwork != null && playerNetwork.IsServer)
        {
            Debug.Log($"[InitializeNetworkValues] Setting network values for {playerState.Data.playerName}");
            
            // Set NetworkVariables - this will trigger OnValueChanged on all clients
            playerNetwork.playerName.Value = playerState.Data.playerName;
            playerNetwork.PlayerDbId.Value = playerState.Data.playerDbId;
            playerNetwork.PlayerImagePath.Value = playerState.Data.playerImagePath;
            playerNetwork.Score.Value = playerState.Score;
            playerNetwork.HasTurn.Value = playerState.HasTurn;
            playerNetwork.IsWinner.Value = playerState.IsWinner;
            playerNetwork.Result.Value = playerState.Result;
            
            Debug.Log($"[InitializeNetworkValues] Network values set for {playerState.Data.playerName}");
        }
        else
        {
            Debug.Log($"[InitializeNetworkValues] Skipped - IsServer: {playerNetwork?.IsServer ?? false}");
        }
    }

    public void SetupUI(Transform canvas, bool isLocalPlayer, int connectionIndex, int totalPlayers)
    {
        Debug.Log($"[SetupUI] Called for {PlayerName} - isLocalPlayer: {isLocalPlayer}, connectionIndex: {connectionIndex}, totalPlayers: {totalPlayers}");

        if (playerUI != null)
        {
            playerUI.SetCanvasReference(canvas);

            Vector2 uiPosition;

            if (isLocalPlayer)
            {
                uiPosition = UIPositionManager.GetLocalPlayerPosition();
            }
            else
            {
                // Rebuild list of remote players (ordered consistently)
                var allControllers = PlayerManager.Instance.playerControllers;
                var remoteControllers = allControllers
                    .Where(p => p.ClientId != Unity.Netcode.NetworkManager.Singleton.LocalClientId)
                    .OrderBy(p => p.ClientId) // Optional: or use ConnectionIndex if you prefer
                    .ToList();

                int remoteIndex = remoteControllers.IndexOf(this);

                uiPosition = UIPositionManager.GetRemotePositionByIndex(remoteIndex, remoteControllers.Count);
            }

            UIVisibilitySettings visibility = UIPositionManager.GetUIVisibility(isLocalPlayer);

            playerUI.SetUIPosition(uiPosition);
            playerUI.SetUIVisibility(visibility);
            UpdateHandCardsUI();

            Debug.Log($"[SetupUI] Completed for {PlayerName} at position {uiPosition}");
        }
        else
        {
            Debug.LogError($"[SetupUI] PlayerUI component not found on {gameObject.name}");
        }
    }


    /// <summary>
    /// Helper method to get the local player's connection index
    /// </summary>
    private int GetLocalPlayerConnectionIndex()
    {
        var localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        var playerManager = PlayerManager.Instance;
        
        for (int i = 0; i < playerManager.playerControllers.Count; i++)
        {
            if (playerManager.playerControllers[i].ClientId == localClientId)
            {
                return i;
            }
        }
        
        Debug.LogError($"[GetLocalPlayerConnectionIndex] Could not find local player connection index for client {localClientId}");
        return 0;
    }

    // ========================================
    // SCORE MANAGEMENT
    // ========================================
    public void IncrementScore()
    {
        // 1. Update data layer
        playerState.SetScore(playerState.Score + 1);
        
        // 2. Sync to network layer (triggers OnScoreChanged automatically)
        SyncScoreToNetwork();
        
        Debug.Log($"PlayerController: Score incremented to {playerState.Score}");
    }
    
    public void SetScore(int newScore)
    {
        // 1. Update data layer
        playerState.SetScore(newScore);
        
        // 2. Sync to network layer (triggers OnScoreChanged automatically)
        SyncScoreToNetwork();
    }
    
    private void SyncScoreToNetwork()
    {
        if (playerNetwork != null && playerNetwork.IsServer)
        {
            playerNetwork.Score.Value = playerState.Score;
            // UI update now handled by OnScoreChanged in PlayerNetwork
        }
    }

    // ========================================
    // TURN MANAGEMENT (Called by TurnManager)
    // ========================================
    public void SetTurn(bool hasTurn)
    {
        // 1. Update data layer
        playerState.SetTurn(hasTurn);
        
        // 2. Sync to network layer (triggers OnHasTurnChanged automatically)
        if (playerNetwork != null && playerNetwork.IsServer)
        {
            playerNetwork.HasTurn.Value = playerState.HasTurn;
            
            // 3. Update UI via ClientRpc
            // Turn indicator for ALL players
            playerNetwork.UpdateTurnIndicatorForAll_ClientRpc(hasTurn);
            
            // Turn controls only for this player
            if (hasTurn)
            {
                // Get all players from PlayerManager and update lists properly
                var allPlayers = PlayerManager.Instance.playerControllers;
                
                // IMPORTANT: Update cards list first (based on current hand)
                UpdateCardsPlayerCanAsk();
                UpdatePlayerToAskList(allPlayers);
                
                // Get current dropdown data
                ulong[] playerIDs = PlayersToAsk.Select(pc => pc.ClientId).ToArray();
                string[] playerNames = PlayersToAsk.Select(pc => pc.PlayerName).ToArray();
                int[] cardIDs = playerState.CardsPlayerCanAsk.Select(card => card.cardId.Value).ToArray();
                
                Debug.Log($"[SetTurn] Sending dropdown data - Players: {playerNames.Length}, Cards: {cardIDs.Length}");
                
                // Concatenate string array
                string playerNamesConcatenated = string.Join(",", playerNames);
                
                // Send controls update with data
                playerNetwork.UpdateTurnControlsForOwner_ClientRpc(true, playerIDs, playerNamesConcatenated, cardIDs);
            }
            else
            {
                // ADDED: Clear cards player can ask when turn ends (cleanup)
                UpdateCardsPlayerCanAsk();
                
                // Just deactivate controls
                playerNetwork.UpdateTurnControlsForOwner_ClientRpc(false, new ulong[0], "", new int[0]);
            }
        }
    }
    
    // ========================================
    // HAND CARDS MANAGEMENT
    // ========================================
    public void AddCardToHand(CardNetwork card)
    {
        Debug.Log($"[AddCardToHand] Called for {PlayerName} with card: {card?.cardName.Value ?? "NULL"}");

        if (card == null)
        {
            Debug.LogError($"[AddCardToHand] Received null card for {PlayerName}");
            return;
        }

        Debug.Log($"[AddCardToHand] PlayerState null check: {playerState == null}");
        Debug.Log($"[AddCardToHand] Hand count before: {playerState?.HandCards.Count ?? -1}");

        // 1. Update data layer
        playerState.AddCardToHand(card);

        Debug.Log($"[AddCardToHand] Hand count after PlayerState update: {playerState.HandCards.Count}");

        // 2. Sync to network layer
        SyncHandCardsToNetwork();

        Debug.Log($"[AddCardToHand] After network sync");

        // 3. Update UI layer (Hand cards need manual UI update - not NetworkVariable)
        UpdateHandCardsUI();

        Debug.Log($"[AddCardToHand] After UI update");

        // 4. Handle game logic consequences
        UpdateCardsPlayerCanAsk();

        Debug.Log($"[AddCardToHand] Completed for {PlayerName}. Final hand count: {playerState.HandCards.Count}");
    }

    public void RemoveCardFromHand(CardNetwork card)
    {
        if (card == null) return;
        
        Debug.Log($"[RemoveCardFromHand] Removing {card.cardName.Value} from {PlayerName}");
        
        // 1. Update data layer
        playerState.RemoveCardFromHand(card);
        
        // 2. Sync to network layer
        SyncHandCardsToNetwork();
        
        // 3. Update UI layer (Hand cards need manual UI update)
        UpdateHandCardsUI();
        
        // 4. CRITICAL FIX: Update game logic after card removal
        UpdateCardsPlayerCanAsk();
        
        Debug.Log($"PlayerController: Removed card {card.cardName.Value} from hand. Remaining cards: {playerState.HandCards.Count}");
    }
    
    private void SyncHandCardsToNetwork()
    {
        if (playerNetwork != null)
        {
            playerNetwork.HandCards.Clear();
            playerNetwork.HandCards.AddRange(playerState.HandCards);
        }
    }
    
    private void UpdateHandCardsUI()
    {
        if (playerNetwork != null)
        {
            int[] cardIDs = playerState.HandCards.Select(card => card.cardId.Value).ToArray();
            Debug.Log($"[UpdateHandCardsUI] Calling ClientRpc for {PlayerName} with {cardIDs.Length} cards");
            
            // Call ClientRpc to update UI on the card owner's client
            if (playerNetwork.IsServer)
            {
                playerNetwork.UpdateHandCardsUI_ClientRpc(cardIDs);
            }
        }
        else
        {
            Debug.LogError($"[UpdateHandCardsUI] PlayerNetwork is null for {PlayerName}");
        }
    }

    public void RefreshHandUI()
    {
        // Force UI update
        UpdateHandCardsUI();
        Debug.Log($"[RefreshHandUI] Forced UI refresh for {PlayerName}");
    }


    // ========================================
    // BASIC GAME LOGIC (Keep minimal - specific game rules handled elsewhere)
    // ========================================
    public void UpdateCardsPlayerCanAsk()
    {
        // Clear existing list
        playerState.ClearCardsPlayerCanAsk();

        // FIXED: Add actual logic - player can ask for cards they don't have
        // In a quartets game, you can typically ask for cards from the same suits you already have

        if (playerState.HandCards.Count > 0)
        {
            // Get unique suits from hand cards
            var suitsInHand = playerState.HandCards
                .Select(card => card.suit.Value.ToString())
                .Distinct()
                .ToList();

            Debug.Log($"[UpdateCardsPlayerCanAsk] Player {PlayerName} has suits: {string.Join(", ", suitsInHand)}");

            // For each suit in hand, player can ask for other cards in that suit
            foreach (var suit in suitsInHand)
            {
                // Get all cards of this suit that the player doesn't have
                var availableCardsInSuit = CardManager.Instance.allSpawnedCards
                    .Where(card => card.suit.Value.ToString() == suit)
                    .Where(card => !playerState.HandCards.Contains(card))
                    .ToList();

                // Add these cards to the "can ask" list
                foreach (var card in availableCardsInSuit)
                {
                    playerState.AddCardPlayerCanAsk(card);
                }
            }

            Debug.Log($"[UpdateCardsPlayerCanAsk] Player {PlayerName} can ask for {playerState.CardsPlayerCanAsk.Count} cards");
        }
        else
        {
            Debug.Log($"[UpdateCardsPlayerCanAsk] Player {PlayerName} has no cards, cannot ask for any");
        }

        // Sync to network layer
        if (playerNetwork != null)
        {
            playerNetwork.CardsPlayerCanAsk.Clear();
            playerNetwork.CardsPlayerCanAsk.AddRange(playerState.CardsPlayerCanAsk);
        }
    }

    public void UpdatePlayerToAskList(List<PlayerController> allControllers)
    {
        Debug.Log($"[UpdatePlayerToAskList] Updating for {PlayerName} with {allControllers.Count} total players");
        
        // 1. Update coordination data - exclude self
        PlayersToAsk.Clear();
        foreach (var controller in allControllers)
        {
            if (controller != this)
            {
                PlayersToAsk.Add(controller);
                Debug.Log($"[UpdatePlayerToAskList] Added {controller.PlayerName} to ask list");
            }
        }
        
        Debug.Log($"[UpdatePlayerToAskList] Player {PlayerName} can ask {PlayersToAsk.Count} players: {string.Join(", ", PlayersToAsk.Select(p => p.PlayerName))}");
        
        // 2. Sync to network layer (for compatibility)
        if (playerNetwork != null)
        {
            playerNetwork.PlayerToAsk.Clear();
            foreach (var controller in PlayersToAsk)
            {
                var otherPlayerNetwork = controller.GetComponent<PlayerNetwork>();
                if (otherPlayerNetwork != null)
                {
                    playerNetwork.PlayerToAsk.Add(otherPlayerNetwork);
                }
            }
        }
    }
    
    private void UpdatePlayerToAskUI()
    {
        if (playerUI != null && playerNetwork != null && playerNetwork.IsOwner && playerState.HasTurn)
        {
            ulong[] playerIDs = PlayersToAsk.Select(pc => pc.ClientId).ToArray();
            string[] playerNames = PlayersToAsk.Select(pc => pc.PlayerName).ToArray();
            playerUI.UpdatePlayersDropdown(playerIDs, playerNames);
        }
    }

    // ========================================
    // UTILITY METHODS
    // ========================================
    public bool IsHandEmpty()
    {
        return playerState.HandCards.Count == 0;
    }
    
    public int GetHandCardsCount()
    {
        return playerState.HandCards.Count;
    }
    
    public string GetPlayerName()
    {
        return playerState.Data.playerName;
    }
    
    public ulong GetClientId()
    {
        return clientId;
    }
    
    public int GetScore()
    {
        return playerState.Score;
    }

    // ========================================
    // EXTERNAL INTERFACE (for PlayerManager)
    // ========================================
    public void SetClientId(ulong id)
    {
        clientId = id;
    }

    // ========================================
    // REMOVED METHODS (Now handled by NetworkVariable OnValueChanged)
    // ========================================
    
    // REMOVED: UpdateScoreUI() - handled by OnScoreChanged
    // REMOVED: RefreshAllUI() - NetworkVariable handles name/score/image
    // REMOVED: BroadcastPlayerInfo() - NetworkVariable handles sync automatically
}