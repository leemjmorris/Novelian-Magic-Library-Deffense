using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Character Info Tabs")]
    [SerializeField] private Button story1Button;
    [SerializeField] private Button story2Button;
    [SerializeField] private Button story3Button;

    [Header("Bookmark slot Buttons")]
    [SerializeField] private Button bookmarkSlot1Button;
    [SerializeField] private Button bookmarkSlot2Button;
    [SerializeField] private Button bookmarkSlot3Button;
    [SerializeField] private Button bookmarkSlot4Button;
    [SerializeField] private Button bookmarkSlot5Button;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;

    [Header("Character Name Text")]
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Character Level Text")]
    [SerializeField] private TextMeshProUGUI characterLevelText;

    [Header("Character EXP Text")]
    [SerializeField] private TextMeshProUGUI characterExpText;

    [Header("Character EXP Slider")]
    [SerializeField] private Slider characterExpSlider;

    [Header("Character Sprite")]
    [SerializeField] private Image characterSprite;


    public void InitInfo(int characterID)
    {

    }

    public void StoryButtonClicked()
    {

    }

    public void ShowPanel()
    {
        panel.SetActive(true);
    }

    public void HidePanel()
    {
        panel.SetActive(false);
    }
}
