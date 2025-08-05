# Animation Manager

# AnimationManager Documentation

## Overview

The `AnimationManager` is a centralized system for handling all game animations in the Unity project. It was created to separate animation logic from core game logic, providing better organization and maintainability.

## Architecture

### Design Principles
- **Separation of Concerns**: Animation logic is isolated from player management and card logic
- **Single Responsibility**: AnimationManager handles only animations, nothing else
- **Network Aware**: Properly handles client-server animation synchronization
- **singleton**: Be a singleton (AnimationManager.Instance)
- **AnimateCardDraw**:Handle AnimateCardDraw(cardId, targetPlayerClientId) calls...Replace animation logic currently in PlayerManager.StartBasicCardAnimation
- **Modular**: Easy to extend with new animation types

### Current Implementation
- **Singleton Pattern**: Single instance accessible via `AnimationManager.Instance`
- **NetworkBehaviour**: Supports multiplayer animations via ClientRpc calls
- **DOTween Integration**: Uses DOTween for smooth, performant animations

## Current Features

### Card Draw Animation
The primary animation currently implemented:

```csharp
// Trigger from server
AnimationManager.Instance.PlayCardDrawAnimation(cardId, targetPlayerClientId);
```

**Flow:**
1. Server calls `PlayCardDrawAnimation()`
2. Triggers `TriggerCardDrawAnimation_ClientRpc()` on all clients
3. Each client finds the card and target player
4. Executes `ExecuteCardDrawAnimation()` with DOTween
5. Animates card from deck position to player hand position

### Position Calculation System
- **Real UI Positions**: Uses actual `DeckUI` and `PlayerUI` positions
- **Fallback System**: Canvas center calculations if UI components not found
- **Multi-Player Support**: Handles local vs remote player positioning
- **Canvas Aware**: Adapts to different canvas sizes and configurations

## Usage

### Basic Card Draw Animation
```csharp
// In CardManager.DrawCardFromDeck()
if (AnimationManager.Instance != null)
{
    AnimationManager.Instance.PlayCardDrawAnimation(
        cardNetwork.cardId.Value,
        playerController.ClientId
    );
}
```

### Setup Requirements
1. Create empty GameObject named "AnimationManager"
2. Attach `AnimationManager` script
3. Ensure `DeckUI` and `PlayerUI` components exist in scene
4. AnimationManager auto-initializes as singleton

## Migration History

### What Was Moved to AnimationManager
**From PlayerManager:**
- `TriggerCardAnimation_ClientRpc()`
- `TriggerCardAnimationWrapper()`
- `StartBasicCardAnimation()`
- `GetCanvasCenter()`
- `GetRealDeckPosition()`
- `GetRealPlayerHandPosition()`
- `FindPlayerControllerByClientId()`
- `FindCardNetworkByIdOnClient()`

**From CardManager:**
- `GetDeckPosition()`
- `CalculatePlayerHandPositionForCurrentClient()`
- `IsCardVisibleToCurrentClient()`
- `OnCardAnimationComplete()`

### Benefits Achieved
- **Reduced Code Duplication**: Animation logic exists in one place
- **Cleaner Managers**: PlayerManager and CardManager focus on their core responsibilities
- **Better Maintainability**: Easy to find and modify animation code
- **Improved Testing**: Can test animations independently

## Potential Improvements

### 1. Animation Events & Callbacks
```csharp
// Add event system for animation completion
public static event System.Action<int, ulong> OnCardDrawAnimationComplete;

// Usage
AnimationManager.OnCardDrawAnimationComplete += (cardId, playerId) => {
    // Handle animation completion
};
```

### 2. Animation Queue System
```csharp
// Queue multiple animations to prevent conflicts
public void QueueAnimation(AnimationData animationData)
{
    animationQueue.Add(animationData);
    if (!isAnimating) ProcessNextAnimation();
}
```

### 3. Animation State Management
```csharp
public enum AnimationState
{
    Idle,
    CardDraw,
    CardPlay,
    CardDiscard
}

public AnimationState CurrentState { get; private set; }
```

### 4. Configurable Animation Settings
```csharp
[System.Serializable]
public class AnimationSettings
{
    public float cardDrawDuration = 2.0f;
    public Ease cardDrawEase = Ease.OutQuart;
    public Vector3 cardDrawArcHeight = Vector3.up * 100f;
}
```

### 5. Animation Pooling
```csharp
// Pool CardUI objects for better performance
private Queue<CardUI> animationCardUIPool;

public CardUI GetPooledCardUI()
{
    return animationCardUIPool.Count > 0 
        ? animationCardUIPool.Dequeue() 
        : CreateNewCardUI();
}
```

### 6. Advanced Position Calculation
```csharp
// More sophisticated positioning system
public Vector3 CalculateOptimalHandPosition(PlayerController player, int cardIndex)
{
    // Consider hand size, screen resolution, UI scaling
    // Calculate arc positioning for multiple cards
    // Handle different player layouts (2-player, 4-player, etc.)
}
```

### 7. Animation Types Expansion
```csharp
public enum AnimationType
{
    CardDraw,
    CardPlay,
    CardDiscard,
    CardShuffle,
    PlayerTurn,
    ScoreUpdate
}

public void PlayAnimation(AnimationType type, AnimationData data)
{
    switch(type)
    {
        case AnimationType.CardDraw: PlayCardDrawAnimation(data); break;
        case AnimationType.CardPlay: PlayCardPlayAnimation(data); break;
        // ... more animation types
    }
}
```

### 8. Performance Optimizations
```csharp
// Batch animations for multiple cards
public void PlayBatchCardAnimation(List<CardAnimationData> cards)
{
    // Animate multiple cards simultaneously with slight delays
}

// LOD system for animations
public void SetAnimationQuality(AnimationQuality quality)
{
    // Adjust animation complexity based on performance
}
```

### 9. Audio Integration
```csharp
// Sync audio with animations
[System.Serializable]
public class AnimationAudioData
{
    public AudioClip startSound;
    public AudioClip endSound;
    public float audioDelay;
}
```

### 10. Animation Editor Tools
```csharp
#if UNITY_EDITOR
// Custom inspector for previewing animations
[CustomEditor(typeof(AnimationManager))]
public class AnimationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Preview Card Draw"))
        {
            // Preview animation in editor
        }
    }
}
#endif
```

## Best Practices

### When Adding New Animations
1. **Keep animations non-blocking**: Game logic should not wait for animations
2. **Use consistent naming**: Follow `Play[AnimationType]Animation()` pattern
3. **Add proper logging**: Use descriptive debug logs for troubleshooting
4. **Handle edge cases**: Always check for null references and missing components
5. **Consider multiplayer**: Ensure animations work correctly for all clients

### Performance Considerations
- **Limit concurrent animations**: Avoid too many simultaneous DOTween animations
- **Use object pooling**: Reuse CardUI objects for animations
- **Profile regularly**: Monitor animation performance impact
- **Fallback gracefully**: Game should work even if animations fail

## Troubleshooting

### Common Issues
- **Animations not playing**: Check AnimationManager singleton initialization
- **Wrong positions**: Verify DeckUI and PlayerUI components exist
- **Multiplayer sync issues**: Ensure ClientRpc is called from server
- **Performance drops**: Review concurrent animation count

### Debug Logs
All AnimationManager logs use prefix `[ANIMATIONMANAGER]` or `[ANIMATIONMANAGER-CLIENT]` for easy filtering.

## Future Considerations

As the game grows, consider:
- **Timeline Integration**: For complex cutscenes
- **Animator Controllers**: For state-based character animations  
- **Particle Systems**: For visual effects
- **UI Animation Framework**: Dedicated system for UI transitions
- **Mobile Optimization**: Reduced animation complexity for mobile devices