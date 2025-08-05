using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Quartets : NetworkBehaviour
{
    [SerializeField] public Transform cardsContainer;
   
    // INTERNAL: Keep CardNetwork for network synchronization
    private List<CardNetwork> quartetCards = new List<CardNetwork>();
   
    // PUBLIC: Expose as CardController list for external access
    public List<CardController> QuartetCards
    {
        get
        {
            return quartetCards.Select(cardNetwork => cardNetwork.GetComponent<CardController>())
                              .Where(controller => controller != null)
                              .ToList();
        }
    }
   
    private QuartetUI quartetUI;
   
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        quartetUI = GetComponent<QuartetUI>();
        
        // Notify QuartetManager that this quartet has spawned
        if (QuartetManager.Instance != null)
        {
            QuartetManager.Instance.OnQuartetSpawned(this);
        }
    }
   
    // PUBLIC INTERFACE: Accept CardController for game logic
    public void AddCardToQuartet(CardController cardController)
    {
        if (cardController != null)
        {
            var cardNetwork = cardController.GetComponent<CardNetwork>();
            if (cardNetwork != null)
            {
                AddCardToQuartet(cardNetwork); // Use existing implementation
            }
        }
    }
   
    // INTERNAL: Keep existing CardNetwork method for network synchronization
    public void AddCardToQuartet(CardNetwork card)
    {
        if (card != null && IsServer)
        {
            quartetCards.Add(card);
            Debug.Log($"Card {card.cardName} added to QuartetCards list.");
           
            UpdateQuartetUI();
        }
        else
        {
            Debug.LogError("Attempted to add a null card to Quartets.");
        }
    }
   
    private void UpdateQuartetUI()
    {
        if (IsServer)
        {
            List<int> cardIDs = quartetCards.Select(card => card.cardId.Value).ToList();
            quartetUI?.UpdateQuartetUIWithIDs(cardIDs);
           
            // Synchronize this list with clients
            UpdateQuartetUIOnClients_ClientRpc(cardIDs.ToArray());
        }
    }
   
    [ClientRpc]
    private void UpdateQuartetUIOnClients_ClientRpc(int[] cardIDs)
    {
        if (quartetUI != null)
        {
            quartetUI.UpdateQuartetUIWithIDs(new List<int>(cardIDs));
        }
    }
   
    // UTILITY METHODS for CardController interface
    public int GetQuartetCount()
    {
        return quartetCards.Count;
    }
   
    public bool HasQuartets()
    {
        return quartetCards.Count > 0;
    }
   
    // Get quartets by suit for CardController interface
    public List<CardController> GetQuartetsBySuit(string suit)
    {
        return quartetCards.Where(card => card.suit.Value.ToString() == suit)
                          .Select(cardNetwork => cardNetwork.GetComponent<CardController>())
                          .Where(controller => controller != null)
                          .ToList();
    }
}