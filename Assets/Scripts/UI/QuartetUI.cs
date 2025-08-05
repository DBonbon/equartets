using System.Collections.Generic;
using UnityEngine;

public class QuartetUI : MonoBehaviour
{
    [Header("Prefab UI Elements")]
    [SerializeField] private Transform quartetUIContainer; // Parent container with quartet UI elements as children
    
    // Canvas references (set by QuartetManager)
    private Transform quartetCanvasReference;   // Where quartet UI elements go
    private Transform cardCanvasContainer;      // Where CardUI objects go
    
    // Fallback
    [SerializeField] private Transform quartetDisplayTransform;
    
    // Set both canvas references - they are independent
    public void SetCanvasReferences(Transform quartetReference, Transform cardContainer)
    {
        quartetCanvasReference = quartetReference;
        cardCanvasContainer = cardContainer;
        
        // Move quartet UI elements to their canvas reference
        MoveQuartetUIElementsToCanvas();
        
        Debug.Log($"Canvas references set - Quartet UI: {quartetReference.name}, Cards: {cardContainer.name}");
    }
    
    // Move quartet UI container (with all children) to canvas
    private void MoveQuartetUIElementsToCanvas()
    {
        if (quartetCanvasReference == null || quartetUIContainer == null) return;
        
        // Get or add RectTransform to the container
        RectTransform containerRect = quartetUIContainer.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            containerRect = quartetUIContainer.gameObject.AddComponent<RectTransform>();
        }
        
        // Store original values
        Vector2 originalSize = containerRect.sizeDelta;
        Vector3 originalScale = containerRect.localScale;
        
        // Move entire container to quartet canvas reference
        containerRect.SetParent(quartetCanvasReference, false);
        
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
            containerRect.sizeDelta = new Vector2(300, 200); // Default size for quartet area
        }
        
        Debug.Log($"Quartet UI container moved to canvas quartet reference, size: {containerRect.sizeDelta}");
    }
    
    // Legacy method for backward compatibility
    public void SetQuartetDisplayTransform(Transform newDisplayTransform)
    {
        if (cardCanvasContainer == null)
        {
            cardCanvasContainer = newDisplayTransform;
        }
    }
   
    public void UpdateQuartetUIWithIDs(List<int> cardIDs)
    {
        // Use card canvas container (separate from quartet UI elements)
        Transform targetContainer = cardCanvasContainer != null ? cardCanvasContainer : quartetDisplayTransform;
        
        if (targetContainer == null)
        {
            Debug.LogError("No card container set for QuartetUI. CardUI objects cannot be displayed.");
            return;
        }
        
        Debug.Log($"Updating QuartetUI with {cardIDs.Count} cards in card container: {targetContainer.name}");
        
        // Deactivate all child GameObjects in the card container
        foreach (Transform child in targetContainer)
        {
            child.gameObject.SetActive(false);
        }
        
        // Activate and parent CardUI objects to the card container (separate from quartet UI)
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
