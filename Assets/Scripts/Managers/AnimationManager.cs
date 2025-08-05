using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class AnimationManager : NetworkBehaviour
{
    public static AnimationManager Instance;

    public static event System.Action<int, ulong> OnAnimationComplete;

    private void Awake()
    {
        Debug.Log($"[ANIMATIONMANAGER] Awake called - IsServer: {NetworkManager.Singleton?.IsServer ?? false}, IsClient: {NetworkManager.Singleton?.IsClient ?? false}");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[ANIMATIONMANAGER] Created AnimationManager instance");
        }
        else
        {
            Debug.Log($"[ANIMATIONMANAGER] Destroying duplicate AnimationManager instance");
            Destroy(gameObject);
        }
    }

    // ========================================
    // CARD DRAW ANIMATION SYSTEM
    // ========================================

    /// <summary>
    /// Main entry point for card draw animations - called from CardManager
    /// </summary>
    public void PlayCardDrawAnimation(int cardId, ulong targetPlayerClientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[ANIMATIONMANAGER] PlayCardDrawAnimation should only be called on server");
            return;
        }

        Debug.Log($"[ANIMATIONMANAGER] Server triggering card draw animation - CardID: {cardId}, PlayerClientID: {targetPlayerClientId}");
        TriggerCardDrawAnimation_ClientRpc(cardId, targetPlayerClientId);
    }

    /// <summary>
    /// ClientRpc to trigger animation on all clients
    /// </summary>
    [ClientRpc]
    private void TriggerCardDrawAnimation_ClientRpc(int cardId, ulong targetPlayerClientId)
    {
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 1: Received cardId: {cardId}");

        // Debug: Check what cards actually exist on client
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 1A: allSpawnedCards count: {CardManager.Instance.allSpawnedCards.Count}");

        // Debug: List first few cards to see what's available
        for (int i = 0; i < Mathf.Min(3, CardManager.Instance.allSpawnedCards.Count); i++)
        {
            var card = CardManager.Instance.allSpawnedCards[i];
            if (card != null)
            {
                Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 1B: Available card {i}: ID={card.cardId.Value}, Name={card.cardName.Value}");
            }
        }

        // Find the card to animate
        CardNetwork cardNetwork = FindCardNetworkByIdOnClient(cardId);
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 1C: Lookup result for {cardId}: {(cardNetwork != null ? "FOUND" : "NOT FOUND")}");

        // Find the target player
        PlayerController targetPlayer = FindPlayerControllerByClientId(targetPlayerClientId);
        if (targetPlayer != null)
        {
            Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 2: SUCCESS - Found player: {targetPlayer.GetComponent<PlayerNetwork>().playerName.Value}");
        }
        else
        {
            Debug.LogError($"[ANIMATIONMANAGER-CLIENT] Step 2: FAILED - Player not found for ClientId: {targetPlayerClientId}");
        }

        // Execute animation if both card and player found
        if (targetPlayer != null && cardNetwork != null)
        {
            Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 3: SUCCESS - Ready to animate card {cardNetwork.cardName.Value} to {targetPlayer.GetComponent<PlayerNetwork>().playerName.Value}");
            ExecuteCardDrawAnimation(cardNetwork, targetPlayer);
        }
        else
        {
            Debug.LogError($"[ANIMATIONMANAGER-CLIENT] Step 3: FAILED - Player: {targetPlayer != null}, Card: {cardNetwork != null}");
        }
    }

    /// <summary>
    /// Executes the actual DOTween animation
    /// </summary>
    /// <summary>
    /// Executes the actual DOTween animation
    /// </summary>
    private void ExecuteCardDrawAnimation(CardNetwork cardNetwork, PlayerController targetPlayer)
    {
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6A: Getting real UI positions");

        // Get real deck position from DeckUI
        Vector3 deckPosition = GetRealDeckPosition();
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6B: Real deck position: {deckPosition}");

        // *** NEW SECTION: Get ACTUAL player hand position from client-side PlayerUI ***
        Vector3 actualHandPosition = GetActualPlayerHandPosition(targetPlayer);
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6C: ACTUAL hand position from client PlayerUI: {actualHandPosition}");

        // Keep the old calculation for comparison
        Vector3 calculatedHandPosition = GetRealPlayerHandPosition(targetPlayer);
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6C: CALCULATED hand position (old method): {calculatedHandPosition}");

        // Show the difference
        Vector3 difference = actualHandPosition - calculatedHandPosition;
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6C: *** POSITION DIFFERENCE: {difference} ***");

        // Use the actual position for animation target
        Vector3 handPosition = actualHandPosition;

        // Fallbacks only if positions are zero
        if (deckPosition == Vector3.zero)
        {
            Vector3 canvasCenter = GetCanvasCenter();
            deckPosition = canvasCenter + new Vector3(0, 0, 0);
            Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6B: Using fallback deck position: {deckPosition}");
        }

        if (handPosition == Vector3.zero)
        {
            // If actual position failed, fall back to calculated position
            handPosition = calculatedHandPosition;
            Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6C: ACTUAL position was zero, using calculated position: {handPosition}");

            // If calculated also failed, use final fallback
            if (handPosition == Vector3.zero)
            {
                Vector3 canvasCenter = GetCanvasCenter();
                handPosition = canvasCenter + new Vector3(0, -300, 0);
                Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6C: Using final fallback hand position: {handPosition}");
            }
        }

        // Get CardUI for animation
        CardUI animationCardUI = CardManager.Instance.FetchCardUIById(cardNetwork.cardId.Value);
        if (animationCardUI == null)
        {
            Debug.LogError($"[ANIMATIONMANAGER-CLIENT] Could not find CardUI for cardId: {cardNetwork.cardId.Value}");
            return;
        }

        // Setup animation
        animationCardUI.transform.position = deckPosition;
        animationCardUI.gameObject.SetActive(true);
        animationCardUI.SetFaceUp(true);

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6D: Animating from deck {deckPosition} to hand {handPosition}");

        // Execute DOTween animation
        animationCardUI.transform.DOMove(handPosition, 2.0f)
            .SetEase(Ease.OutQuart)
            .OnComplete(() =>
            {
                // Hide animation CardUI when complete
                animationCardUI.gameObject.SetActive(false);
                Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 6E: Animation complete");
                RefreshTargetPlayerHandUI(targetPlayer);
                OnAnimationComplete?.Invoke(cardNetwork.cardId.Value, targetPlayer.ClientId);
            });
    }

    /// <summary>
    /// Directly call PlayerController to refresh hand UI after animation
    /// </summary>
    private void RefreshTargetPlayerHandUI(PlayerController targetPlayer)
    {
        if (targetPlayer == null)
        {
            Debug.LogError($"[ANIMATIONMANAGER-CLIENT] RefreshTargetPlayerHandUI: targetPlayer is null");
            return;
        }

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] RefreshTargetPlayerHandUI: Calling RefreshHandUI for {targetPlayer.PlayerName}");

        // Direct call to PlayerController's refresh method
        targetPlayer.RefreshHandUI();

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] RefreshTargetPlayerHandUI: RefreshHandUI call completed for {targetPlayer.PlayerName}");
    }


    // ========================================
    // POSITION CALCULATION METHODS
    // ========================================

    private Vector3 GetRealDeckPosition()
    {
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetRealDeckPosition: Starting deck position search");

        // Try to find DeckUI component and get its position
        DeckUI deckUI = FindObjectOfType<DeckUI>();
        if (deckUI != null)
        {
            Vector3 position = deckUI.transform.position;
            Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetRealDeckPosition: Found DeckUI at position {position}");
            return position;
        }
        else
        {
            Debug.LogError($"[ANIMATIONMANAGER-CLIENT] GetRealDeckPosition: DeckUI not found");
            return Vector3.zero;
        }
    }

    private Vector3 GetRealPlayerHandPosition(PlayerController targetPlayer)
    {
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetRealPlayerHandPosition: Starting for player {targetPlayer.ClientId}");

        // Get the player's network component to determine their positioning
        PlayerNetwork playerNetwork = targetPlayer.GetComponent<PlayerNetwork>();
        if (playerNetwork == null)
        {
            Debug.LogError($"[ANIMATIONMANAGER-CLIENT] PlayerNetwork not found");
            return Vector3.zero;
        }

        // Get canvas info
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta; // (1920, 1080)

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Canvas size: {canvasSize}");
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Player IsOwner: {playerNetwork.IsOwner}");

        // Calculate actual PlayerUI position based on player positioning logic
        Vector3 actualPlayerUIPosition;

        if (playerNetwork.IsOwner)
        {
            // Local player - positioned at bottom center
            actualPlayerUIPosition = new Vector3(canvasSize.x / 2, 200, 0); // Adjust Y as needed
            Debug.Log($"[ANIMATIONMANAGER-CLIENT] Local player - positioned at bottom");
        }
        else
        {
            // Remote players - positioned around top/sides based on connection index
            int connectionIndex = playerNetwork.ConnectionIndex.Value;
            int totalPlayers = playerNetwork.TotalPlayers.Value;

            // Your positioning logic here - this is just an example
            if (totalPlayers == 2)
            {
                actualPlayerUIPosition = new Vector3(canvasSize.x / 2, canvasSize.y - 200, 0); // Top center
            }
            else
            {
                // More complex positioning for 3+ players
                actualPlayerUIPosition = new Vector3(canvasSize.x / 2, canvasSize.y - 200, 0); // Placeholder
            }

            Debug.Log($"[ANIMATIONMANAGER-CLIENT] Remote player {connectionIndex}/{totalPlayers} - positioned at top/side");
        }

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Calculated actual PlayerUI position: {actualPlayerUIPosition}");

        // Add CardDisplayTransform relative offset (based on prefab: +150 Y)
        Vector3 expectedHandPosition = actualPlayerUIPosition + new Vector3(0, 150, 0);

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Final calculated hand position: {expectedHandPosition}");

        return expectedHandPosition;
    }

    private Vector3 GetCanvasCenter()
    {
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetCanvasCenter: Looking for main Canvas");

        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas != null)
        {
            RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                Vector3 worldPosition = mainCanvas.transform.position;
                Vector2 anchoredPosition = canvasRect.anchoredPosition;
                Vector2 sizeDelta = canvasRect.sizeDelta;

                Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetCanvasCenter: Canvas transform.position = {worldPosition}");
                Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetCanvasCenter: Canvas anchoredPosition = {anchoredPosition}");
                Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetCanvasCenter: Canvas sizeDelta = {sizeDelta}");

                // Try using the sizeDelta center for UI positioning
                Vector3 canvasCenter = new Vector3(sizeDelta.x / 2, sizeDelta.y / 2, 0);
                Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetCanvasCenter: Calculated center = {canvasCenter}");

                return canvasCenter;
            }
        }

        Debug.LogError($"[ANIMATIONMANAGER-CLIENT] GetCanvasCenter: Canvas or RectTransform not found");
        return new Vector3(960, 540, 0); // Your known center as fallback
    }

    // ========================================
    // UTILITY METHODS
    // ========================================

    private PlayerController FindPlayerControllerByClientId(ulong clientId)
    {
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 2A: Looking for ClientId: {clientId}");

        // Find PlayerNetwork with matching OwnerClientId, then get its PlayerController
        PlayerNetwork[] allPlayerNetworks = FindObjectsOfType<PlayerNetwork>();
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 2B: Found {allPlayerNetworks.Length} PlayerNetworks in scene");

        foreach (var playerNetwork in allPlayerNetworks)
        {
            Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 2C: PlayerNetwork {playerNetwork.playerName.Value} has OwnerClientId: {playerNetwork.OwnerClientId}");
            if (playerNetwork.OwnerClientId == clientId)
            {
                PlayerController playerController = playerNetwork.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    Debug.Log($"[ANIMATIONMANAGER-CLIENT] Step 2D: MATCH! Found PlayerController for: {playerNetwork.playerName.Value}");
                    return playerController;
                }
            }
        }

        return null;
    }

    private CardNetwork FindCardNetworkByIdOnClient(int cardId)
    {
        CardNetwork[] allCardNetworks = FindObjectsOfType<CardNetwork>();
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] Found {allCardNetworks.Length} CardNetworks in scene");

        foreach (var cardNetwork in allCardNetworks)
        {
            if (cardNetwork.cardId.Value == cardId)
            {
                Debug.Log($"[ANIMATIONMANAGER-CLIENT] MATCH! Found card: {cardNetwork.cardName.Value}");
                return cardNetwork;
            }
        }
        return null;
    }

    /// <summary>
    /// Debug method to get actual PlayerUI cardDisplayTransform position on client
    /// </summary>
    private Vector3 GetActualPlayerHandPosition(PlayerController targetPlayer)
    {
        Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetActualPlayerHandPosition: Starting for player {targetPlayer.ClientId}");

        PlayerUI playerUI = targetPlayer.GetComponent<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogError($"[ANIMATIONMANAGER-CLIENT] GetActualPlayerHandPosition: PlayerUI component not found");
            return Vector3.zero;
        }

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetActualPlayerHandPosition: Found PlayerUI component: {playerUI.name}");

        // Use the public getter method or property
        Vector3 actualPosition = playerUI.CardDisplayPosition;

        Debug.Log($"[ANIMATIONMANAGER-CLIENT] GetActualPlayerHandPosition: *** ACTUAL POSITION: {actualPosition} ***");

        return actualPosition;
    }
}