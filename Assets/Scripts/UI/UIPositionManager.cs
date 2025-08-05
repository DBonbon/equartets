using UnityEngine;

public static class UIPositionManager
{
    public static readonly Vector2 BOTTOM_POSITION = new Vector2(0.5f, 0.15f);  // Bottom center
    private static readonly Vector2 RIGHT_POSITION = new Vector2(0.85f, 0.5f);   // Right center
    private static readonly Vector2 TOP_POSITION = new Vector2(0.5f, 0.85f);     // Top center
    private static readonly Vector2 LEFT_POSITION = new Vector2(0.15f, 0.5f);    // Left center

    /// <summary>
    /// Get the UI position for a remote player by order index (0 = right, 1 = top, 2 = left)
    /// </summary>
    public static Vector2 GetRemotePositionByIndex(int remoteIndex, int totalRemotePlayers)
    {
        if (totalRemotePlayers == 1)
        {
            return TOP_POSITION;
        }

        switch (remoteIndex)
        {
            case 0: return RIGHT_POSITION;
            case 1: return TOP_POSITION;
            case 2: return LEFT_POSITION;
            default:
                Debug.LogWarning($"[UIPositionManager] Invalid remote player index {remoteIndex} for {totalRemotePlayers} remotes");
                return TOP_POSITION; // fallback
        }
    }


    public static Vector2 GetLocalPlayerPosition()
    {
        return BOTTOM_POSITION;
    }

    public static UIVisibilitySettings GetUIVisibility(bool isLocalPlayer)
    {
        return new UIVisibilitySettings
        {
            ShowPersonalInfo = true,
            ShowTurnUI = true,
            ShowPlayerHand = isLocalPlayer
        };
    }
}

public struct UIVisibilitySettings
{
    public bool ShowPersonalInfo;
    public bool ShowTurnUI;
    public bool ShowPlayerHand;
}
