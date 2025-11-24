using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LibraryCharacterSlot : MonoBehaviour
{
    private int characterID;
    public int CharacterID => characterID;
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private TextMeshProUGUI characterExp;
    [SerializeField] private TextMeshProUGUI characterLevel;
    [SerializeField] private Slider characterExpBar;
    [SerializeField] private Image characterSprite;
    [SerializeField] private Button characterInfoButton;

    private CharacterInfoPanel infoPanel;

    private void Start()
    {
        characterInfoButton.onClick.AddListener(OnClickCharacterInfo);


    }

    private void OnDestroy()
    {
        characterInfoButton.onClick.RemoveListener(OnClickCharacterInfo);
    }

    private void OnClickCharacterInfo()
    {
        Debug.Log($"Character Info Clicked for ID: {characterID}");
        infoPanel.InitInfo(characterID);
        infoPanel.ShowPanel();
    }

    public void SetInfoPanelObj(GameObject panel)
    {
        infoPanel = panel.GetComponent<CharacterInfoPanel>();
    }

    public void InitSlot(int id, string name, int level, int exp, int maxExp, Sprite sprite)
    {
        characterID = id;
        characterName.text = name;
        characterLevel.text = $"Lv. {level}";
        characterExp.text = $"{exp} / {maxExp} EXP";
        characterSprite.sprite = sprite;

        // Assuming max EXP for level up is 1000 for demonstration
        characterExpBar.maxValue = maxExp;
        characterExpBar.value = exp;
    }

    public void UpdateUI(int exp, int maxExp)
    {
        characterExp.text = $"{exp} / {maxExp}";
        characterExpBar.maxValue = maxExp;
        characterExpBar.value = exp;
    }
}
