using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookmarkEquipPanel : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotContainer;

    [Header("Panel References")]
    [SerializeField] private GameObject InfoPanel;

    [SerializeField] BookMarkManager bmManager;

    private void Start()
    {

        for (int i = 0; i < 30; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotContainer);
        }
    }


    
    public void ClosePanel()
    {
        this.gameObject.SetActive(false);
        InfoPanel.SetActive(true);
    }
}
