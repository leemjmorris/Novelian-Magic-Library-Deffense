using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터에 책갈피를 장착하기 위한 패널
/// 보유한 책갈피 목록을 표시하고, 클릭 시 LibraryBookMarkInfoPanel을 통해 장착/해제
/// </summary>
public class BookmarkEquipPanel : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotContainer;

    [Header("Panel References")]
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private GameObject libraryBookMarkInfoPanel;

    [Header("Manager")]
    [SerializeField] private BookMarkManager bmManager;

    // 생성된 슬롯 리스트 (정리용)
    private List<CraftSceneBookMarkSlot> slotList = new List<CraftSceneBookMarkSlot>();

    private void OnEnable()
    {
        CreateBookmarkSlots();
    }

    private void OnDisable()
    {
        ClearSlots();
    }

    /// <summary>
    /// 보유한 책갈피 슬롯 생성
    /// </summary>
    private void CreateBookmarkSlots()
    {
        // 기존 슬롯 정리
        ClearSlots();

        // BookMarkManager에서 모든 책갈피 가져오기
        List<BookMark> allBookmarks = BookMarkManager.Instance.GetAllBookmarks();

        for (int i = 0; i < allBookmarks.Count; i++)
        {
            BookMark bookMark = allBookmarks[i];

            GameObject slot = Instantiate(slotPrefab, slotContainer);
            CraftSceneBookMarkSlot slotComponent = slot.GetComponent<CraftSceneBookMarkSlot>();

            if (slotComponent != null)
            {
                slotList.Add(slotComponent);

                // 아이콘 키 결정 (BookMarkUI.cs와 동일한 방식)
                string categoryKey = GetCategoryIconKey(bookMark.Type);
                string bookmarkKey = GetBookmarkIconKey(bookMark);

                // Init 호출 - LibraryBookMarkInfoPanel 전달
                slotComponent.Init(
                    bookMark,
                    categoryKey,
                    bookmarkKey,
                    null,  // choicePanel은 사용하지 않음
                    libraryBookMarkInfoPanel
                ).Forget();
            }
        }

        Debug.Log($"[BookmarkEquipPanel] 책갈피 슬롯 {allBookmarks.Count}개 생성 완료");
    }

    /// <summary>
    /// 슬롯 정리
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in slotList)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        slotList.Clear();

        // slotContainer의 자식도 정리
        if (slotContainer != null)
        {
            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 카테고리 아이콘 키 반환 (BookMarkUI.cs에서 복사)
    /// </summary>
    private string GetCategoryIconKey(BookmarkType type)
    {
        return type switch
        {
            BookmarkType.Stat => "PictoIcon_Buff",
            BookmarkType.Skill => "PictoIcon_Battle",
            _ => "PictoIcon_Attack"
        };
    }

    /// <summary>
    /// 책갈피 아이콘 키 반환 (BookMarkUI.cs에서 복사)
    /// </summary>
    private string GetBookmarkIconKey(BookMark bookMark)
    {
        return bookMark.Type switch
        {
            BookmarkType.Stat => "PictoIcon_Attack",
            BookmarkType.Skill => "PictoIcon_Attack",
            _ => "PictoIcon_Attack"
        };
    }

    /// <summary>
    /// 슬롯 새로고침 - 장착/해제 후 호출
    /// </summary>
    public void RefreshSlots()
    {
        CreateBookmarkSlots();
    }

    /// <summary>
    /// 특정 책갈피에 해당하는 슬롯 찾기
    /// </summary>
    public CraftSceneBookMarkSlot FindSlotByBookmark(BookMark bookmark)
    {
        if (bookmark == null) return null;

        foreach (var slot in slotList)
        {
            if (slot != null && slot.BookMarkData == bookmark)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        if (characterInfoPanel != null)
        {
            characterInfoPanel.SetActive(true);
        }
    }
}
