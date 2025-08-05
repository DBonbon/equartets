[System.Serializable]
public class PlayerData
{
    public string playerName;
    //public int score; // Assuming you want to keep this field based on your JSON
    public int playerDbId;
    public string playerImagePath;

    // Constructor not necessary but could be useful for initializing new instances
    public PlayerData(string name, int dbId, string imagePath, int score = 0)
    {
        playerName = name;
        playerDbId = dbId;
        playerImagePath = imagePath;
        //this.score = score; // Assign the score if using
    }
}
