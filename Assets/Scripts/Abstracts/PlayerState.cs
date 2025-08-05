using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    // ========================================
    // PURE DATA STORAGE - NO LOGIC
    // ========================================
    public PlayerData Data { get; private set; }
    public int PlayerDbId => Data?.playerDbId ?? -1;
    public List<CardNetwork> HandCards { get; private set; }
    public int Score { get; private set; }
    public bool HasTurn { get; private set; }
    public List<CardNetwork> Quartets { get; private set; }
    public List<CardNetwork> CardsPlayerCanAsk { get; private set; }
    
    // Game state fields
    public bool IsWinner { get; private set; }
    public int Result { get; private set; }
    
    // ========================================
    // CONSTRUCTOR
    // ========================================
    public PlayerState(PlayerData data)
    {
        // Validate input
        if (data == null)
        {
            Debug.LogError("PlayerState: PlayerData cannot be null");
            return;
        }
        
        Data = data;
        HandCards = new List<CardNetwork>();
        CardsPlayerCanAsk = new List<CardNetwork>();
        Quartets = new List<CardNetwork>();
        Score = 0;
        HasTurn = false;
        IsWinner = false;
        Result = 0;
    }
    
    // ========================================
    // SIMPLE SETTERS - NO COMPLEX LOGIC
    // ========================================
    public void SetScore(int newScore) 
    { 
        Score = Mathf.Max(0, newScore); // Prevent negative scores
    }
    
    public void SetTurn(bool hasTurn) 
    { 
        HasTurn = hasTurn; 
    }
    
    public void SetWinner(bool isWinner) 
    { 
        IsWinner = isWinner; 
    }
    
    public void SetResult(int result) 
    { 
        Result = result; 
    }
    
    // ========================================
    // HAND CARDS OPERATIONS
    // ========================================
    public void AddCardToHand(CardNetwork card)
    {
        if (card != null && !HandCards.Contains(card))
        {
            HandCards.Add(card);
        }
    }
    
    public void RemoveCardFromHand(CardNetwork card)
    {
        if (card != null)
        {
            HandCards.Remove(card);
        }
    }
    
    // ========================================
    // CARDS PLAYER CAN ASK OPERATIONS
    // ========================================
    public void ClearCardsPlayerCanAsk()
    {
        CardsPlayerCanAsk.Clear();
    }
    
    public void AddCardPlayerCanAsk(CardNetwork card)
    {
        if (card != null && !CardsPlayerCanAsk.Contains(card))
        {
            CardsPlayerCanAsk.Add(card);
        }
    }
    
    // ========================================
    // QUARTETS OPERATIONS
    // ========================================
    public void AddToQuartets(CardNetwork card)
    {
        if (card != null && !Quartets.Contains(card))
        {
            Quartets.Add(card);
        }
    }
    
    public void RemoveFromQuartets(CardNetwork card)
    {
        if (card != null)
        {
            Quartets.Remove(card);
        }
    }
    
    public void ClearQuartets()
    {
        Quartets.Clear();
    }
    
    // ========================================
    // UTILITY METHODS (Read-only)
    // ========================================
    public bool IsHandEmpty()
    {
        return HandCards.Count == 0;
    }
    
    public int GetHandCount()
    {
        return HandCards.Count;
    }
    
    public int GetQuartetsCount()
    {
        return Quartets.Count;
    }
    
    public int GetCardsPlayerCanAskCount()
    {
        return CardsPlayerCanAsk.Count;
    }
}