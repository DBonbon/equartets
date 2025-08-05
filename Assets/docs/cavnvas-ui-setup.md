I understand your Unity project structure and the challenge you're facing. Let me recap to ensure I have it right:

**Current Situation:**
- 2D multiplayer card game with a mostly empty scene canvas (just start/server buttons)
- Network elements come from prefabs that instantiate under root (not canvas) since canvas isn't a network object
- This creates multiple canvases (scene + one per prefab), causing animation issues

**Goal:**
- Move UI elements from prefabs to the scene canvas for positioning only (not parenting)
- Keep prefab structure unchanged - if a prefab has PlayerUI attached, it stays attached
- Use existing scene managers (DeckManager, QuartetsManager, PlayerManager) as intermediaries to pass canvas references since prefabs can't have scene references
- Start with DeckUI and QuartetsUI, then move to PlayerUI if successful
- No changes to other scripts/functionalities

**Key Constraints:**
1. Prefab structure remains intact
2. UI components stay attached to their original prefabs
3. Scene canvas reference passed through manager scripts
4. Only positioning changes, not parent-child relationships

