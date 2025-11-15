using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 클릭 처리
/// </summary>
[RequireComponent(typeof(Button))]
public class CharacterCard : MonoBehaviour
{
    [Header("UI References")]
    public Image characterImage;
    public TextMeshProUGUI characterNameText;

    [Header("Manager Reference")]
    [SerializeField] private CardSelectionManager manager;

    [Header("Card Type")]
    public int cardIndex; // 0 = Card1, 1 = Card2

    public GenreType genreType;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        // Remove listener to avoid duplicate calls
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        // Validate manager reference
        if (manager == null)
        {
            Debug.LogError("[CharacterCard] CardSelectionManager가 할당되지 않았습니다! Inspector에서 할당해주세요.");
        }

        // Auto-find characterImage if not assigned
        if (characterImage == null)
        {
            characterImage = GetComponentInChildren<Image>();
        }
    }

    /// <summary>
    /// characterCard info update
    /// </summary>
    public void UpdateCharacter(Sprite sprite, string characterName, GenreType genre)
    {
        if (characterImage != null)
        {
            characterImage.sprite = sprite;
        }

        if (characterNameText != null)
        {
            characterNameText.text = characterName;
        }

        genreType = genre;
    }

    void OnClick()
    {
        // Debug.Log($"[CharacterCard] OnClick - cardIndex: {cardIndex}, manager null? {manager == null}");

        if (manager == null)
        {
            Debug.LogError("[CharacterCard] CardSelectionManager가 null입니다!");
            return;
        }

        // Call appropriate method based on cardIndex
        if (cardIndex == 0)
        {
            // Debug.Log("[CharacterCard] Calling OnCard1Selected()");
            manager.OnCard1Selected();
        }
        else if (cardIndex == 1)
        {
            // Debug.Log("[CharacterCard] Calling OnCard2Selected()");
            manager.OnCard2Selected();
        }
        else
        {
            // Debug.LogWarning($"[CharacterCard] Invalid cardIndex: {cardIndex}. Falling back to OnCardSelected()");
            manager.OnCardSelected();
        }
    }
}
