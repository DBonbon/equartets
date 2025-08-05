using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pure game rules for Quartets - updated to work with CardController interface
/// Contains only the logic of how the Quartets card game works
/// </summary>
public static class QuartetsGameRules
{
    /// <summary>
    /// Check if a guess is valid according to Quartets rules - CardController interface
    /// </summary>
    public static bool IsValidGuess(PlayerNetwork guessingPlayer, PlayerNetwork targetPlayer, CardController requestedCard)
    {
        // Convert to CardNetwork for compatibility with current PlayerNetwork structure
        var requestedCardNetwork = requestedCard.GetComponent<CardNetwork>();
        return IsValidGuess(guessingPlayer, targetPlayer, requestedCardNetwork);
    }
    
    /// <summary>
    /// Check if a guess is valid according to Quartets rules - CardNetwork version (internal)
    /// </summary>
    public static bool IsValidGuess(PlayerNetwork guessingPlayer, PlayerNetwork targetPlayer, CardNetwork requestedCard)
    {
        // Rule: Can't guess if you already have the card
        bool alreadyHasCard = guessingPlayer.HandCards.Any(c => c.cardId.Value == requestedCard.cardId.Value);
       
        // Rule: Target must actually have the card
        bool targetHasCard = targetPlayer.HandCards.Contains(requestedCard);
       
        // Rule: Can only ask for cards in suits you already have
        bool hasSameSuit = guessingPlayer.HandCards.Any(c => c.suit.Value.ToString() == requestedCard.suit.Value.ToString());
       
        return !alreadyHasCard && targetHasCard && hasSameSuit;
    }
   
    /// <summary>
    /// Process the result of a guess - returns true if guess was correct - CardController interface
    /// </summary>
    public static bool ProcessGuess(PlayerNetwork guessingPlayer, PlayerNetwork targetPlayer, CardController requestedCard)
    {
        var requestedCardNetwork = requestedCard.GetComponent<CardNetwork>();
        return ProcessGuess(guessingPlayer, targetPlayer, requestedCardNetwork);
    }
    
    /// <summary>
    /// Process the result of a guess - returns true if guess was correct - CardNetwork version (internal)
    /// </summary>
    public static bool ProcessGuess(PlayerNetwork guessingPlayer, PlayerNetwork targetPlayer, CardNetwork requestedCard)
    {
        if (!IsValidGuess(guessingPlayer, targetPlayer, requestedCard))
        {
            Debug.LogWarning("Invalid guess attempted");
            return false;
        }
       
        // Target has the card - guess is correct
        return targetPlayer.HandCards.Contains(requestedCard);
    }
   
    /// <summary>
    /// Check if player has completed any quartets (4 cards of same suit)
    /// Returns list of completed quartet suits
    /// </summary>
    public static List<string> GetCompletedQuartets(PlayerNetwork player)
    {
        var completedSuits = new List<string>();
       
        // Group cards by suit
        var groupedBySuit = player.HandCards.GroupBy(card => card.suit.Value.ToString());
       
        foreach (var suitGroup in groupedBySuit)
        {
            if (suitGroup.Count() >= 4) // Complete quartet
            {
                completedSuits.Add(suitGroup.Key);
            }
        }
       
        return completedSuits;
    }
   
    /// <summary>
    /// Check if the game should end
    /// Game ends when all cards are in quartets (no cards left in hands or deck)
    /// </summary>
    public static bool IsGameOver(List<PlayerNetwork> players, int cardsLeftInDeck)
    {
        // Game ends when no cards left in any hands AND no cards left in deck
        bool allHandsEmpty = players.All(p => p.HandCards.Count == 0);
        bool deckEmpty = cardsLeftInDeck == 0;
       
        return allHandsEmpty && deckEmpty;
    }
   
    /// <summary>
    /// Determine winner(s) based on quartets completed
    /// </summary>
    public static List<PlayerNetwork> GetWinners(List<PlayerNetwork> players)
    {
        if (players.Count == 0) return new List<PlayerNetwork>();
       
        int maxScore = players.Max(p => p.Score.Value);
        return players.Where(p => p.Score.Value == maxScore).ToList();
    }
   
    /// <summary>
    /// Check if a player's hand is empty
    /// </summary>
    public static bool IsHandEmpty(PlayerNetwork player)
    {
        return player.HandCards.Count == 0;
    }
    
    /// <summary>
    /// FUTURE: CardController-based methods for when PlayerNetwork is fully migrated
    /// These methods show the direction we're heading towards
    /// </summary>
    
    // Future method signatures (commented out for now):
    /*
    public static bool IsValidGuess(PlayerController guessingPlayer, PlayerController targetPlayer, CardController requestedCard)
    {
        // Pure CardController logic - no CardNetwork dependencies
        // Will be implemented when PlayerController interface is complete
    }
    
    public static List<string> GetCompletedQuartets(PlayerController player)
    {
        // Check quartets using CardController interface
        // Will replace the PlayerNetwork version
    }
    
    public static bool IsGameOver(List<PlayerController> players, int cardsLeftInDeck)
    {
        // Game over logic using CardController interface
        // Clean separation from network layer
    }
    */
}