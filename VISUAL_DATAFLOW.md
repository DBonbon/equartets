# Visual Data Flow Diagram

This diagram shows the high-level data movement in **Equartets**, from player actions to game state updates and UI changes.

```mermaid
flowchart LR
    subgraph Players
        AP[Active Player]
        TP[Target Player]
    end

    subgraph UI
        CardUI
        PlayerUI
        QuartetUI
    end

    subgraph Managers
        TM[TurnManager]
        GM[GameFlowManager]
        DM[DeckManager]
        CM[CardManager]
        QM[QuartetsManager]
    end

    subgraph Network
        NL[Network Layer]
    end

    %% Flow from player input
    AP -->|Request card| TM
    TM --> NL
    NL --> TP
    TP -->|Respond with card/deny| TM

    %% Update game state
    TM --> CM
    CM --> QM
    DM --> GM

    %% UI Updates
    QM -->|Quartet formed| QuartetUI
    CM -->|Card moved| CardUI
    GM --> PlayerUI

    %% Animation trigger
    CardUI -->|Play animation| UI
    QuartetUI -->|Celebrate| UI
```

**Legend:**
- **Players**: Local or remote participants
- **UI**: Game visuals and player interaction elements
- **Managers**: Orchestrators of game logic and state
- **Network Layer**: Handles communication between host and clients
