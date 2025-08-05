# Data Flow Overview

This document describes how data moves through the **Equartets** Unity project, both locally and across the network.

## 1. Game Start Sequence

```mermaid
sequenceDiagram
    participant Host
    participant Client
    participant NetworkManagerUI
    participant GameFlowManager
    participant DeckManager
    participant PlayerManager

    Host->>NetworkManagerUI: Initialize host session
    NetworkManagerUI->>GameFlowManager: Signal game start
    GameFlowManager->>DeckManager: Create and shuffle deck
    DeckManager->>PlayerManager: Distribute cards to players
    PlayerManager-->>Client: Send initial hand via network sync
```

---

## 2. Turn Flow (Card Request & Response)

```mermaid
sequenceDiagram
    participant ActivePlayer
    participant TargetPlayer
    participant TurnManager
    participant NetworkLayer
    participant UI

    ActivePlayer->>TurnManager: Request card from TargetPlayer
    TurnManager->>NetworkLayer: Send request event
    NetworkLayer-->>TargetPlayer: Deliver request
    TargetPlayer->>TurnManager: Return card if owned
    TurnManager->>UI: Update card movements
    UI->>AnimationManager: Play move animation
```

---

## 3. Quartet Completion

```mermaid
sequenceDiagram
    participant Player
    participant CardManager
    participant QuartetsManager
    participant UI

    Player->>CardManager: Place final card of quartet
    CardManager->>QuartetsManager: Validate quartet
    QuartetsManager->>UI: Update quartet display
    UI->>AnimationManager: Play "quartet complete" animation
```

---

## Key Principles

- **Managers** handle system-level orchestration (Deck, Game Flow, Turn logic).
- **Network Layer** ensures all game state changes propagate between host and clients.
- **UI Layer** listens to state changes and triggers animations or visual updates.
- **Abstracts** define the immutable data models for cards, decks, and players.

