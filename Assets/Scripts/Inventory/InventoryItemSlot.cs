using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using TMPro;

/// <summary>
/// 인벤토리 UI의 개별 아이템 슬롯을 관리하는 컴포넌트
/// 와이어프레임의 각 그리드 슬롯에 해당
/// </summary>
public class InventoryItemSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{   
    // 모든 슬롯이 공유하는 아이콘 캐시
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> IconCache
        = new System.Collections.Generic.Dictionary<string, Sprite>();
        
    [Header("UI References")]
    [SerializeField] private Image itemIconImage;           // 아이템 아이콘 이미지
    [SerializeField] private TextMeshProUGUI countText;     // 아이템 수량 텍스트 (X 999)

    [Header("Long Press Settings")]
    [SerializeField] private float longPressDuration = 0.5f; // 길게 누르기 감지 시간 (초)

    // 현재 슬롯에 표시된 아이템 정보
    private InventoryItemInfo itemInfo;
    private int slotIndex; // 같은 아이템이 여러 슬롯에 걸쳐있을 때의 인덱스

    // 길게 누르기 감지 관련
    private bool isPointerDown = false;
    private float pointerDownTimer = 0f;
    private bool hasTriggeredLongPress = false;

    // 콜백 이벤트
    public System.Action<InventoryItemInfo> OnItemLongPressed;

    private void Update()
    {
        // 길게 누르기 감지
        if (isPointerDown && !hasTriggeredLongPress)
        {
            pointerDownTimer += Time.deltaTime;

            if (pointerDownTimer >= longPressDuration)
            {
                hasTriggeredLongPress = true;
                TriggerLongPress();
            }
        }
    }

    /// <summary>
    /// 슬롯에 아이템 정보 설정 및 UI 업데이트
    /// </summary>
    /// <param name="info">표시할 아이템 정보</param>
    /// <param name="slotIdx">슬롯 인덱스 (같은 아이템이 여러 슬롯에 걸칠 때)</param>
    public void SetItem(InventoryItemInfo info, int slotIdx = 0)
    {
        itemInfo = info;
        slotIndex = slotIdx;

        if (info == null)
        {
            ClearSlot();
            return;
        }

        UpdateUI().Forget();
    }

    /// <summary>
    /// UI 업데이트: 아이콘, 수량, 등급 배경색 등
    /// </summary>
    private async UniTaskVoid UpdateUI()
    {
        if (itemInfo == null) return;

        // 아이템 아이콘 설정
        if (itemIconImage != null)
        {
            itemIconImage.enabled = true;
            string iconKey = itemInfo.IconPath;

            // 아이콘 로드 
            if (!string.IsNullOrEmpty(iconKey))
            {
            // 1) 캐시에 있는지 먼저 확인
            if (IconCache.TryGetValue(iconKey, out var cachedSprite) && cachedSprite != null)
            {
                itemIconImage.sprite = cachedSprite;
            }
            else
            {
                try
                {
                    // 2) 없으면 한 번만 로드해서 캐시에 저장
                    var handle = Addressables.LoadAssetAsync<Sprite>(iconKey);
                    Sprite icon = await handle.Task;

                    if (icon != null)
                    {
                        IconCache[iconKey] = icon;

                        // await 동안 다른 아이템으로 바뀌었을 수 있으니 방어
                        if (itemInfo != null && itemInfo.IconPath == iconKey)
                        {
                            itemIconImage.sprite = icon;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[InventoryItemSlot] 아이콘 로드 실패: {iconKey}\n{e.Message}");
                }
            }
        }

        
        }

        // 수량 텍스트 설정
        if (countText != null)
        {
            int displayCount = itemInfo.GetCountForSlot(slotIndex);
            if (displayCount > 0)
            {
                countText.text = $"X {displayCount}";
                countText.enabled = true;
            }
            else
            {
                countText.text = "";
                countText.enabled = false;
            }
        }

      
    }

    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void ClearSlot()
    {
        itemInfo = null;
        slotIndex = 0;

        if (itemIconImage != null)
            itemIconImage.enabled = false;

        if (countText != null)
            countText.enabled = false;

     
    }

    /// <summary>
    /// 등급에 따른 배경색 반환
    /// </summary>
    private Color GetGradeColor(Grade grade)
    {
        return grade switch
        {
            Grade.Common => new Color(0.8f, 0.8f, 0.8f, 0.3f),      // 회색
            Grade.Rare => new Color(0.3f, 0.6f, 1f, 0.3f),          // 파란색
            Grade.Unique => new Color(0.8f, 0.4f, 1f, 0.3f),        // 보라색
            Grade.Legendary => new Color(1f, 0.6f, 0.2f, 0.3f),     // 주황색
            Grade.Mythic => new Color(1f, 0.2f, 0.2f, 0.3f),        // 빨간색
            _ => Color.clear
        };
    }

    #region Pointer Events (길게 누르기 감지)

    public void OnPointerDown(PointerEventData eventData)
    {
        if (itemInfo == null) return;

        isPointerDown = true;
        pointerDownTimer = 0f;
        hasTriggeredLongPress = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetPointerState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPointerState();
    }

    private void ResetPointerState()
    {
        isPointerDown = false;
        pointerDownTimer = 0f;
        hasTriggeredLongPress = false;
    }

    /// <summary>
    /// 길게 누르기 이벤트 발생
    /// </summary>
    private void TriggerLongPress()
    {
        if (itemInfo != null)
        {
            Debug.Log($"[InventoryItemSlot] 아이템 길게 누름: {itemInfo.ItemName}");
            OnItemLongPressed?.Invoke(itemInfo);
        }
    }

    #endregion

    /// <summary>
    /// 현재 슬롯이 비어있는지 확인
    /// </summary>
    public bool IsEmpty()
    {
        return itemInfo == null;
    }

    /// <summary>
    /// 현재 슬롯의 아이템 정보 반환
    /// </summary>
    public InventoryItemInfo GetItemInfo()
    {
        return itemInfo;
    }
}
