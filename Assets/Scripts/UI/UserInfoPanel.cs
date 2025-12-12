using UnityEngine;
using UnityEngine.UI;

public class UserInfoPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panel1;
    [SerializeField] private GameObject panel2;

    public void OnCloseButton()
    {
        if (panel2 && panel2.activeSelf)
        {
            panel2.SetActive(false);
        }
        else if (panel1 && panel1.activeSelf)
        {
            panel1.SetActive(false);
        }
    }

    public void ShowPanel2()
    {
        if (panel2)
        {
            panel2.SetActive(true);
        }
    }
    
}