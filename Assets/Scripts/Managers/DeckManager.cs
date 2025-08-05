// DeckManager.cs - Added deckReference for UI elements
using Unity.Netcode;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;
    public GameObject deckPrefab;
    
    [Header("Canvas Positioning")]
    [SerializeField] private Canvas sceneCanvas; // Main scene canvas
    [SerializeField] private Transform deckReference; // Where deck UI elements (bgimage, bgdeck) go
    [SerializeField] private Transform cardContainer; // Where CardUI objects go (separate)
   
    public GameObject DeckInstance { get; private set; }
    public Deck CurrentDeck { get; private set; }
   
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
   
    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnDeckPrefab;
    }
   
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnDeckPrefab;
        }
    }
   
    private void SpawnDeckPrefab()
    {
        if (NetworkManager.Singleton.IsServer && deckPrefab != null)
        {
            DeckInstance = Instantiate(deckPrefab);
            CurrentDeck = DeckInstance.GetComponent<Deck>();
            
            DeckInstance.GetComponent<NetworkObject>().Spawn();
        }
    }
    
    // Called when deck spawns - set up both references
    public void OnDeckSpawned(Deck deck)
    {
        if (deck != null)
        {
            DeckUI deckUI = deck.GetComponent<DeckUI>();
            if (deckUI != null)
            {
                // Pass deck reference for UI elements (bgimage, bgdeck)
                Transform deckTarget = deckReference != null ? deckReference : sceneCanvas.transform;
                
                // Pass card container for CardUI objects (separate)
                Transform cardTarget = cardContainer != null ? cardContainer : sceneCanvas.transform;
                
                // Set both references in DeckUI
                deckUI.SetCanvasReferences(deckTarget, cardTarget);
                
                Debug.Log($"DeckUI references set - Deck: {deckTarget.name}, Cards: {cardTarget.name}");
            }
        }
    }
    
    // Utility methods
    public Canvas GetSceneCanvas() => sceneCanvas;
    public Transform GetDeckReference() => deckReference != null ? deckReference : sceneCanvas.transform;
    public Transform GetCardContainer() => cardContainer != null ? cardContainer : sceneCanvas.transform;
}

