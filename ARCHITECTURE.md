# Architecture Overview

This document describes the high-level architecture of the **Equartets** Unity project.

## Layers and Responsibilities

```mermaid
graph TD
    A[Abstracts Layer] --> B[Controllers Layer]
    B --> C[Managers Layer]
    C --> D[UI Layer]
    C --> E[Network Layer]
    E --> C
    D --> C
    subgraph Abstracts
        A1[CardData]
        A2[CardState]
        A3[Deck]
        A4[PlayerData]
        A5[PlayerState]
        A6[Quartets]
    end
    subgraph Controllers
        B1[CardController]
        B2[PlayerController0]
    end
    subgraph Managers
        C1[AnimationManager]
        C2[CardManager]
        C3[DataManager]
        C4[DeckManager]
        C5[GameFlowManager]
        C6[NetworkManagerUI]
        C7[PlayerManager]
        C8[QuartetsManager]
        C9[TurnManager]
    end
    subgraph Network
        E1[CardNetwork]
        E2[ConnectionApprovalHandler]
        E3[MatchmakerClient]
        E4[NetworkVariableIntWrapper]
        E5[PlayerInstance]
        E6[PlayerNetwork]
        E7[ServerStartUp]
        E8[TargetFPS]
    end
    subgraph UI
        D1[CardAnimationManager]
        D2[CardUI]
        D3[DeckUI]
        D4[PlayerUI]
        D5[QuartetUI]
        D6[UIPositionManager]
    end
    subgraph Utility
        U1[DOTweenTest]
        U2[TextFormatter]
        U3[Initialization]
    end
```

## Description of Layers

- **Abstracts** → Core game state definitions (cards, deck, players, quartets).
- **Controllers** → Handle direct interactions between data models and managers.
- **Managers** → Coordinate game systems (deck handling, turn flow, animations, etc.).
- **Network** → Multiplayer connectivity using Netcode for GameObjects.
- **UI** → Visual representation and interaction handling for cards, decks, players.
- **Utility** → Helper scripts, formatting tools, and testing utilities.
