using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;
    public GameObject cardPrefab; // Network object prefab for cards
    public GameObject cardUIPrefab; // UI prefab for cards
    public Transform DecklTransform; // Parent object for Card UI instances

    private List<CardUI> cardUIPool = new List<CardUI>(); // Pool for Card UI
    public List<CardData> allCardsList = new List<CardData>(); // Loaded card data
    public List<CardNetwork> allSpawnedCards = new List<CardNetwork>(); // Inventory of spawned card instances

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DataManager.OnCardDataLoaded += LoadCardDataLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += () => StartCoroutine(StartCardSpawningProcess());
    }

    private void LoadCardDataLoaded(List<CardData> loadedCardDataList)
    {
       allCardsList = loadedCardDataList;
        ShuffleCards();
        InitializeCardUIPool(); // Ensure CardUI pool is initialized after loading data
    }

    private void InitializeCardUIPool()
    {
        foreach (var cardUIInstance in cardUIPool)
        {
            Destroy(cardUIInstance.gameObject); // Clear existing pool
        }
        cardUIPool.Clear();

        foreach (var cardData in allCardsList)
        {
            var cardUIObject = Instantiate(cardUIPrefab, DecklTransform);
            var cardUIComponent = cardUIObject.GetComponent<CardUI>();
            if (cardUIComponent)
            {
                cardUIComponent.UpdateCardUIWithCardData(cardData);
                cardUIPool.Add(cardUIComponent);
                cardUIObject.SetActive(false); // Start inactive
            }
        }
    }

    public CardUI FetchCardUIById(int cardId)
    {
        foreach (CardUI cardUI in cardUIPool)
        {
            // Match based on cardId instead of CardName
            if (cardUI.cardId == cardId && !cardUI.gameObject.activeInHierarchy)
            {
                return cardUI;
            }
        }
        return null;
    }

    // GAME LOGIC INTERFACE - Returns CardController for external scripts
    public CardController FetchCardControllerById(NetworkVariable<int> cardId)
    {
        foreach (CardNetwork cardNetwork in allSpawnedCards)
        {
            if (cardNetwork != null && cardNetwork.cardId.Value == cardId.Value)
            {
                return cardNetwork.GetComponent<CardController>();
            }
        }
        return null;
    }
    
    // OVERLOADED: Accept regular int for CardController interface
    public CardController FetchCardControllerById(int cardId)
    {
        foreach (CardNetwork cardNetwork in allSpawnedCards)
        {
            if (cardNetwork != null && cardNetwork.cardId.Value == cardId)
            {
                return cardNetwork.GetComponent<CardController>();
            }
        }
        return null;
    }

    // NETWORK INTERFACE - Returns CardNetwork for internal/network operations
    public CardNetwork FetchCardNetworkById(NetworkVariable<int> cardId)
    {
        foreach (CardNetwork cardNetwork in allSpawnedCards)
        {
            if (cardNetwork != null && cardNetwork.cardId.Value == cardId.Value)
            {
                return cardNetwork;
            }
        }
        return null;
    }
    
    // OVERLOADED: Accept regular int for CardNetwork interface
    public CardNetwork FetchCardNetworkById(int cardId)
    {
        foreach (CardNetwork cardNetwork in allSpawnedCards)
        {
            if (cardNetwork != null && cardNetwork.cardId.Value == cardId)
            {
                return cardNetwork;
            }
        }
        return null;
    }

    // LEGACY METHOD - For backward compatibility during migration
    public CardNetwork FetchCardById(NetworkVariable<int> cardId)
    {
        return FetchCardNetworkById(cardId);
    }

    System.Collections.IEnumerator StartCardSpawningProcess()
    {
        while (DeckManager.Instance.DeckInstance == null)
        {
            yield return null;
        }
        SpawnCards();
    }

    private void SpawnCards()
    {
        foreach (var cardData in allCardsList)
        {
            var spawnedCard = Instantiate(cardPrefab, transform); // Instantiate without parent to avoid hierarchy issues
            var networkObject = spawnedCard.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();

                var cardComponent = spawnedCard.GetComponent<CardNetwork>();
                if (cardComponent != null)
                {
                    // Initialize the card
                    cardComponent.InitializeCard(cardData.cardId, cardData.cardName, cardData.suit, cardData.hint, cardData.siblings);
                    allSpawnedCards.Add(cardComponent); // Add the component, not the GameObject

                    // Assuming DeckInstance holds the deck GameObject, access Deck component to call AddCardToDeck
                    if (DeckManager.Instance.DeckInstance != null)
                    {
                        var deckComponent = DeckManager.Instance.DeckInstance.GetComponent<Deck>();
                        if (deckComponent != null)
                        {
                            deckComponent.AddCardToDeck(spawnedCard); // Pass the GameObject directly
                        }
                        else
                        {
                            Debug.LogError("Deck component not found on DeckInstance.");
                        }
                    }
                    else
                    {
                        Debug.LogError("DeckInstance is null.");
                    }

                    //Debug.Log($"Card initialized and its Name is: {cardData.cardName}");
                }
                // In CardManager.SpawnCards() method, after card creation:
                var cardController = spawnedCard.GetComponent<CardController>();
                    if (cardController == null)
                    {
                        cardController = spawnedCard.AddComponent<CardController>();
                    }
                    cardController.Initialize(cardData);
            }
        }
    }

    private void ShuffleCards()
    {
        System.Random rng = new System.Random();
        int n =allCardsList.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var value =allCardsList[k];
           allCardsList[k] =allCardsList[n];
           allCardsList[n] = value;
        }
    }

    private void OnDestroy()
    {
        DataManager.OnCardDataLoaded -= LoadCardDataLoaded;
    }

    // ========================================
    // DISTRIBUTION METHODS - Updated for PlayerController
    // ========================================
    
    // UPDATED: Accept List<PlayerController> instead of List<PlayerNetwork>
    public void DistributeCards(List<PlayerController> playerControllers) 
    {       
        int cardsPerPlayer = 5;
        Debug.Log($"=== DISTRIBUTE CARDS START === Players: {playerControllers.Count}");

        Deck deck = DeckManager.Instance.DeckInstance.GetComponent<Deck>();
        if (deck == null) {
            Debug.LogError("=== DECK NOT FOUND ===");
            return;
        }
        
        Debug.Log($"=== DECK FOUND === Cards available: {deck.DeckCards?.Count ?? 0}");

        foreach (var playerController in playerControllers) {
            Debug.Log($"=== DISTRIBUTING TO === {playerController.PlayerName}");
            
            // Distribute cards directly to PlayerController
            for (int i = 0; i < cardsPerPlayer; i++) {
                Debug.Log($"=== DRAWING CARD {i + 1} for {playerController.PlayerName} ===");
                
                // Keep existing deck interface - returns CardController
                CardController card = deck.RemoveCardControllerFromDeck();
                if (card != null) {
                    Debug.Log($"=== CARD DRAWN === {card.Data?.cardName ?? "Unknown"} (ID: {card.Data?.cardId ?? -1})");
                    
                    // Convert CardController to CardNetwork for PlayerController
                    CardNetwork cardNetwork = card.GetComponent<CardNetwork>();
                    if (cardNetwork != null) {
                        Debug.Log($"=== CardNetwork FOUND === Name: {cardNetwork.cardName.Value}, ID: {cardNetwork.cardId.Value}");
                        Debug.Log($"=== CALLING AddCardToHand === Player: {playerController.PlayerName}, Card: {cardNetwork.cardName.Value}");
                        
                        // This should trigger our debug logs in PlayerController
                        playerController.AddCardToHand(cardNetwork);
                        
                        Debug.Log($"=== AddCardToHand COMPLETED === for {playerController.PlayerName}");
                    } else {
                        Debug.LogError($"=== NO CardNetwork COMPONENT === on card: {card.Data?.cardName}");
                    }
                } else {
                    Debug.LogWarning($"=== DECK RETURNED NULL === for {playerController.PlayerName} on card {i + 1}");
                    break;
                }
            }
            
            // Final verification
            Debug.Log($"=== FINAL HAND COUNT === Player: {playerController.PlayerName}, Cards: {playerController.GetHandCardsCount()}");
            
            Debug.Log($"Distributed {cardsPerPlayer} cards to {playerController.PlayerName}");
        }
        
        Debug.Log($"=== DISTRIBUTE CARDS END ===");
    }

    // REPLACE the existing DrawCardFromDeck method in CardManager with this version:

    // UPDATE the DrawCardFromDeck() method in CardManager to completely remove PlayerManager fallback:

    public void DrawCardFromDeck(PlayerController playerController)
    {
        // All your existing code stays exactly the same
        if (playerController == null)
        {
            Debug.LogWarning("Invalid player.");
            return;
        }

        Deck deck = DeckManager.Instance.DeckInstance.GetComponent<Deck>();
        if (deck == null)
        {
            Debug.LogError("Deck is not found.");
            return;
        }

        CardController card = deck.RemoveCardControllerFromDeck();
        if (card != null)
        {
            CardNetwork cardNetwork = card.GetComponent<CardNetwork>();
            if (cardNetwork != null)
            {
                Debug.Log($"[TRIGGER ANIMATION] About to call animation for {playerController.PlayerName}");

                // ADD: Subscribe to animation complete event BEFORE starting animation
                System.Action<int, ulong> onComplete = null;
                onComplete = (animatedCardId, animatedClientId) =>
                {
                    if (animatedCardId == cardNetwork.cardId.Value && animatedClientId == playerController.ClientId)
                    {
                        Debug.Log($"[ANIMATION COMPLETE] Refreshing UI for {playerController.PlayerName}");
                        playerController.RefreshHandUI();

                        // Unsubscribe to avoid memory leaks
                        AnimationManager.OnAnimationComplete -= onComplete;
                    }
                };
                AnimationManager.OnAnimationComplete += onComplete;

                // Your existing animation call stays exactly the same
                if (AnimationManager.Instance != null)
                {
                    Debug.Log($"[STEP3-FINAL] Using AnimationManager only - no fallback");
                    AnimationManager.Instance.PlayCardDrawAnimation(
                        cardNetwork.cardId.Value,
                        playerController.ClientId
                    );
                }
                else
                {
                    Debug.LogError("[STEP3-FINAL] AnimationManager not available! Check AnimationManager setup.");
                }

                // Your existing immediate add stays exactly the same
                playerController.AddCardToHand(cardNetwork);
            }
        }
        else
        {
            Debug.LogWarning("Deck is out of cards.");
        }
    }


// REMOVE ALL OTHER ANIMATION METHODS FOR NOW
    

    

    // ========================================
    // OVERLOADED METHODS - For backwards compatibility during transition
    // ========================================
    
    // Overload for List<PlayerNetwork> - converts to PlayerController
    public void DistributeCards(List<PlayerNetwork> players) 
    {
        // Convert PlayerNetwork list to PlayerController list
        var playerControllers = new List<PlayerController>();
        
        foreach (var playerNetwork in players) {
            var playerController = PlayerManager.Instance.playerControllers
                .Find(pc => pc.Data.playerDbId == playerNetwork.PlayerDbId.Value);
            
            if (playerController != null) {
                playerControllers.Add(playerController);
            }
        }
        
        // Call the main method
        DistributeCards(playerControllers);
    }

    // Overload for PlayerNetwork - converts to PlayerController
    

    // ========================================
    // UTILITY METHODS
    // ========================================
    
    public string GetCardNameById(int cardId)
    {
        var cardData = allCardsList.Find(card => card.cardId == cardId);
        return cardData != null ? cardData.cardName : "Unknown Card";
    }
    
    // UTILITY METHODS - CardController interface
    public List<CardController> GetAllCardControllers()
    {
        return allSpawnedCards.Select(cardNetwork => cardNetwork.GetComponent<CardController>())
                             .Where(controller => controller != null)
                             .ToList();
    }
    
    public CardController GetCardControllerByName(string cardName)
    {
        foreach (CardNetwork cardNetwork in allSpawnedCards)
        {
            if (cardNetwork != null && cardNetwork.cardName.Value.ToString() == cardName)
            {
                return cardNetwork.GetComponent<CardController>();
            }
        }
        return null;
    }
}