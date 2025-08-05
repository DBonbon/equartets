using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardController : MonoBehaviour
{
    // DATA STORAGE
    private CardState cardState;
    
    // COMPONENT REFERENCES
    private CardNetwork cardNetworkComponent;  // Your existing CardNetwork.cs (NetworkBehaviour)
    private CardUI currentCardUI;  // External reference - can change

    // DATA ACCESS PROPERTIES
    public CardData Data => cardState.Data;
    public bool IsVisible => cardState.IsVisible;
    public bool IsFaceUp => cardState.IsFaceUp;
    public int CurrentOwnerId => cardState.CurrentOwnerId;
    public bool IsInHand => cardState.IsInHand;
    public bool IsInDeck => cardState.IsInDeck;
    public bool IsInQuartet => cardState.IsInQuartet;
    public Vector3 Position => cardState.Position;
    public Transform Parent => cardState.Parent;
    
    // NETWORK DATA ACCESS (for rules engine compatibility)
    public int NetworkCardId => cardNetworkComponent?.cardId.Value ?? Data.cardId;
    public string NetworkCardName => cardNetworkComponent?.cardName.Value.ToString() ?? Data.cardName;
    public string NetworkSuit => cardNetworkComponent?.suit.Value.ToString() ?? Data.suit;
    
    // INITIALIZATION
    public void Initialize(CardData data)
    {
        cardState = new CardState(data);
        cardNetworkComponent = GetComponent<CardNetwork>();
        // Note: CardUI will be assigned separately via SetCardUI()
        
        Debug.Log($"CardController initialized for card: {data.cardName}");
    }
    
    // CARDUI MANAGEMENT
    public void SetCardUI(CardUI cardUI)
    {
        currentCardUI = cardUI;
        if (currentCardUI != null)
        {
            // Update the CardUI with current state
            UpdateVisibility();
            SyncUIPosition();
        }
    }
    
    public void ClearCardUI()
    {
        currentCardUI = null;
    }
    
    // ENHANCED GAME LOGIC METHODS
    public void MoveToPlayer(int playerId)
    {
        // Update state
        cardState.SetOwner(playerId);
        cardState.SetInHand(true);
        
        // Sync with network component
        if (cardNetworkComponent != null)
        {
            // Keep CardNetwork in sync during migration
            // Future: Add network synchronization here
        }
        
        // Update UI
        UpdateVisibility();
        
        Debug.Log($"CardController: Moved card {Data.cardName} to player {playerId}");
    }
    
    public void MoveToQuartet()
    {
        // Update state
        cardState.SetInQuartet(true);
        cardState.SetFaceUp(true);
        cardState.SetVisibility(true);
        
        // Clear ownership when moving to quartet
        cardState.SetOwner(-1);
        
        // Update UI
        UpdateVisibility();
        
        Debug.Log($"CardController: Moved card {Data.cardName} to quartet area");
    }
    
    public void MoveToDeck()
    {
        // Update state
        cardState.SetInDeck(true);
        cardState.SetFaceUp(false);
        cardState.SetVisibility(false);
        
        // Clear ownership when moving to deck
        cardState.SetOwner(-1);
        
        // Update UI
        UpdateVisibility();
        
        Debug.Log($"CardController: Moved card {Data.cardName} to deck");
    }
    
    public void SetVisibilityForPlayer(int playerId, bool isLocalPlayer)
    {
        // Game logic: Cards in hand visible only to owner
        if (cardState.IsInHand)
        {
            bool shouldBeVisible = (cardState.CurrentOwnerId == playerId) && isLocalPlayer;
            cardState.SetFaceUp(shouldBeVisible);
            cardState.SetVisibility(true); // Card exists, but face up/down depends on ownership
        }
        // Cards in quartet always face up and visible
        else if (cardState.IsInQuartet)
        {
            cardState.SetFaceUp(true);
            cardState.SetVisibility(true);
        }
        // Cards in deck always face down and typically not visible
        else if (cardState.IsInDeck)
        {
            cardState.SetFaceUp(false);
            cardState.SetVisibility(false);
        }
        
        // Update UI
        UpdateVisibility();
    }
    
    public void UpdateVisibility()
    {
        if (currentCardUI != null)
        {
            currentCardUI.gameObject.SetActive(cardState.IsVisible);
            currentCardUI.SetFaceUp(cardState.IsFaceUp);
        }
    }
    
    public void SetParent(Transform newParent)
    {
        cardState.SetParent(newParent);
        SyncUIPosition();
    }
    
    public void SetPosition(Vector3 position)
    {
        cardState.SetPosition(position);
        SyncUIPosition();
    }
    
    private void SyncUIPosition()
    {
        if (currentCardUI != null)
        {
            if (cardState.Parent != null)
            {
                currentCardUI.transform.SetParent(cardState.Parent, false);
            }
            if (cardState.Position != Vector3.zero)
            {
                currentCardUI.transform.position = cardState.Position;
            }
        }
    }
    
    // ENHANCED UTILITY METHODS
    public bool CanBeAskedBy(int playerId)
    {
        // Game rule: Can ask for cards not in your hand and currently in someone's hand
        return cardState.IsInHand && cardState.CurrentOwnerId != playerId && cardState.CurrentOwnerId != -1;
    }
    
    public bool BelongsToPlayer(int playerId)
    {
        return cardState.IsInHand && cardState.CurrentOwnerId == playerId;
    }
    
    public bool IsInSameSuit(string suit)
    {
        return Data.suit == suit;
    }
    
    // NEW: CARD COMPARISON AND MATCHING METHODS
    public bool IsSameSuit(CardController otherCard)
    {
        return otherCard != null && Data.suit == otherCard.Data.suit;
    }
    
    public bool IsSameCard(CardController otherCard)
    {
        return otherCard != null && Data.cardId == otherCard.Data.cardId;
    }
    
    public bool IsInSameSuitAs(List<CardController> cards)
    {
        return cards.Any(card => card.Data.suit == Data.suit);
    }
    
    // NEW: QUARTET CHECKING METHODS
    public List<CardController> FindQuartetMatches(List<CardController> availableCards)
    {
        return availableCards.Where(card => card.Data.suit == Data.suit && card.Data.cardId != Data.cardId)
                            .ToList();
    }
    
    public bool IsPartOfQuartet(List<CardController> cards)
    {
        var suitMatches = cards.Where(card => card.Data.suit == Data.suit).ToList();
        return suitMatches.Count >= 4;
    }
    
    // NEW: OWNERSHIP AND STATE VALIDATION
    public bool IsOwnedBy(int playerId)
    {
        return cardState.CurrentOwnerId == playerId;
    }
    
    public bool IsAvailableForTransfer()
    {
        return cardState.IsInHand && cardState.CurrentOwnerId != -1;
    }
    
    public bool IsInPlayableState()
    {
        return cardState.IsInHand || cardState.IsInDeck;
    }
    
    // NEW: CARD TRANSFER METHODS
    public void TransferToPlayer(int newPlayerId)
    {
        if (IsAvailableForTransfer())
        {
            int oldOwner = cardState.CurrentOwnerId;
            MoveToPlayer(newPlayerId);
            Debug.Log($"CardController: Transferred card {Data.cardName} from player {oldOwner} to player {newPlayerId}");
        }
        else
        {
            Debug.LogWarning($"CardController: Cannot transfer card {Data.cardName} - not in transferable state");
        }
    }
    
    public void ResetToInitialState()
    {
        MoveToDeck();
        Debug.Log($"CardController: Reset card {Data.cardName} to initial state");
    }
    
    // NEW: UI COORDINATION HELPERS
    public void ForceUIUpdate()
    {
        UpdateVisibility();
        SyncUIPosition();
    }
    
    public void SetUIActive(bool active)
    {
        if (currentCardUI != null)
        {
            currentCardUI.gameObject.SetActive(active);
        }
    }
    
    // NEW: DEBUG AND UTILITY
    public string GetStateDescription()
    {
        var location = cardState.IsInHand ? "Hand" : 
                      cardState.IsInDeck ? "Deck" : 
                      cardState.IsInQuartet ? "Quartet" : "Unknown";
        
        return $"Card {Data.cardName}: {location}, Owner: {cardState.CurrentOwnerId}, Visible: {cardState.IsVisible}, FaceUp: {cardState.IsFaceUp}";
    }
    
    public override string ToString()
    {
        return $"CardController({Data.cardId}: {Data.cardName})";
    }
}