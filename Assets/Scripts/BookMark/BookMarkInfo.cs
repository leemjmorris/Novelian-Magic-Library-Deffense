using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookMarkInfo : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;

    [SerializeField] private Image bookMarkIcon;
    [SerializeField] private TextMeshProUGUI bookMarkNameText;
    [SerializeField] private TextMeshProUGUI bookMarkDescriptionText;
    public void CloseInfoPanel()
    {
        infoPanel.SetActive(false);
    }

    public void OpenInfoPanel(Sprite icon, string name, string description)
    {
        // JML: null 체크 추가
        if (bookMarkIcon != null && icon != null)
            bookMarkIcon.sprite = icon;
        if (bookMarkNameText != null)
            bookMarkNameText.text = name;
        if (bookMarkDescriptionText != null)
            bookMarkDescriptionText.text = description;
        infoPanel.SetActive(true);
    }
}
