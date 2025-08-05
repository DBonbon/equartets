using System.Linq;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;
    
    public delegate void EnableUIEvent(bool enableUI);
    public static event EnableUIEvent OnEnableUI;
    
    // UPDATED: Use CardController for game logic
    private CardController selectedCard;
    private PlayerController selectedPlayer; // UPDATED: PlayerNetwork → PlayerController
    private PlayerController currentPlayer; // UPDATED: PlayerNetwork → PlayerController
    private bool isPlayerUIEnabled = true;
    private bool isDrawingCard = false;
    private bool hasHandledCurrentPlayer = false;
    private bool isInitialized = false;
    private bool activateTurnUIFlag = false;
    
    // UPDATED: Work with PlayerControllers
    private List<PlayerController> playerControllers;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // UPDATED: Get PlayerControllers instead of PlayerNetworks
            playerControllers = PlayerManager.Instance.playerControllers;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartTurnManager()
    {
        Debug.Log("[TurnManager] turnmanager started");
        AssignTurnToPlayer();
        StartTurnLoop();
    }

    private void AssignTurnToPlayer()
    {
        Debug.Log("[TurnManager] AssignTurnToPlayer is called");
        if (playerControllers.Count == 0) return;

        // UPDATED: Set all players turn to false using PlayerController
        foreach (var playerController in playerControllers)
        {
            playerController.SetTurn(false);
        }

        // UPDATED: Assign turn to random PlayerController
        int randomIndex = UnityEngine.Random.Range(0, playerControllers.Count);
        currentPlayer = playerControllers[randomIndex];
        currentPlayer.SetTurn(true);
        
        // UPDATED: Update player lists using PlayerController interface
        currentPlayer.UpdatePlayerToAskList(playerControllers);
        currentPlayer.UpdateCardsPlayerCanAsk();

        Debug.Log($"Turn assigned to player: {currentPlayer.PlayerName}");
    }

    private void StartTurnLoop()
    {
        if (!isInitialized)
        {
            Debug.Log("Turn Manager Started");
            isInitialized = true;
            
            // UPDATED: Find current player using PlayerController
            currentPlayer = playerControllers.Find(player => player.HasTurn);
            Debug.Log($"call TurnLoop: {currentPlayer?.PlayerName}");

            if (currentPlayer != null)
            {
                StartCoroutine(TurnLoop());
                Debug.Log($"call coroutine TurnLoop: {currentPlayer.PlayerName}");
            }
            else
            {
                Debug.LogError("No initial player with hasTurn == true found.");
            }
        }
    }

    private System.Collections.IEnumerator TurnLoop()
    {
        Debug.Log("Turn loop is running");
        while (true)
        {
            Debug.Log($"Turn loop current player: {currentPlayer.PlayerName} with turn status: {currentPlayer.HasTurn}");
            if (currentPlayer.HasTurn)
            {
                if (!hasHandledCurrentPlayer)
                {
                    Debug.Log($"Turn loop hasHandledCurrentPlayer current player: {currentPlayer.PlayerName} with turn status: {currentPlayer.HasTurn} and hashandlecurrentplay flag is: {hasHandledCurrentPlayer}");
                    HandlePlayerTurn(currentPlayer);
                    Debug.Log($"Turn loop hasHandledCurrentPlayer current player: {currentPlayer.PlayerName} with turn status: {currentPlayer.HasTurn}");
                    Debug.Log($" hashandlecurrentplay flag is: {hasHandledCurrentPlayer}");
                    hasHandledCurrentPlayer = true;
                    Debug.Log($" hashandlecurrentplay1 flag is: {hasHandledCurrentPlayer}");
                    Debug.Log($"Turn loop hasHandledCurrentPlayer current player: {currentPlayer.PlayerName} and hashandlecurrentplay flag is: {hasHandledCurrentPlayer}");
                }
            }

            if (!currentPlayer.HasTurn)
            {
                Debug.Log($"Turn loop hasHandledCurrentPlayer current player: {currentPlayer.PlayerName} has Turn: {currentPlayer.HasTurn}");
                hasHandledCurrentPlayer = false;
                NextCurrentPlayer();
            }
            Debug.Log("Turn loop is still running");
            yield return null;
        }
        Debug.Log("Turn loop terminated");
    }

    public void OnEventGuessClick(ulong playerId, NetworkVariable<int> cardId)
    {
        Debug.Log($"The playerid value is: {playerId}, and cardid: {cardId}");
        
        // Get selected card
        CardController selectedCard = CardManager.Instance.FetchCardControllerById(cardId);
        Debug.Log($"oneventguessclick selected card: {selectedCard.Data.cardName}");
        
        // UPDATED: Find PlayerController by ClientId instead of OwnerClientId
        PlayerController selectedPlayer = playerControllers.Find(player => player.ClientId == playerId);
        Debug.Log($"oneventguessclick selected player: {selectedPlayer?.PlayerName}");
        
        this.selectedCard = selectedCard;
        this.selectedPlayer = selectedPlayer;
        
        if (currentPlayer != null && !isDrawingCard)
        {
            HandlePlayerTurn(currentPlayer);
            Debug.Log($"HandlePlayerTurn currentPlayer is: {currentPlayer.PlayerName}");
        }
        else
        {
            Debug.LogWarning($"{Time.time}: Invalid player turn.");
        }
    }

    private void HandlePlayerTurn(PlayerController currentPlayer)
    {
        Debug.Log("HandlePlayerTurn is called");
        Debug.Log($"currentPlayer is: {currentPlayer.PlayerName}");
        Debug.Log($"Selected Card: {(selectedCard != null ? selectedCard.Data.cardName : "None")}");
        ActivateTurnUI();
        Debug.Log("ActivateTurnUI is called");

        if (selectedCard != null && selectedPlayer != null)
        {
            GuessCheck(selectedCard, selectedPlayer);
            Debug.Log("GuessCheck is called");
            selectedCard = null;
            selectedPlayer = null;
        }
        else
        {
            Debug.Log($"handle player method waits for selectedCard: {selectedCard?.Data.cardName} and/or selectedPlayer {selectedPlayer?.PlayerName}");
        }
    }

    private void ActivateTurnUI()
    {
        activateTurnUIFlag = !activateTurnUIFlag;
        Debug.Log("starting Activating Turn UI for player: " + currentPlayer.PlayerName);

        if (activateTurnUIFlag)
        {
            Debug.Log("Activating Turn UI for player: " + currentPlayer.PlayerName);
        }
        else
        {
            Debug.Log("Deactivating Turn UI for player: " + currentPlayer.PlayerName);
        }
    }

    private void GuessCheck(CardController selectedCard, PlayerController selectedPlayer)
    {
        ActivateTurnUI();
        Debug.Log("GuessCheck is running");
        
        // UPDATED: Use PlayerController-compatible rules check
        // Convert to PlayerNetwork temporarily for rules engine compatibility
        var currentPlayerNetwork = currentPlayer.GetComponent<PlayerNetwork>();
        var selectedPlayerNetwork = selectedPlayer.GetComponent<PlayerNetwork>();
        
        // Use GameRules to determine if guess is correct
        bool isCorrectGuess = QuartetsGameRules.ProcessGuess(currentPlayerNetwork, selectedPlayerNetwork, selectedCard);
        
        if (isCorrectGuess)
        {
            CorrectGuess();
        }
        else
        {
            WrongGuess();
        }
    }

    private void CorrectGuess()
    {
        Debug.Log("AskForCard guess is correct");
        TransferCard(selectedCard, currentPlayer);
        Debug.Log($"TransferCard is correct: {selectedCard.Data.cardName}");
        CheckForQuartets(); 
        selectedCard = null;
        selectedPlayer = null;
        
        if (!IsPlayerHandEmpty(currentPlayer))
        {
            HandlePlayerTurn(currentPlayer);
        }
        else if (DeckManager.Instance.CurrentDeck != null && DeckManager.Instance.CurrentDeck.DeckCards.Count > 0)
        {
            DrawCardFromDeck(() =>
            {
                if (!IsPlayerHandEmpty(currentPlayer))
                {
                    HandlePlayerTurn(currentPlayer);
                }
            });
        }
    }

    private void WrongGuess()
    {
        Debug.Log("ask for card, player doesn't have card");
        DisplayMessage($"{selectedPlayer.PlayerName} does not have {selectedCard.Data.cardName}.");
        DrawCardFromDeck(() => 
        {
            Debug.Log("ask for card, call end turn");
            EndTurn();
        });
    }
    
    private void TransferCard(CardController selectedCard, PlayerController curPlayer)
    {
        Debug.Log("TransferCard is correct");    
        
        // UPDATED: Direct PlayerController operations
        if (selectedPlayer != null && currentPlayer != null) {
            // Get CardNetwork from CardController for PlayerController interface
            var cardNetwork = selectedCard.GetComponent<CardNetwork>();
            selectedPlayer.RemoveCardFromHand(cardNetwork);
            currentPlayer.AddCardToHand(cardNetwork);
        }
    }

    private bool IsPlayerHandEmpty(PlayerController currentPlayer)
    {
        // UPDATED: Use PlayerController interface
        return currentPlayer.IsHandEmpty();
    }

    private void EndTurn()
    {
        NextCurrentPlayer();
        Debug.Log("end turn is running");
    }

    private void NextCurrentPlayer()
    {
        Debug.Log("next current player is called");
        if (playerControllers.Count == 0) return;

        // UPDATED: Work with PlayerController list
        int currentIndex = playerControllers.IndexOf(currentPlayer);
        
        if (currentIndex == -1) return;

        // UPDATED: Use PlayerController.SetTurn()
        currentPlayer.SetTurn(false);

        int nextIndex = (currentIndex + 1) % playerControllers.Count;

        if (nextIndex < playerControllers.Count && nextIndex >= 0)
        {
            currentPlayer = playerControllers[nextIndex];
            currentPlayer.SetTurn(true);
            
            // UPDATED: Use PlayerController interface
            currentPlayer.UpdatePlayerToAskList(playerControllers);
            currentPlayer.UpdateCardsPlayerCanAsk();
            
            Debug.Log($"Turn assigned to player: {currentPlayer.PlayerName}");
        }
        else
        {
            Debug.LogError("Next player index is out of valid range.");
        }
    }

    private void CheckForQuartets()
    {
        Debug.Log("check for quartets is called");
        
        // UPDATED: Use PlayerNetwork temporarily for compatibility with rules engine
        var currentPlayerNetwork = currentPlayer.GetComponent<PlayerNetwork>();
        var completedQuartets = QuartetsGameRules.GetCompletedQuartets(currentPlayerNetwork);
        
        if (completedQuartets.Count > 0)
        {
            foreach (string suit in completedQuartets)
            {
                // UPDATED: Get hand cards and handle quartet movement externally
                var cardsToMove = currentPlayer.GetHandCards()
                    .Where(card => card.suit.Value.ToString() == suit)
                    .Take(4)
                    .ToList();
                
                // UPDATED: Handle quartet logic externally instead of in PlayerController
                foreach (var card in cardsToMove)
                {
                    // Remove card from player hand
                    currentPlayer.RemoveCardFromHand(card);
                    
                    // Add to quartets - convert CardNetwork to CardController
                    var cardController = card.GetComponent<CardController>();
                    if (cardController != null)
                    {
                        Quartets quartetZone = QuartetManager.Instance.QuartetInstance.GetComponent<Quartets>();
                        if (quartetZone != null)
                        {
                            quartetZone.AddCardToQuartet(cardController);
                        }
                    }
                }
                
                // Increment player score
                currentPlayer.IncrementScore();
                
                Debug.Log($"TurnManager: Moved quartet of {suit} for {currentPlayer.PlayerName}");
            }
        }
        
        if (IsPlayerHandEmpty(currentPlayer) && DeckManager.Instance.CurrentDeck.DeckCards.Count == 0)
        {
            CheckGameEnd();
            EndTurn();
        }
    }

    private void DisplayMessage(string message)
    {
        Debug.Log("Display Message: " + message);
    }

    public void DrawCardFromDeck(Action onCardDrawn)
    {
        Debug.Log("draw card from deck is called");
        // UPDATED: Use PlayerController interface
        CardManager.Instance.DrawCardFromDeck(currentPlayer);
        
        onCardDrawn?.Invoke();
    }

    private void CheckGameEnd()
    {
        int cardsLeftInDeck = DeckManager.Instance.CurrentDeck?.DeckCards.Count ?? 0;
        
        // UPDATED: Convert to PlayerNetwork temporarily for rules compatibility
        var playerNetworks = playerControllers.Select(pc => pc.GetComponent<PlayerNetwork>()).ToList();
        
        if (QuartetsGameRules.IsGameOver(playerNetworks, cardsLeftInDeck))
        {
            GameEnd();
        }
    }

    private void GameEnd()
    {
        Debug.Log($"{Time.time}: Game Ended");
    }
}