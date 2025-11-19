using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class BookMarkCraft : MonoBehaviour
{
    [Header("Craft Button")]
    [SerializeField] private Button CraftButton;

    private void OnclickCraftButton()
    {
        Debug.Log("BookMark Craft Button Clicked");
    }
}
