using System.Collections.Generic;
using UnityEngine;

public class DeckUI : MonoBehaviour
{
    [Header("Prefab UI Elements")]
    [SerializeField] private Transform deckUIContainer; // Parent container with bgimage and bgdeck as children
    
    // Canvas references (set by DeckManager)
    private Transform deckCanvasReference;   // Where deck UI elements go
    private Transform cardCanvasContainer;   // Where CardUI objects go
    
    // Fallback
    [SerializeField] private Transform deckDisplayTransform;
    
    // Set both canvas references - they are independent
    public void SetCanvasReferences(Transform deckReference, Transform cardContainer)
    {
        deckCanvasReference = deckReference;
        cardCanvasContainer = cardContainer;
        
        // Move deck UI elements to their canvas reference
        MoveDeckUIElementsToCanvas();
        
        Debug.Log($"Canvas references set - Deck UI: {deckReference.name}, Cards: {cardContainer.name}");
    }
    
    // Move deck UI container (with all children) to canvas
    private void MoveDeckUIElementsToCanvas()
    {
        if (deckCanvasReference == null || deckUIContainer == null) return;
        
        // Get or add RectTransform to the container
        RectTransform containerRect = deckUIContainer.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            containerRect = deckUIContainer.gameObject.AddComponent<RectTransform>();
        }
        
        // Store original values
        Vector2 originalSize = containerRect.sizeDelta;
        Vector3 originalScale = containerRect.localScale;
        
        // Move entire container to deck canvas reference
        containerRect.SetParent(deckCanvasReference, false);
        
        // Restore scale
        containerRect.localScale = originalScale;
        
        // Position the container
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        
        // Preserve original size
        if (originalSize != Vector2.zero)
        {
            containerRect.sizeDelta = originalSize;
        }
        else
        {
            containerRect.sizeDelta = new Vector2(250, 350); // Default size
        }
        
        Debug.Log($"Deck UI container moved to canvas deck reference, size: {containerRect.sizeDelta}");
    }
    
    // Legacy method for backward compatibility
    public void SetCanvasContainer(Transform container)
    {
        if (cardCanvasContainer == null)
        {
            cardCanvasContainer = container;
        }
    }
    
    public void SetDeckDisplayTransform(Transform newDisplayTransform)
    {
        if (cardCanvasContainer == null)
        {
            cardCanvasContainer = newDisplayTransform;
        }
    }
   
    public void UpdateDeckUIWithIDs(List<int> cardIDs)
    {
        // Use card canvas container (separate from deck UI elements)
        Transform targetContainer = cardCanvasContainer != null ? cardCanvasContainer : deckDisplayTransform;
        
        if (targetContainer == null)
        {
            Debug.LogError("No card container set for DeckUI. CardUI objects cannot be displayed.");
            return;
        }
        
        Debug.Log($"Updating DeckUI with {cardIDs.Count} cards in card container: {targetContainer.name}");
        
        // Deactivate all child GameObjects in the card container
        foreach (Transform child in targetContainer)
        {
            child.gameObject.SetActive(false);
        }
        
        // Activate and parent CardUI objects to the card container (separate from deck UI)
        foreach (int cardID in cardIDs)
        {
            CardUI cardUI = CardManager.Instance?.FetchCardUIById(cardID);
            if (cardUI != null)
            {
                cardUI.gameObject.SetActive(true);
                cardUI.transform.SetParent(targetContainer, false);
                Debug.Log($"CardUI {cardID} parented to card container: {targetContainer.name}");
            }
            else
            {
                Debug.LogWarning($"No CardUI found for card ID: {cardID}");
            }
        }
    }
}