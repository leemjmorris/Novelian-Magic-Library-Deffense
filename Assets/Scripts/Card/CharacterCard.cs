using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 클릭 처리
/// </summary>
[RequireComponent(typeof(Button))]
public class CharacterCard : MonoBehaviour
{
    public Image characterImage;
    public TextMeshProUGUI characterNameText;
    public GenreType genreType;

    private Button button;
    private CardSelectionManager manager;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void Start()
    {
        manager = GameObject.FindWithTag("Manager").GetComponent<CardSelectionManager>();
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
        if (manager != null)
        {
            manager.OnCardSelected();
        }
    }
}
