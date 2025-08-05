// QuartetManager.cs - Same pattern as DeckManager
using Unity.Netcode;
using UnityEngine;

public class QuartetManager : MonoBehaviour
{
    public static QuartetManager Instance;
    public GameObject quartetPrefab;
    
    [Header("Canvas Positioning")]
    [SerializeField] private Canvas sceneCanvas; // Main scene canvas
    [SerializeField] private Transform quartetReference; // Where quartet UI elements go
    [SerializeField] private Transform cardContainer; // Where CardUI objects go (separate)
   
    public GameObject QuartetInstance { get; private set; }
   
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
        NetworkManager.Singleton.OnServerStarted += SpawnQuartetPrefab;
    }
   
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnQuartetPrefab;
        }
    }
   
    private void SpawnQuartetPrefab()
    {
        if (NetworkManager.Singleton.IsServer && quartetPrefab != null)
        {
            QuartetInstance = Instantiate(quartetPrefab);
            
            QuartetInstance.GetComponent<NetworkObject>().Spawn();
            Debug.Log("Quartets prefab spawned on server start.");
        }
    }
    
    // Called when quartet spawns - set up both references
    public void OnQuartetSpawned(Quartets quartet)
    {
        if (quartet != null)
        {
            QuartetUI quartetUI = quartet.GetComponent<QuartetUI>();
            if (quartetUI != null)
            {
                // Pass quartet reference for UI elements
                Transform quartetTarget = quartetReference != null ? quartetReference : sceneCanvas.transform;
                
                // Pass card container for CardUI objects (separate)
                Transform cardTarget = cardContainer != null ? cardContainer : sceneCanvas.transform;
                
                // Set both references in QuartetUI
                quartetUI.SetCanvasReferences(quartetTarget, cardTarget);
                
                Debug.Log($"QuartetUI references set - Quartet: {quartetTarget.name}, Cards: {cardTarget.name}");
            }
        }
    }
    
    // Utility methods
    public Canvas GetSceneCanvas() => sceneCanvas;
    public Transform GetQuartetReference() => quartetReference != null ? quartetReference : sceneCanvas.transform;
    public Transform GetCardContainer() => cardContainer != null ? cardContainer : sceneCanvas.transform;
}
