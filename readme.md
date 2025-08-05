# 🃏 Multiplayer Quartets Card Game (Unity C#)

A robust 2–4 player **multiplayer online card game** implemented in Unity using **Netcode for GameObjects (NGO)**. The game replicates the "quartets" gameplay style, featuring server-authoritative logic, secure data handling, dynamic player room setup, and clean separation of concerns for maintainability.

---

## 🚀 Features

* 🎮 **Multiplayer Support**: 2–4 players via Unity NGO
* 📡 **Server-Authoritative Logic**: Full gameplay decisions are made server-side
* 🃏 **Card Distribution & Turn Logic**: Dynamic and rule-based
* 📋 **JSON-Driven Content**: Load card/player data from backend
* 🧠 **Modular System**: Managers for cards, players, turns, and deck
* 🎨 **Dynamic UI Positioning**: Adapts to number and orientation of players
* 🔁 **Pooled Card UI**: Efficient rendering and animation readiness
* 🧱 **Clean Architecture**: Five-layer player object model for separation of concerns
* 📦 **Build Targets**: Windows, Linux (dedicated), WebGL

---

## 🏗️ Architecture

### Core Systems:

### Dual Object Model:

* **CardPrefab** (networked game logic)
* **CardUI** (client-only visual representation)

### 5-Layer Player Architecture:

* `PlayerData.cs`: Loads from JSON
* `PlayerState.cs`: Holds runtime state
* `PlayerController.cs`: Core logic
* `PlayerNetwork.cs`: Network sync & RPCs
* `PlayerUI.cs`: Interface rendering

📄 See also:
- [ARCHITECTURE.md](./ARCHITECTURE.md) – full breakdown of layers and responsibilities
- [DATAFLOW.md](./DATAFLOW.md) – sequence diagrams of game logic
- [VISUAL_DATAFLOW.md](./VISUAL_DATAFLOW.md) – high-level visual data flow chart

---

## 🔄 Game Flow

### Initialization

1. `DataManager` loads card and player data
2. `CardManager` spawns networked cards and UI pool
3. `PlayerManager` connects and initializes players

### Gameplay Loop

1. Cards are distributed via `CardManager`
2. `TurnManager` handles turn order, guesses, rule enforcement
3. Cards move across hands with full sync via `NetworkVariables` & `ClientRpc`
4. Quartets detected, scores updated, game continues

### UI Logic

* Player UIs repositioned based on local player perspective
* Network sync keeps scores and hands updated in real time

---

## 🖼️ Snapshots

Screenshots of the game in action can be found in the [`./snapshot`](./snapshot) folder:

![Game State 1](./snapshot/screenshot1.png)
![Game State 2](./snapshot/screenshot2.png)

---

## 🛠️ Technologies Used

* **Unity 2022+**
* **Netcode for GameObjects (NGO)**
* **C#**
* **StreamingAssets (JSON)**
* **WebGL / Windows / Linux builds**

Future compatible with:

* 🧩 **Unity Lobby + Relay**
* ☁️ **Photon Fusion / GameLift**
* 🎙️ **Vivox** for voice integration

---

## 📦 Build Targets

* ✅ WebGL (browser)
* ✅ Windows (client/server)
* ✅ Linux (dedicated server)

---

## 📂 Project Structure Highlights

* `CardManager.cs`: Handles card spawning, UI pool, distribution
* `PlayerManager.cs`: Controls player lifecycles and connection
* `TurnManager.cs`: Orchestrates turn logic and game progression
* `DeckManager.cs`: Manages card deck as GameObject singleton
* `GameFlowManager.cs`: High-level game phase control
* `QuartetsManager.cs`: Quartet detection and score logic

---

## ✨ Why This Project Stands Out

* ✅ Fully server-authoritative for secure multiplayer
* ✅ Clean modular structure for easy extension
* ✅ Supports both visual and logic layer customization
* ✅ Architecture ready for production networking
* ✅ Designed for human-scale social gameplay and rapid iteration

---

## 📍 Future Directions

* 🎥 Add card and score animations
* 🌐 Add online matchmaking with Unity Relay or Photon
* 🧩 Integrate backend for account tracking and stats
* 📊 Game analytics and session logs

---

## 📜 License

Proprietary. All rights reserved. For inquiries, please contact the author.
