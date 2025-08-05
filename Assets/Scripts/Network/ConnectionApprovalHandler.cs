using UnityEngine;

/// <Summary>
/// approval process for new connecting players
/// currently we are just making sure the amount of players does not exceed the MaxPlayers count
/// here you can do all sorts of pre-preprocessing such as selecting different prefabs to spawn
/// with depending on certain conditionss
/// https://docs-multiplayer.unity3d.com/netcode/current/basics/connection-approval/
/// </Summary>
public class ConnectionApprovalHandler : MonoBehaviour
{
    
    public static int MaxPlayers = 4;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start dummy statement");
    }

    // Update is called once per frame

}
