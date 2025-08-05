using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Handles all card animation effects - flying cards, dealing, shuffling, etc.
/// Attach this to PlayerUI GameObject to keep animation logic separate
/// </summary>
public class CardAnimationManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float cardMoveDuration = 3.0f;
    [SerializeField] private float cardStaggerDelay = 1.0f;
    [SerializeField] private Ease moveEasing = Ease.OutQuart;
    
    [Header("Flying Card Prefab")]
    [SerializeField] private GameObject flyingCardPrefab; // Assign a simple card back prefab
    
    [Header("Canvas for Flying Cards")]
    [SerializeField] private Canvas animationCanvas; // High sorting order canvas for flying cards
    
    // Animation state
    private bool animationInProgress = false;
    
    /// <summary>
    /// Animate cards flying from deck to player hand
    /// </summary>
    /// <param name="finalCards">The actual cards that will be shown at the end</param>
    /// <param name="deckPosition">World position of the deck</param>
    /// <param name="onComplete">Callback when all animations complete</param>
    public void AnimateCardsFromDeckToHand(List<CardUI> finalCards, Vector3 deckPosition, System.Action onComplete = null)
    {
        if (animationInProgress)
        {
            Debug.LogWarning("CardAnimationManager: Animation already in progress");
            return;
        }
        
        StartCoroutine(PerformCardDealingAnimation(finalCards, deckPosition, onComplete));
    }
    
    /// <summary>
    /// Main animation coroutine
    /// </summary>
    private IEnumerator PerformCardDealingAnimation(List<CardUI> finalCards, Vector3 deckPosition, System.Action onComplete)
    {
        animationInProgress = true;
        
        Debug.Log($"CardAnimationManager: Starting animation for {finalCards.Count} cards");
        
        for (int i = 0; i < finalCards.Count; i++)
        {
            CardUI finalCard = finalCards[i];
            
            // Create and animate flying card
            yield return StartCoroutine(AnimateSingleCard(finalCard, deckPosition, i));
        }
        
        animationInProgress = false;
        onComplete?.Invoke();
        
        Debug.Log("CardAnimationManager: All card animations completed");
    }
    
    /// <summary>
    /// Animate a single card from deck to its final position
    /// </summary>
    private IEnumerator AnimateSingleCard(CardUI finalCard, Vector3 deckPosition, int cardIndex)
    {
        // Wait for stagger delay
        yield return new WaitForSeconds(cardIndex * cardStaggerDelay);
        
        // Create flying card
        GameObject flyingCard = CreateFlyingCard(deckPosition);
        
        // Get target position (where the final card should appear)
        Vector3 targetPosition = finalCard.transform.position;
        
        Debug.Log($"CardAnimationManager: Flying card {cardIndex} from {deckPosition} to {targetPosition}");
        
        // Animate flying card to target
        bool animationComplete = false;
        
        flyingCard.transform.DOMove(targetPosition, cardMoveDuration)
            .SetEase(moveEasing)
            .OnComplete(() => {
                // When flying card arrives:
                ShowFinalCard(finalCard, flyingCard);
                animationComplete = true;
            });
        
        // Wait for animation to complete
        while (!animationComplete)
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// Create a flying card object for animation
    /// </summary>
    private GameObject CreateFlyingCard(Vector3 startPosition)
    {
        GameObject flyingCard;
        
        if (flyingCardPrefab != null)
        {
            // Use provided prefab
            flyingCard = Instantiate(flyingCardPrefab);
        }
        else
        {
            // Create simple card back
            flyingCard = CreateSimpleCardBack();
        }
        
        // Set parent to animation canvas if available
        if (animationCanvas != null)
        {
            flyingCard.transform.SetParent(animationCanvas.transform, false);
        }
        
        // Position at deck
        flyingCard.transform.position = startPosition;
        
        return flyingCard;
    }
    
    /// <summary>
    /// Create a simple card back when no prefab is provided
    /// </summary>
    private GameObject CreateSimpleCardBack()
    {
        GameObject cardBack = new GameObject("FlyingCard");
        
        // Add UI Image component
        var image = cardBack.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.3f, 0.8f, 1f); // Blue card back
        
        // Set size
        var rectTransform = cardBack.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(60, 80); // Standard card size
        
        return cardBack;
    }
    
    /// <summary>
    /// Show the final card and clean up flying card
    /// </summary>
    private void ShowFinalCard(CardUI finalCard, GameObject flyingCard)
    {
        // Show the real card
        finalCard.gameObject.SetActive(true);
        finalCard.SetFaceUp(true);
        
        // Optional: Card arrival effect
        finalCard.transform.localScale = Vector3.one * 0.8f;
        finalCard.transform.DOScale(1.1f, 0.15f)
            .OnComplete(() => finalCard.transform.DOScale(1f, 0.1f));
        
        // Clean up flying card
        if (flyingCard != null)
        {
            Destroy(flyingCard);
        }
        
        Debug.Log($"CardAnimationManager: Final card shown - {finalCard.CardName}");
    }
    
    /// <summary>
    /// Stop all animations (for cleanup or interruption)
    /// </summary>
    public void StopAllAnimations()
    {
        StopAllCoroutines();
        DOTween.Kill(transform);
        animationInProgress = false;
        
        // Clean up any remaining flying cards
        if (animationCanvas != null)
        {
            foreach (Transform child in animationCanvas.transform)
            {
                if (child.name.Contains("FlyingCard"))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// Check if animation is currently running
    /// </summary>
    public bool IsAnimating => animationInProgress;
    
    #region Settings Accessors (for runtime tweaking)
    public void SetAnimationSpeed(float duration) => cardMoveDuration = duration;
    public void SetStaggerDelay(float delay) => cardStaggerDelay = delay;
    public void SetEasing(Ease easing) => moveEasing = easing;
    #endregion
}