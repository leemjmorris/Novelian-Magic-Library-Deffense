using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Buff card click handler (for testing)
/// </summary>
[RequireComponent(typeof(Button))]
public class BuffCard : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cardPanel; // Card panel to deactivate
    public TextMeshProUGUI statText; // Stat text (e.g., "Attack +1")
    public Image buffIcon; // Buff icon image

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// Called when buff card is clicked
    /// </summary>
    void OnClick()
    {
        // Test: Deactivate card panel
        if (cardPanel != null)
        {
            Debug.Log($"[BuffCard] Card panel deactivated: {cardPanel.name}");
            cardPanel.SetActive(false);
        }

        // Test: Log current stat text
        if (statText != null)
        {
            Debug.Log($"[BuffCard] Stat text: {statText.text}");
        }
        else
        {
            Debug.LogWarning("[BuffCard] statText is not assigned!");
        }

        // TODO: Add actual buff application logic
        // ApplyBuff();
    }

    /// <summary>
    /// Update buff card information
    /// </summary>
    public void UpdateBuffCard(Sprite icon, string buffText)
    {
        if (buffIcon != null && icon != null)
        {
            buffIcon.sprite = icon;
        }

        if (statText != null && !string.IsNullOrEmpty(buffText))
        {
            statText.text = buffText;
        }
    }

    // Actual buff application logic (to be implemented)
    // private void ApplyBuff()
    // {
    //     // Increase player stats, etc.
    // }
}
