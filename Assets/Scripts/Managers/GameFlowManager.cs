using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;


public class GameFlowManager : MonoBehaviour
{
    //[SerializeField] private TurnManager turnManager;
    
    //[SerializeField] private Canvas setupCanvas;
    //[SerializeField] private Canvas gameCanvas;
    //[SerializeField] private Canvas endGameCanvas;
    //[SerializeField] private float canvasDisplayDuration = 4f;

    //private bool isPlayerDataLoaded = false;
    //private bool isCardDataLoaded = false;

    //private List<Canvas> canvasesInOrder = new List<Canvas>();

    private void Awake()
    {
        //DataManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
        //DataManager.OnCardDataLoaded += OnCardDataLoaded;

        // Add the canvases to the list in your desired order
        //canvasesInOrder.Add(setupCanvas);
        //canvasesInOrder.Add(gameCanvas);
        //canvasesInOrder.Add(endGameCanvas);
    }

    private void OnDestroy()
    {
        //DataManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        //DataManager.OnCardDataLoaded -= OnCardDataLoaded;
    }

    private void Start()
    {
        // Start the game flow when the scene starts
        // You can also trigger it from a button click or another event as needed.
        //StartGameFlow();
    }

    /*
    private void OnPlayerDataLoaded(List<PlayerData> playerDataList)
    {
        if (!isPlayerDataLoaded)
        {
            PlayerManager.Instance.GenerateGamePlayers(playerDataList);
            isPlayerDataLoaded = true;

            if (isCardDataLoaded)
            {
                // Start the game flow when both player and card data are loaded
                //StartGameFlow();
            }
        }
    }
*/
    /*
    private void OnCardDataLoaded(List<CardData> cardDataList)
    {
        if (!isCardDataLoaded)
        {
            /*CardManager.Instance.InitializeCards(cardDataList);
            DataManager.OnCardDataLoaded -= OnCardDataLoaded;
            isCardDataLoaded = true;

            if (isPlayerDataLoaded)
            {
                // Start the game flow when both player and card data are loaded
                //StartGameFlow();
            }
        }
    }*/

    /*
    private void StartGameFlow()
    {
        // Deactivate all canvases initially
        DeactivateAllCanvases();

        // Activate the setupCanvas initially
        ActivateCanvas(setupCanvas);
    }

    // The public method to handle the button click event for starting the game
    public void OnSetupButtonClicked()
    {
        // Transition to the main game flow
        //DisplayEndGameResults();   
        MainGameFlow();
    }

    private void MainGameFlow()
    {
        // Deactivate all canvases
        DeactivateAllCanvases();

        // Activate the gameCanvas
        ActivateCanvas(gameCanvas);

        // Initialize the PlayerToAsk list for each player
        foreach (Player player in playerManager.Players)
        {
            player.SetPlayerToAsk(playerManager.Players.Where(p => p != player).ToList());
            PlayerController playerController = player.GetComponent<PlayerController>();
            playerController.SetPlayerData(player);
        }
            StartTurnManager();
    }

    private void InitializePlayerToAskList()
    {
        // Initialize the PlayerToAsk list for each player
        foreach (Player player in playerManager.Players)
        {
            //player.PlayerToAsk();
        }
    }

    private void StartTurnManager()
    {
        if (turnManager != null)
        {
            turnManager.StartTurnManager();
            Debug.Log($"{Time.time}: TurnManager started successfully.");
        }
        else
        {
            Debug.LogError($"{Time.time}: TurnManager reference is not set in the Inspector.");
        }
    }
    

    // Method to activate a specific canvas
    private void ActivateCanvas(Canvas canvas)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    // Method to deactivate all canvases
    private void DeactivateAllCanvases()
    {
        foreach (Canvas canvas in canvasesInOrder)
        {
            canvas.gameObject.SetActive(false);
        }
    }
    
    public void DisplayEndGameResults()
    {
        // Deactivate all canvases except the end game results canvas
        DeactivateAllCanvases();
        ActivateCanvas(endGameCanvas);

        endGameManager.StartEndGameManager();
    }*/

}
