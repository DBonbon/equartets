using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Deck : NetworkBehaviour
{
    [SerializeField] public Transform cardsContainer; // For actual card GameObjects (not UI)
   
    private List<CardNetwork> deckCards = new List<CardNetwork>();
   
    public List<CardController> DeckCards
    {
        get
        {
            return deckCards.Select(cardNetwork => cardNetwork.GetComponent<CardController>())
                           .Where(controller => controller != null)
                           .ToList();
        }
    }
   
    private DeckUI deckUI;
   
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        deckUI = GetComponent<DeckUI>();
        
        // Notify DeckManager that this deck has spawned
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnDeckSpawned(this);
        }
    }
   
    public void AddCardToDeck(GameObject cardGameObject)
    {
        if (cardGameObject != null)
        {
            var cardNetworkComponent = cardGameObject.GetComponent<CardNetwork>();
            if (IsServer && cardNetworkComponent != null)
            {
                deckCards.Add(cardNetworkComponent);
                Debug.Log($"Card {cardNetworkComponent.name} added to deckCards list.");
               
                UpdateDeckUI();
            }
            else
            {
                Debug.LogError($"The GameObject deck does not have a CardNetwork component.");
            }
        }
        else
        {
            Debug.LogError("cardGameObject is null.");
        }
    }
   
    public CardController RemoveCardControllerFromDeck()
    {
        if (deckCards.Count > 0)
        {
            CardNetwork cardNetworkToGive = deckCards[0];
            deckCards.RemoveAt(0);
           
            UpdateDeckUI();
           
            return cardNetworkToGive.GetComponent<CardController>();
        }
        return null;
    }
    
    private void UpdateDeckUI()
    {
        List<int> cardIDs = deckCards.Select(card => card.cardId.Value).ToList();
        
        if (deckUI != null)
        {
            deckUI.UpdateDeckUIWithIDs(cardIDs);
        }
        
        if (IsServer)
        {
            UpdateDeckUIOnClients_ClientRpc(cardIDs.ToArray());
        }
    }
   
    [ClientRpc]
    private void UpdateDeckUIOnClients_ClientRpc(int[] cardIDs)
    {
        if (deckUI != null)
        {
            deckUI.UpdateDeckUIWithIDs(new List<int>(cardIDs));
        }
    }
   
    public int GetCardCount() => deckCards.Count;
    public bool IsEmpty() => deckCards.Count == 0;
}