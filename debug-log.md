# Debug logger

# Debug Log Entry

**Date:** 2025-06-12  
**Action:** Tagging the first functional gameplay version.

## Summary
- Created Git tag `v0.1-functional-gameplay`.
- Purpose: mark the first version where the game logic and UI are functional and allow full game flow.
- The version includes basic player instantiation, card distribution, turn flow, and simple UI.

## Reasoning
- Establish a stable reference point before beginning large refactoring.
- Allows safe rollback to a known working state.

# Debug Log Entry

**Date:** 2025-06-12  
**Action:** Document practice for resetting project to a clean state.

## Summary
- Identified that `git reset --hard` alone is not sufficient for full Unity project reset.
- It is necessary to delete the `Library/` folder after reset to force Unity to reimport all assets and ensure no stale cached data affects the runtime.

## Reasoning
- Avoids problems where broken references or cached data persist after a Git reset.
- Ensures scenes, prefabs, and scripts are reloaded consistently from Git-tracked sources.

## Managfers scripts

## Action
Reviewed TurnManager.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
TurnManager is the Turn Flow Controller. Existing methods are core and must be kept. Initial debug comment block was removed. 

## Action
Reviewed QuartetManager.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
QuartetManager spawns Quartet prefab on server start and maintains reference. Structure is clean. One commented Debug.Log — can be removed for cleaner code. No other changes required.

## Action
Cleaned PlayerManager.cs: removed commented and unused methods. Kept final GetPlayerNameByClientId with simplified code.

## Date
2025-06-11

## Notes
Removed two commented GetPlayerNameByClientId versions, commented AssignTurnToPlayer, CleanupPlayers, and GenerateGamePlayers methods. Kept dictionary-based GetPlayerNameByClientId and cleaned it for current usage.

## Action
Reviewed NetworkManagerUI.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
NetworkManagerUI provides simple UI to start Host/Client/Server sessions and handles disconnect UI. No Debug.Log present. No changes required at this step.

## Action
Reviewed DeckManager.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
DeckManager manages deck UI instantiation in the scene. Code is clean. No Debug.Log present. Minor possible improvement: make deckInstance private with public getter — not critical.

## Action
Reviewed DataManager.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
DataManager is responsible for loading card data from Resources and broadcasting it via OnCardDataLoaded event. Structure is clean. One Debug.Log ("Card data loaded successfully.") is useful — can optionally be tagged.

## Action
Reviewed CardManager.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
CardManager is responsible for card instantiation (network object), UI pooling, and card registry. Hooks into DataManager.OnCardDataLoaded. No Debug.Log present — no cleanup required at this step.

## Network scripts:

## Action
Reviewed ConnectionApprovalHandler.cs: placeholder script, no implemented logic.

## Date
2025-06-11

## Notes
ConnectionApprovalHandler was intended to handle connection approval but currently contains no logic. Dummy Start() method and Update() were removed. Preferred action: delete the script for now — re-implement when needed.

## Action
Reviewed MatchmakerClient.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
MatchmakerClient handles Unity Matchmaker sign-in flow. Hooks ServerStartUp event to trigger sign-in. Uses ParrelSync for local testing. Structure is clean. No changes required at this step.


## Action
Reviewed NetworkVariableIntWrapper.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
NetworkVariableIntWrapper provides custom network-serializable integer. Implements INetworkSerializable correctly. Class is very clean. No changes required.


## Action
Reviewed ServerStartUp.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
ServerStartUp is intended as dedicated server bootstrapper for Unity Services (Matchmaker / Multiplay). Parses command line args to detect server mode. Structure is correct. No changes required at this step.

## Action
Reviewed TargetFPS.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
TargetFPS limits frame rate to configured target. Simple utility script. Structure is very clean. No changes required at this step.


## Action
Reviewed CardData.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
CardData is the core card data model, used for JSON loading. Contains fields and sibling population logic. 


## Action
Reviewed Card.cs: confirmed role and usage.

## Date
2025-06-11

## Notes
Card is the core networked card entity. Implements IComparable and NetworkBehaviour correctly. Fields and serialization are clean and efficient. No changes required.



# Debug Log Entry

**Date:** 2025-06-12  
**Action:** Reviewed UI scripts and confirmed safe and correct usage.

## Summary
- Reviewed and confirmed roles of the following UI scripts:
    - `CardUI`: Renders a card's name, suit, and ID in the UI.
    - `DeckUI`: Displays the deck visually in the UI; not deeply used in current game loop but clean.
    - `PlayerUI`: Displays player name and indicates turn ownership.
    - `QuartetUI`: Displays the cards of a formed quartet in the UI.
    - `QuartetsUI`: Displays multiple quartets per player (potential replacement / better design going forward).

## Reasoning
- Ensures clarity of the roles of each UI script.
- Provides clean base to proceed with next steps (refactoring, safe migration of `Quartet` → `Quartets`, or new UI improvements).





**Date:** 2025-06-12  
**Action:** Successfully recovered functional project after failed reset and mixed state.

## Summary
- Identified that adding/removing scripts improperly caused project instability.
- Restored functional gameplay version successfully after learning proper Unity+Git workflow regarding script add/remove and prefab references.
- Confirmed that the `QuartetManager` and `Quartet.cs` setup is restored and the game functions as intended.

## Reasoning
- Critical learning point: managing script lifecycle (add / remove / rename) in Unity must be done with awareness of prefab and scene references.
- Re-establishes a clean base for further controlled development and refactoring.


