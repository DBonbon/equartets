using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class PlayerNetwork : NetworkBehaviour
{
    // ========================================
    // NETWORK VARIABLES - Pure data synchronization
    // ========================================
    public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<int> PlayerDbId = new NetworkVariable<int>();
    public NetworkVariable<FixedString128Bytes> PlayerImagePath = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> Result = new NetworkVariable<int>(0);
    public NetworkVariable<bool> IsWinner = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> HasTurn = new NetworkVariable<bool>(false);

    public NetworkVariable<int> ConnectionIndex = new NetworkVariable<int>(-1);
    public NetworkVariable<int> TotalPlayers = new NetworkVariable<int>(0);

    // ========================================
    // COMPATIBILITY LISTS (For external systems during transition)
    // ========================================
    public List<CardNetwork> HandCards { get; set; } = new List<CardNetwork>();
    public List<PlayerNetwork> PlayerToAsk { get; set; } = new List<PlayerNetwork>();
    public List<CardNetwork> CardsPlayerCanAsk { get; set; } = new List<CardNetwork>();
    public List<CardNetwork> Quartets { get; set; } = new List<CardNetwork>();

    // ========================================
    // NETWORK LIFECYCLE
    // ========================================
    // Add this to PlayerNetwork.cs

    public override void OnNetworkSpawn()
    {
        // Initialize network variables on server
        if (IsServer)
        {
            Score.Value = 0;
            HasTurn.Value = false;
            IsWinner.Value = false;
            Result.Value = 0;
        }

        // Set up listeners on ALL clients (including server)
        playerName.OnValueChanged += OnPlayerNameChanged;
        PlayerImagePath.OnValueChanged += OnPlayerImagePathChanged;
        Score.OnValueChanged += OnScoreChanged;
        HasTurn.OnValueChanged += OnHasTurnChanged;
        IsWinner.OnValueChanged += OnIsWinnerChanged;
        Result.OnValueChanged += OnResultChanged;
        ConnectionIndex.OnValueChanged += OnPositioningDataChanged;
        TotalPlayers.OnValueChanged += OnPositioningDataChanged;

        // CRITICAL: Force initial UI update on ALL clients (not just server)
        StartCoroutine(ForceInitialUIUpdateForAllClients());

        Debug.Log($"PlayerNetwork spawned - IsServer: {IsServer}, IsOwner: {IsOwner}");
    }

    // Force initial UI update for both server and clients
    private System.Collections.IEnumerator ForceInitialUIUpdateForAllClients()
    {
        yield return null; // Wait 1 frame for initialization
        yield return new WaitForSeconds(0.1f); // Wait for network sync

        var playerUI = GetComponent<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.UpdateScoreUI(Score.Value);

            if (!string.IsNullOrEmpty(playerName.Value.ToString()))
            {
                playerUI.InitializePlayerUI(playerName.Value.ToString(), PlayerImagePath.Value.ToString());
            }

            if (IsOwner)
            {
                playerUI.UpdateTurnUI(HasTurn.Value);
            }
        }

        ApplyPositioning(); // <- Force it here after initialization
    }


    // ========================================
    // positioning playerui on client as well
    // ========================================
    private void OnPositioningDataChanged(int oldValue, int newValue)
    {
        if (ConnectionIndex.Value >= 0 && TotalPlayers.Value > 0)
        {
            StartCoroutine(DelayedApplyPositioning());
        }
    }

    private System.Collections.IEnumerator DelayedApplyPositioning()
    {
        // Wait a bit for all PlayerNetwork objects to be spawned and initialized
        yield return new WaitForSeconds(0.2f);

        ApplyPositioning();
    }



    private void ApplyPositioning()
    {
        if (!IsClient) return;

        var allPlayers = FindObjectsOfType<PlayerNetwork>();
        var playerUI = GetComponent<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogError($"[ApplyPositioning] PlayerUI not found for {playerName.Value}");
            return;
        }

        if (IsOwner)
        {
            // Local player goes to bottom
            playerUI.SetUIPosition(UIPositionManager.GetLocalPlayerPosition());
            playerUI.SetUIVisibility(UIPositionManager.GetUIVisibility(true));
            Debug.Log($"[ApplyPositioning] {playerName.Value} positioned at BOTTOM (local player)");
        }
        else
        {
            // Filter out the local player
            var remotePlayers = allPlayers
                .Where(p => !p.IsOwner)
                .OrderBy(p => p.ConnectionIndex.Value) // Optional: ensure stable order
                .ToList();

            int remoteIndex = remotePlayers.IndexOf(this);

            if (remoteIndex >= 0 && remoteIndex < 3)
            {
                Vector2 pos = UIPositionManager.GetRemotePositionByIndex(remoteIndex, remotePlayers.Count);
                playerUI.SetUIPosition(pos);
                playerUI.SetUIVisibility(UIPositionManager.GetUIVisibility(false));
                Debug.Log($"[ApplyPositioning] {playerName.Value} positioned at {pos} (remote index {remoteIndex})");
            }
            else
            {
                Debug.LogWarning($"[ApplyPositioning] Skipped positioning for {playerName.Value} - too many players?");
            }
        }
    }



    private int GetLocalPlayerConnectionIndex()
    {
        var localClientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"[GetLocalPlayerConnectionIndex] CALLED by {playerName.Value} - LocalClientId: {localClientId}");

        var allPlayerNetworks = FindObjectsOfType<PlayerNetwork>();
        foreach (var player in allPlayerNetworks)
        {
            Debug.Log($"[GetLocalPlayerConnectionIndex] Checking {player.playerName.Value}: OwnerClientId={player.OwnerClientId}, ConnectionIndex={player.ConnectionIndex.Value}");

            if (player.OwnerClientId == localClientId)
            {
                Debug.Log($"[GetLocalPlayerConnectionIndex] FOUND: {player.playerName.Value} with ConnectionIndex: {player.ConnectionIndex.Value}");
                return player.ConnectionIndex.Value;
            }
        }

        Debug.LogError($"[GetLocalPlayerConnectionIndex] NOT FOUND for ClientId {localClientId}!");
        return 0;
    }



    // ========================================
    // NETWORK RPCS - Called by PlayerController
    // ========================================

    [ClientRpc]
    public void UpdateHandCardsUI_ClientRpc(int[] cardIDs)
    {
        Debug.Log($"[UpdateHandCardsUI_ClientRpc] Received {cardIDs.Length} card IDs for {playerName.Value} (IsOwner: {IsOwner})");

        // Only update UI for the card owner (the player themselves)
        if (IsOwner)
        {
            var playerUI = GetComponent<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.UpdatePlayerHandUIWithIDs(cardIDs.ToList());
                Debug.Log($"[UpdateHandCardsUI_ClientRpc] Updated hand UI for {playerName.Value} with {cardIDs.Length} cards");
            }
            else
            {
                Debug.LogError("[UpdateHandCardsUI_ClientRpc] PlayerUI component not found");
            }
        }
        else
        {
            Debug.Log($"[UpdateHandCardsUI_ClientRpc] Skipped - not owner for {playerName.Value}");
        }
    }

    [ClientRpc]
    public void UpdateTurnIndicatorForAll_ClientRpc(bool hasTurn)
    {
        Debug.Log($"[UpdateTurnIndicatorForAll_ClientRpc] Player {playerName.Value} hasTurn: {hasTurn}");

        var playerUI = GetComponent<PlayerUI>();
        if (playerUI != null)
        {
            // Update turn indicator for ALL players (everyone sees who has the turn)
            playerUI.UpdateTurnIndicator(hasTurn);
            Debug.Log($"[UpdateTurnIndicatorForAll_ClientRpc] Updated turn indicator for {playerName.Value}: {hasTurn}");
        }
    }

    [ClientRpc]
    public void UpdateTurnControlsForOwner_ClientRpc(bool hasTurn, ulong[] playerIDs, string playerNamesConcatenated, int[] cardIDs)
    {
        Debug.Log($"[UpdateTurnControlsForOwner_ClientRpc] Player {playerName.Value} hasTurn: {hasTurn} (IsOwner: {IsOwner})");

        // Only update controls for the player themselves (the one with/without the turn)
        if (IsOwner)
        {
            var playerUI = GetComponent<PlayerUI>();
            if (playerUI != null)
            {
                // Update turn controls (dropdowns, guess button)
                playerUI.UpdateTurnControls(hasTurn);

                if (hasTurn)
                {
                    // Split the concatenated string back to array (same pattern as existing code)
                    string[] playerNames = string.IsNullOrEmpty(playerNamesConcatenated) ?
                        new string[0] : playerNamesConcatenated.Split(',');

                    // Update dropdowns with current data
                    playerUI.UpdatePlayersDropdown(playerIDs, playerNames);
                    playerUI.UpdateCardsDropdownWithIDs(cardIDs);
                    Debug.Log($"[UpdateTurnControlsForOwner_ClientRpc] Activated turn controls for {playerName.Value}");
                }
                else
                {
                    Debug.Log($"[UpdateTurnControlsForOwner_ClientRpc] Deactivated turn controls for {playerName.Value}");
                }
            }
        }
        else
        {
            Debug.Log($"[UpdateTurnControlsForOwner_ClientRpc] Skipped - not owner for {playerName.Value}");
        }
    }

    [ClientRpc]
    public void UpdatePlayerDbAttributes_ClientRpc(string playerName, string playerImagePath)
    {
        // Pure network call - just trigger the update on receiving clients
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Let PlayerController handle the UI update
            var playerUI = GetComponent<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.InitializePlayerUI(playerName, playerImagePath);
            }
        }
    }

    [ClientRpc]
    public void UpdatePlayerHandUI_ClientRpc(int[] cardIDs, ulong targetClient)
    {
        if (IsOwner)
        {
            var playerUI = GetComponent<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.UpdatePlayerHandUIWithIDs(cardIDs.ToList());
            }
        }
    }

    [ClientRpc]
    public void UpdateCardDropdown_ClientRpc(int[] cardIDs)
    {
        if (IsOwner)
        {
            var playerUI = GetComponent<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.UpdateCardsDropdownWithIDs(cardIDs);
            }
        }
    }

    [ClientRpc]
    public void TurnUIForPlayer_ClientRpc(ulong[] playerIDs, string playerNamesConcatenated)
    {
        if (IsOwner)
        {
            string[] playerNames = playerNamesConcatenated.Split(',');
            var playerUI = GetComponent<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.UpdatePlayersDropdown(playerIDs, playerNames);
            }
        }
    }


    // ========================================
    // GAME EVENT HANDLING
    // ========================================
    [ServerRpc(RequireOwnership = true)]
    public void OnEventGuessClickServerRpc(ulong selectedPlayerId, int cardId)
    {
        // Pass to game systems - this is where network meets game logic
        if (TurnManager.Instance != null)
        {
            NetworkVariable<int> networkCardId = new NetworkVariable<int>(cardId);
            TurnManager.Instance.OnEventGuessClick(selectedPlayerId, networkCardId);
        }
    }

    // ========================================
    // UTILITY METHODS
    // ========================================
    public void ShowHand(bool isLocalPlayer)
    {
        foreach (var card in HandCards)
        {
            var cardUI = card.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetFaceUp(isLocalPlayer);
            }
        }
    }

    public bool IsHandEmpty()
    {
        return HandCards.Count == 0;
    }

    // ========================================
    // ONVALUECHANGED HANDLERS
    // ========================================

    private void OnPlayerNameChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        Debug.Log($"[OnPlayerNameChanged] Old: '{oldValue}' -> New: '{newValue}' (IsServer: {IsServer}, IsOwner: {IsOwner})");

        var playerUI = GetComponent<PlayerUI>();
        if (playerUI != null)
        {
            string imagePath = PlayerImagePath.Value.ToString();
            playerUI.InitializePlayerUI(newValue.ToString(), imagePath);
            Debug.Log($"[OnPlayerNameChanged] Updated UI for: {newValue}");
        }
    }

    private void OnPlayerImagePathChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        Debug.Log($"[OnPlayerImagePathChanged] Old: '{oldValue}' -> New: '{newValue}'");

        var playerUI = GetComponent<PlayerUI>();
        if (playerUI != null)
        {
            // FIXED: Use this.playerName.Value instead of playerName.Value
            string currentPlayerName = this.playerName.Value.ToString();
            playerUI.InitializePlayerUI(currentPlayerName, newValue.ToString());
            Debug.Log($"[OnPlayerImagePathChanged] Updated UI image for: {currentPlayerName}");
        }
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {
        // FIXED: Use this.playerName.Value instead of playerName.Value
        Debug.Log($"[OnScoreChanged] Old: {oldValue} -> New: {newValue} for player: {this.playerName.Value}");

        var playerUI = GetComponent<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.UpdateScoreUI(newValue);
            Debug.Log($"[OnScoreChanged] Updated score UI to: {newValue}");
        }
    }

    private void OnHasTurnChanged(bool oldValue, bool newValue)
    {
        // FIXED: Use this.playerName.Value instead of playerName.Value
        Debug.Log($"[OnHasTurnChanged] Old: {oldValue} -> New: {newValue} for player: {this.playerName.Value}");

        // Only update UI for the owner (the player themselves)
        if (IsOwner)
        {
            var playerUI = GetComponent<PlayerUI>();
            if (playerUI != null)
            {
                playerUI.UpdateTurnUI(newValue);
                Debug.Log($"[OnHasTurnChanged] Updated turn UI to: {newValue}");
            }
        }
    }

    private void OnIsWinnerChanged(bool oldValue, bool newValue)
    {
        // FIXED: Use this.playerName.Value instead of playerName.Value
        Debug.Log($"[OnIsWinnerChanged] Old: {oldValue} -> New: {newValue} for player: {this.playerName.Value}");

        // You can add winner UI updates here if needed
        // For now, just log the change
        if (newValue)
        {
            Debug.Log($"[OnIsWinnerChanged] {this.playerName.Value} is now the winner!");
        }
    }

    private void OnResultChanged(int oldValue, int newValue)
    {
        // FIXED: Use this.playerName.Value instead of playerName.Value
        Debug.Log($"[OnResultChanged] Old: {oldValue} -> New: {newValue} for player: {this.playerName.Value}");

        // You can add result UI updates here if needed
        // For now, just log the change
    }

    public override void OnDestroy()
    {
        // Clean up ALL listeners
        if (playerName != null) playerName.OnValueChanged -= OnPlayerNameChanged;
        if (PlayerImagePath != null) PlayerImagePath.OnValueChanged -= OnPlayerImagePathChanged;
        if (Score != null) Score.OnValueChanged -= OnScoreChanged;
        if (HasTurn != null) HasTurn.OnValueChanged -= OnHasTurnChanged;
        if (IsWinner != null) IsWinner.OnValueChanged -= OnIsWinnerChanged;
        if (Result != null) Result.OnValueChanged -= OnResultChanged;
        if (ConnectionIndex != null) ConnectionIndex.OnValueChanged -= OnPositioningDataChanged;
        if (TotalPlayers != null) TotalPlayers.OnValueChanged -= OnPositioningDataChanged;

        base.OnDestroy();
    }

    // ========================================
    // HELPER METHODS (Called by PlayerController)
    // ========================================

    /// <summary>
    /// Synchronizes basic player info across network
    /// Called by PlayerController when initializing
    /// </summary>
    public void SyncPlayerInfo(string name, int dbId, string imagePath)
    {
        if (IsServer)
        {
            playerName.Value = name;
            PlayerDbId.Value = dbId;
            PlayerImagePath.Value = imagePath;

            // Broadcast to all clients
            UpdatePlayerDbAttributes_ClientRpc(name, imagePath);
        }
    }

    /// <summary>
    /// Synchronizes score across network
    /// Called by PlayerController when score changes
    /// </summary>
    public void SyncScore(int newScore)
    {
        if (IsServer)
        {
            Score.Value = newScore;
        }
    }

    /// <summary>
    /// Synchronizes turn state across network
    /// Called by PlayerController when turn changes
    /// </summary>
    public void SyncTurn(bool hasTurn)
    {
        if (IsServer)
        {
            HasTurn.Value = hasTurn;
        }
    }

    /// <summary>
    /// Synchronizes winner state across network
    /// Called by PlayerController when game ends
    /// </summary>
    public void SyncWinner(bool isWinner)
    {
        if (IsServer)
        {
            IsWinner.Value = isWinner;
        }
    }

    /// <summary>
    /// Synchronizes result across network
    /// Called by PlayerController when result is determined
    /// </summary>
    public void SyncResult(int result)
    {
        if (IsServer)
        {
            Result.Value = result;
        }
    }

    /// <summary>
    /// Synchronizes hand cards UI across network
    /// Called by PlayerController when hand changes
    /// </summary>
    public void SyncHandCardsUI(int[] cardIDs)
    {
        if (IsServer)
        {
            UpdatePlayerHandUI_ClientRpc(cardIDs, OwnerClientId);
        }
    }

    /// <summary>
    /// Synchronizes cards dropdown across network
    /// Called by PlayerController when available cards change
    /// </summary>
    public void SyncCardsDropdown(int[] cardIDs)
    {
        if (IsServer)
        {
            UpdateCardDropdown_ClientRpc(cardIDs);
        }
    }

    /// <summary>
    /// Synchronizes players dropdown across network
    /// Called by PlayerController when players to ask changes
    /// </summary>
    public void SyncPlayersDropdown(ulong[] playerIDs, string[] playerNames)
    {
        if (IsServer)
        {
            string playerNamesConcatenated = string.Join(",", playerNames);
            TurnUIForPlayer_ClientRpc(playerIDs, playerNamesConcatenated);
        }
    }

    // ========================================
    // ANIMATIONS (Called by PlayerController)
    // ========================================
    

    private Vector3 CalculatePlayerHandPositionForCurrentClient(PlayerController targetPlayer)
    {
        Debug.Log($"[CLIENT ANIMATION] Calculating hand position for {targetPlayer.PlayerName} on current client");
        
        // Get the target player's UI component
        PlayerUI targetPlayerUI = targetPlayer.GetComponent<PlayerUI>();
        if (targetPlayerUI == null) 
        {
            Debug.LogError($"[CLIENT ANIMATION] No PlayerUI found for {targetPlayer.PlayerName}");
            return Vector3.zero;
        }
        
        // Get where the cards should appear in their hand on THIS client
        int currentHandCount = targetPlayer.GetHandCardsCount();
        Vector3 handPosition = targetPlayerUI.CalculateCardPositionInHand(currentHandCount, currentHandCount + 1);
        
        Debug.Log($"[CLIENT ANIMATION] Hand position for {targetPlayer.PlayerName}: {handPosition} (current hand count: {currentHandCount})");
        return handPosition;
    }

    private bool IsCardVisibleToCurrentClient(PlayerController targetPlayer)
    {
        // Check if the target player is the local player on this client
        ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        bool isLocalPlayer = targetPlayer.ClientId == localClientId;
        
        Debug.Log($"[CLIENT ANIMATION] Card visibility check - Target: {targetPlayer.PlayerName}, Local Client: {localClientId}, Target Client: {targetPlayer.ClientId}, Is Local: {isLocalPlayer}");
        
        return isLocalPlayer; // Only show face up for local player
    }
}