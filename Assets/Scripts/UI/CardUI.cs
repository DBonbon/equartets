using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardUI : MonoBehaviour
{
    public string CardName;
    public int cardId;
   
    [SerializeField] private TextMeshProUGUI suitText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Image iconImage;
    [SerializeField] private List<TextMeshProUGUI> siblingTexts; // Only use this for all 4 names
    [SerializeField] private GameObject cardFront;
    [SerializeField] private GameObject cardBack;
   
    private Sprite[] cardIcons;
   
    private void Start()
    {
        SetFaceUp(false); // Optional: start all cards face down
    }
   
    public void UpdateCardUIWithCardData(CardData cardData)
    {
        this.cardId = cardData.cardId;
        CardName = cardData.cardName;
        string matchingSiblingName = cardData.cardName;
        UpdateUI(cardData.cardName, cardData.suit, cardData.hint, cardData.siblings, matchingSiblingName, cardData.cardImage);
    }
   
    public void SetFaceUp(bool faceUp)
    {
        if (cardFront != null && cardBack != null)
        {
            cardFront.SetActive(faceUp);
            cardBack.SetActive(!faceUp);
        }
    }
   
    public void UpdateUI(string cardName, string suit, string hint, List<string> siblings, string matchingSiblingName, string iconFileName)
    {
        // Format text for display using TextFormatter
        suitText.text = TextFormatter.FormatSuitName(suit);
        hintText.text = TextFormatter.FormatHint(hint);
       
        // Load the sprite dynamically
        if (iconImage != null && !string.IsNullOrEmpty(iconFileName))
        {
            Sprite loadedIcon = Resources.Load<Sprite>("Icons/" + System.IO.Path.GetFileNameWithoutExtension(iconFileName));
            if (loadedIcon != null)
            {
                iconImage.sprite = loadedIcon;
            }
            else
            {
                Debug.LogWarning($"Missing icon: {iconFileName}, using fallback.");
                iconImage.sprite = null;
            }
        }
       
        // Only use siblingTexts array - this handles all 4 quartet members
        for (int i = 0; i < siblingTexts.Count; i++)
        {
            if (i < siblings.Count)
            {
                siblingTexts[i].gameObject.SetActive(true);
                
                // Format sibling names for display
                string formattedSiblingName = TextFormatter.FormatCardName(siblings[i]);
                siblingTexts[i].text = formattedSiblingName;
                
                // Set red color for the current card name, black for others
                // Compare using original (unformatted) names for accuracy
                siblingTexts[i].color = siblings[i] == matchingSiblingName ? Color.red : Color.black;
               
                // Debug to verify correct assignment
                Debug.Log($"siblingTexts[{i}] ({siblingTexts[i].gameObject.name}) = '{formattedSiblingName}' (original: '{siblings[i]}') at position {siblingTexts[i].transform.localPosition}");
            }
            else
            {
                siblingTexts[i].gameObject.SetActive(false);
            }
        }
    }
   
    public void ResetUI()
    {
        suitText.text = "";
        hintText.text = "";
        iconImage.sprite = null;
       
        foreach (var siblingText in siblingTexts)
        {
            siblingText.gameObject.SetActive(false);
            siblingText.text = "";
            siblingText.color = Color.black;
        }
    }
}