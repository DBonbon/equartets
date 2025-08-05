using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public struct SiblingName : INetworkSerializable, IEquatable<SiblingName>
{
    private FixedString32Bytes name;
    public string Name => name.ToString();
    
    public SiblingName(string name)
    {
        this.name = name;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
    }
    
    public bool Equals(SiblingName other)
    {
        return name.Equals(other.name);
    }
    
    public override bool Equals(object obj)
    {
        return obj is SiblingName other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return name.GetHashCode();
    }
}
/// <summary>
/// Pure network representation of a card instance in the game
/// This is the actual card object that moves between players
/// No UI logic - only network state and data
/// </summary>
public class CardNetwork : NetworkBehaviour, IComparable<CardNetwork>
{
    [Header("Network Card Data")]
    public NetworkVariable<int> cardId = new NetworkVariable<int>();
    public NetworkVariable<FixedString128Bytes> cardName = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<FixedString128Bytes> suit = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<FixedString128Bytes> hint = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<FixedString128Bytes> level = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<FixedString128Bytes> cardImagePath = new NetworkVariable<FixedString128Bytes>();

    public NetworkList<SiblingName> siblingNames = new NetworkList<SiblingName>();

    /// <summary>
    /// Initialize this card instance with data from CardData
    /// Should only be called on server
    /// </summary>
    public void InitializeCard(int id, string name, string cardSuit, string cardHint, List<string> siblings)
    {
        if (!IsServer)
        {
            Debug.LogWarning("InitializeCard should only be called on server");
            return;
        }

        Debug.Log($"Initializing CardNetwork: {name}");

        cardId.Value = id;
        cardName.Value = name;
        suit.Value = cardSuit;
        hint.Value = cardHint;

        siblingNames.Clear();
        foreach (var sibling in siblings)
        {
            siblingNames.Add(new SiblingName(sibling));
        }

        Debug.Log($"CardNetwork initialized - ID: {cardId.Value}, Name: {cardName.Value}");
    }

    /// <summary>
    /// Get the static card data this instance represents
    /// </summary>
    public CardData GetCardData()
    {
        // Find the matching CardData from DataManager
        var allCardData = DataManager.LoadedCardData;
        if (allCardData != null)
        {
            return allCardData.Find(data => data.cardId == cardId.Value);
        }

        Debug.LogWarning($"Could not find CardData for cardId: {cardId.Value}");
        return null;
    }

    /// <summary>
    /// Compare cards by suit for sorting
    /// </summary>
    public int CompareTo(CardNetwork other)
    {
        if (other == null) return 1;
        return String.Compare(suit.Value.ToString(), other.suit.Value.ToString(), StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return $"CardNetwork({cardId.Value}: {cardName.Value})";
    }
}