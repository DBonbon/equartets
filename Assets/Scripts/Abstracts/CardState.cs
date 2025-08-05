using System.Collections.Generic;
using UnityEngine;

public class CardState
{
    // Pure data storage - no logic
    public CardData Data { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsFaceUp { get; private set; }
    public int CurrentOwnerId { get; private set; }
    public Vector3 Position { get; private set; }
    public Transform Parent { get; private set; }
    
    // Card game state
    public bool IsInHand { get; private set; }
    public bool IsInDeck { get; private set; }
    public bool IsInQuartet { get; private set; }
    
    // Constructor
    public CardState(CardData data)
    {
        Data = data;
        IsVisible = false;
        IsFaceUp = false;
        CurrentOwnerId = -1;
        Position = Vector3.zero;
        Parent = null;
        IsInHand = false;
        IsInDeck = true;  // Cards start in deck
        IsInQuartet = false;
    }
    
    // Simple setters - no complex logic
    public void SetVisibility(bool visible) { IsVisible = visible; }
    public void SetFaceUp(bool faceUp) { IsFaceUp = faceUp; }
    public void SetOwner(int ownerId) { CurrentOwnerId = ownerId; }
    public void SetPosition(Vector3 position) { Position = position; }
    public void SetParent(Transform parent) { Parent = parent; }
    
    // Location state setters
    public void SetInHand(bool inHand) 
    { 
        IsInHand = inHand;
        if (inHand) 
        { 
            IsInDeck = false; 
            IsInQuartet = false; 
        }
    }
    
    public void SetInDeck(bool inDeck) 
    { 
        IsInDeck = inDeck;
        if (inDeck) 
        { 
            IsInHand = false; 
            IsInQuartet = false;
            CurrentOwnerId = -1;
        }
    }
    
    public void SetInQuartet(bool inQuartet) 
    { 
        IsInQuartet = inQuartet;
        if (inQuartet) 
        { 
            IsInHand = false; 
            IsInDeck = false; 
        }
    }
}