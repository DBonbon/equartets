using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerUI : MonoBehaviour
{
    // ========================================
    // UI ELEMENT REFERENCES
    // ========================================

    #region Personal Info UI
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image playerImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    #endregion

    #region Turn UI Elements
    [SerializeField] private TMP_Dropdown cardsDropdown;
    [SerializeField] private TMP_Dropdown playersDropdown;
    [SerializeField] private GameObject hasTurnIndicator;
    [SerializeField] private Button guessButton;
    #endregion

    #region Hand UI Elements
    [SerializeField] private Transform cardDisplayTransform; // PlayerHand - where cards are placed
    #endregion


    #region UI Container Reference
    [SerializeField] private Transform playerUIContainer; // The empty object that contains all UI elements
    #endregion

    // ========================================
    // INTERNAL STATE (for dropdown management)
    // ========================================
    private List<int> cardIDs = new List<int>();
    private List<ulong> playerIdsInDropdown = new List<ulong>();

    // ========================================
    // COMPONENT REFERENCES
    // ========================================
    private PlayerNetwork playerNetwork;
    private const string DefaultImagePath = "Images/character_01";
    public Vector3 CardDisplayPosition
    {
        get
        {
            if (cardDisplayTransform == null)
            {
                Debug.LogError($"[PlayerUI] CardDisplayPosition: cardDisplayTransform is null");
                return Vector3.zero;
            }
            return cardDisplayTransform.position;
        }
    }

    // ========================================
    // INITIALIZATION
    // ========================================
    void Awake()
    {
        // Setup button event
        if (guessButton != null)
        {
            guessButton.onClick.AddListener(OnEventGuessClick);
        }

        // Get network component reference
        playerNetwork = GetComponent<PlayerNetwork>();

        // Initialize UI state
        InitializeTurnUI(false);
    }

    public void SetCanvasReference(Transform canvas)
    {
        Debug.Log($"[CONTAINER DEBUG] playerUIContainer name: {playerUIContainer?.name ?? "NULL"}");
        Debug.Log($"[CONTAINER DEBUG] playerUIContainer type: {playerUIContainer?.GetType() ?? null}");
        Debug.Log($"[CONTAINER DEBUG] Is RectTransform: {playerUIContainer is RectTransform}");

        if (canvas != null && playerUIContainer != null)
        {
            Debug.Log($"[MOVE DEBUG] About to move {playerUIContainer.name} from {playerUIContainer.parent.name} to {canvas.name}");

            playerUIContainer.SetParent(canvas, false);

            // IMMEDIATELY check if it worked
            Debug.Log($"[MOVE DEBUG] After SetParent - actual parent is: {playerUIContainer.parent.name}");
            Debug.Log($"[MOVE DEBUG] Move successful: {playerUIContainer.parent == canvas}");

            // Disable prefab canvas
            Canvas prefabCanvas = GetComponent<Canvas>();
            if (prefabCanvas != null)
            {
                prefabCanvas.enabled = false;
            }
        }
    }

    /// <summary>
    /// Initialize player basic info (called by PlayerController)
    /// </summary>
    public void InitializePlayerUI(string playerName, string imagePath)
    {
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        // Set player image
        SetPlayerImage(imagePath);

        // Initialize turn UI as inactive
        InitializeTurnUI(false);

        Debug.Log($"PlayerUI initialized for {playerName}");
    }

    private void SetPlayerImage(string imagePath)
    {
        string pathToUse = string.IsNullOrEmpty(imagePath) ? DefaultImagePath : imagePath;

        var imageSprite = Resources.Load<Sprite>(pathToUse);
        if (playerImage != null && imageSprite != null)
        {
            playerImage.sprite = imageSprite;
        }
        else
        {
            Debug.LogWarning($"Could not load player image from path: {pathToUse}");
        }
    }

    private void InitializeTurnUI(bool isActive)
    {
        if (playersDropdown != null) playersDropdown.gameObject.SetActive(isActive);
        if (cardsDropdown != null) cardsDropdown.gameObject.SetActive(isActive);
        if (guessButton != null) guessButton.gameObject.SetActive(isActive);
    }

    // ========================================
    // SCORE DISPLAY
    // ========================================

    /// <summary>
    /// Update score display (called by PlayerController)
    /// </summary>
    public void UpdateScoreUI(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }

    // ========================================
    // TURN MANAGEMENT UI
    // ========================================

    /// <summary>
    /// Update turn UI state (called by PlayerController)
    /// </summary>
    public void UpdateTurnUI(bool hasTurn)
    {
        // Show/hide turn indicator
        if (hasTurnIndicator != null)
        {
            hasTurnIndicator.SetActive(hasTurn);
        }

        // Activate/deactivate turn controls
        ActivateTurnUI(hasTurn);

        Debug.Log($"PlayerUI: Turn UI updated - hasTurn: {hasTurn}");
    }

    private void ActivateTurnUI(bool hasTurn)
    {
        if (playersDropdown != null) playersDropdown.gameObject.SetActive(hasTurn);
        if (cardsDropdown != null) cardsDropdown.gameObject.SetActive(hasTurn);
        if (guessButton != null) guessButton.gameObject.SetActive(hasTurn);
    }

    /// <summary>
    /// Update turn indicator (visible to ALL players)
    /// </summary>
    public void UpdateTurnIndicator(bool hasTurn)
    {
        // Show/hide turn indicator for everyone
        if (hasTurnIndicator != null)
        {
            hasTurnIndicator.SetActive(hasTurn);
        }

        Debug.Log($"PlayerUI: Turn indicator updated - hasTurn: {hasTurn}");
    }

    /// <summary>
    /// Update turn controls (visible only to the player with the turn)
    /// </summary>
    public void UpdateTurnControls(bool hasTurn)
    {
        // Activate/deactivate turn controls (dropdowns, guess button)
        if (playersDropdown != null) playersDropdown.gameObject.SetActive(hasTurn);
        if (cardsDropdown != null) cardsDropdown.gameObject.SetActive(hasTurn);
        if (guessButton != null) guessButton.gameObject.SetActive(hasTurn);

        Debug.Log($"PlayerUI: Turn controls updated - hasTurn: {hasTurn}");
    }

    // ========================================
    // HAND CARDS DISPLAY
    // ========================================

    /// <summary>
    /// Update hand cards display with instant positioning
    /// </summary>
    public void UpdatePlayerHandUIWithIDs(List<int> cardIDs)
    {
        if (cardDisplayTransform == null)
        {
            Debug.LogError("cardDisplayTransform is NULL");
            return;
        }

        // Hide current cards
        foreach (Transform child in cardDisplayTransform)
        {
            child.gameObject.SetActive(false);
        }

        // Display new cards instantly
        UpdatePlayerHandUIInstant(cardIDs);
    }

    /// <summary>
    /// Instant positioning method for card display
    /// </summary>
    private void UpdatePlayerHandUIInstant(List<int> cardIDs)
    {
        for (int i = 0; i < cardIDs.Count; i++)
        {
            int cardID = cardIDs[i];
            CardUI cardUI = CardManager.Instance?.FetchCardUIById(cardID);

            if (cardUI != null)
            {
                cardUI.gameObject.SetActive(false);
                cardUI.transform.SetParent(cardDisplayTransform, false);
                cardUI.SetFaceUp(false);

                // Position immediately
                Vector3 targetPosition = CalculateCardPositionInHand(i, cardIDs.Count);
                cardUI.transform.position = targetPosition;

                cardUI.gameObject.SetActive(true);
                cardUI.SetFaceUp(true);
            }
        }
    }

    /// <summary>
    /// Calculate where the card should end up in the hand layout
    /// </summary>
    public Vector3 CalculateCardPositionInHand(int cardIndex, int totalCards)
    {
        // Simple horizontal layout
        float cardSpacing = Mathf.Max(50f, Mathf.Min(100f, 500f / totalCards));
        float totalWidth = (totalCards - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        Vector3 basePosition = cardDisplayTransform.position;
        Vector3 targetPosition = new Vector3(
            basePosition.x + startX + (cardIndex * cardSpacing),
            basePosition.y,
            basePosition.z
        );

        // COMPREHENSIVE DEBUG LOGGING
        Debug.Log($"[CARD POSITION DEBUG] ========== {gameObject.name} ==========");
        Debug.Log($"[CARD POSITION] cardDisplayTransform.name: {cardDisplayTransform.name}");
        Debug.Log($"[CARD POSITION] cardDisplayTransform.position: {cardDisplayTransform.position}");
        Debug.Log($"[CARD POSITION] cardDisplayTransform.localPosition: {cardDisplayTransform.localPosition}");
        Debug.Log($"[CARD POSITION] cardDisplayTransform.parent: {cardDisplayTransform.parent?.name ?? "NULL"}");
        Debug.Log($"[CARD POSITION] Calculated target position: {targetPosition}");
        Debug.Log($"[CARD POSITION] Card index: {cardIndex}, Total cards: {totalCards}");
        Debug.Log($"[CARD POSITION] =======================================");

        return targetPosition;
    }

    // ========================================
    // DROPDOWN MANAGEMENT
    // ========================================

    /// <summary>
    /// Update players dropdown (called by PlayerController)
    /// </summary>
    public void UpdatePlayersDropdown(ulong[] playerIDs, string[] playerNames)
    {
        if (playersDropdown == null) return;

        // Validate input
        if (playerIDs.Length != playerNames.Length)
        {
            Debug.LogError($"PlayerUI: Mismatch between playerIDs ({playerIDs.Length}) and playerNames ({playerNames.Length})");
            return;
        }

        // Clear existing data
        playersDropdown.ClearOptions();
        playerIdsInDropdown.Clear();

        // Populate dropdown
        List<string> playerNamesList = new List<string>(playerNames);
        playerIdsInDropdown.AddRange(playerIDs);
        playersDropdown.AddOptions(playerNamesList);

        Debug.Log($"PlayerUI: Updated players dropdown with {playerNames.Length} players: {string.Join(", ", playerNames)}");
    }

    /// <summary>
    /// Update cards dropdown (called by PlayerController)
    /// </summary>
    public void UpdateCardsDropdownWithIDs(int[] cardIDs)
    {
        if (cardsDropdown == null) return;

        // Clear existing data
        cardsDropdown.ClearOptions();
        this.cardIDs.Clear();

        // Populate dropdown
        List<string> cardNames = new List<string>();
        foreach (int id in cardIDs)
        {
            string cardName = CardManager.Instance?.GetCardNameById(id) ?? $"Card {id}";
            cardNames.Add(cardName);
            this.cardIDs.Add(id);
        }

        cardsDropdown.AddOptions(cardNames);

        Debug.Log($"PlayerUI: Updated cards dropdown with {cardIDs.Length} cards");
    }

    // ========================================
    // EVENT HANDLING
    // ========================================

    /// <summary>
    /// Handle guess button click (UI event)
    /// </summary>
    public void OnEventGuessClick()
    {
        // Validate dropdown selections
        int selectedPlayerIndex = playersDropdown != null ? playersDropdown.value : -1;
        int selectedCardIndex = cardsDropdown != null ? cardsDropdown.value : -1;

        if (!ValidateSelection(selectedPlayerIndex, selectedCardIndex))
        {
            return;
        }

        // Get selected values
        ulong selectedPlayerId = playerIdsInDropdown[selectedPlayerIndex];
        int selectedCardId = cardIDs[selectedCardIndex];

        // Send through network layer
        if (playerNetwork != null)
        {
            playerNetwork.OnEventGuessClickServerRpc(selectedPlayerId, selectedCardId);
            Debug.Log($"PlayerUI: Guess submitted - Player ID: {selectedPlayerId}, Card ID: {selectedCardId}");
        }
        else
        {
            Debug.LogError("PlayerUI: PlayerNetwork component not found");
        }

        // Re-enable button
        if (guessButton != null)
        {
            guessButton.interactable = true;
        }
    }

    private bool ValidateSelection(int selectedPlayerIndex, int selectedCardIndex)
    {
        // Validate card selection
        if (selectedCardIndex < 0 || selectedCardIndex >= cardIDs.Count)
        {
            Debug.LogError($"PlayerUI: Invalid card selection - Index: {selectedCardIndex}, Available: {cardIDs.Count}");
            return false;
        }

        // Validate player selection
        if (selectedPlayerIndex < 0 || selectedPlayerIndex >= playerIdsInDropdown.Count)
        {
            Debug.LogError($"PlayerUI: Invalid player selection - Index: {selectedPlayerIndex}, Available: {playerIdsInDropdown.Count}");
            return false;
        }

        return true;
    }

    // ========================================
    // UTILITY METHODS
    // ========================================

    /// <summary>
    /// Clear all dropdown data
    /// </summary>
    public void ClearDropdowns()
    {
        if (playersDropdown != null) playersDropdown.ClearOptions();
        if (cardsDropdown != null) cardsDropdown.ClearOptions();

        playerIdsInDropdown.Clear();
        cardIDs.Clear();
    }

    /// <summary>
    /// Reset UI to default state
    /// </summary>
    public void ResetUI()
    {
        ClearDropdowns();
        InitializeTurnUI(false);

        if (scoreText != null) scoreText.text = "Score: 0";
        if (playerNameText != null) playerNameText.text = "Player";

        // Hide all hand cards
        if (cardDisplayTransform != null)
        {
            foreach (Transform child in cardDisplayTransform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Set UI position on screen using anchor coordinates
    /// </summary>
    /// <param name="anchorPosition">Position from 0,0 to 1,1 (bottom-left to top-right)</param>
    public void SetUIPosition(Vector2 anchorPosition)
    {
        Debug.Log($"[SetUIPosition] CALLED for {gameObject.name} with position {anchorPosition}");

        if (playerUIContainer != null)
        {
            RectTransform rectTransform = playerUIContainer.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = anchorPosition;
                rectTransform.anchorMax = anchorPosition;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                Debug.Log($"[SetUIPosition] SUCCESS: Set {gameObject.name} to position {anchorPosition}");
            }
            else
            {
                Debug.LogError($"[SetUIPosition] ERROR: RectTransform not found for {gameObject.name}");
            }
        }
        else
        {
            Debug.LogError($"[SetUIPosition] ERROR: playerUIContainer is null for {gameObject.name}");
        }
    }

    /// <summary>
    /// Set UI section visibility based on player type (local vs remote)
    /// </summary>
    /// <param name="visibility">Visibility settings for different UI sections</param>
    public void SetUIVisibility(UIVisibilitySettings visibility)
    {
        Debug.Log($"PlayerUI: Setting visibility - Personal: {visibility.ShowPersonalInfo}, Turn: {visibility.ShowTurnUI}, Hand: {visibility.ShowPlayerHand}");

        // Personal info is always shown (name, image, score) - no change needed
        // Turn UI already works correctly with HasTurn system - no change needed

        // Only change: Player hand visibility based on local/remote
        SetPlayerHandVisibility(visibility.ShowPlayerHand);
    }

    /// <summary>
    /// Control visibility of player hand section (cards display)
    /// </summary>
    private void SetPlayerHandVisibility(bool visible)
    {
        if (cardDisplayTransform != null)
        {
            cardDisplayTransform.gameObject.SetActive(visible);
            Debug.Log($"PlayerUI: Set player hand visibility to {visible} for {gameObject.name}");

            if (visible)
            {
                // Flip all currently active cards face-up
                foreach (Transform child in cardDisplayTransform)
                {
                    CardUI cardUI = child.GetComponent<CardUI>();
                    if (cardUI != null)
                    {
                        cardUI.SetFaceUp(true);
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        if (playerUIContainer != null)
        {
            string parentName = playerUIContainer.transform.parent?.name ?? "null";
            Debug.Log($"[LateUpdate] {playerUIContainer.name} parent: {parentName}");
        }
    }
}