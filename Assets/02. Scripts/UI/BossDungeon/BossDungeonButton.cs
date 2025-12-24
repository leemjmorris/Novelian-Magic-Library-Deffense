using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NovelianMagicLibraryDefense.Managers;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #476 - 도전던전 스테이지 버튼
    /// StageButton을 기반으로 BossDungeonTable 연동
    /// </summary>
    public class BossDungeonButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image stageImage;
        [SerializeField] private TextMeshProUGUI stageNumberText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Button button;

        [Header("Color References")]
        [SerializeField] private Image starImage;        // 클리어(해금) 색상 참조용
        [SerializeField] private Image overlayImage;     // 잠금 색상 참조용

        [Header("Settings")]
        [SerializeField] private int floorIndex;         // 스테이지 번호 (1~100)

        [Header("PopUp Panel (Issue #511)")]
        [SerializeField] private BossDungeonPopUpPanel popUpPanel;

        private bool isLocked = true;

        // CSV에서 로드된 던전 데이터
        private BossDungeonData cachedDungeonData;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void Start()
        {
            SetFloorIndex(floorIndex);
            // Issue #476: 해금 상태는 BossDungeonScrollManager.InitializeDungeonButtons()에서 설정
        }

        /// <summary>
        /// 잠금 상태 설정
        /// </summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;

            if (lockOverlay != null)
                lockOverlay.SetActive(locked);

            if (lockIcon != null)
                lockIcon.SetActive(locked);

            if (stageImage != null)
            {
                if (locked && overlayImage != null)
                    stageImage.color = overlayImage.color;
                else if (!locked && starImage != null)
                    stageImage.color = starImage.color;
            }

            if (button != null)
                button.interactable = !locked;
        }

        public bool IsLocked() => isLocked;
        public int GetFloorIndex() => floorIndex;

        /// <summary>
        /// 스테이지 번호 설정
        /// </summary>
        public void SetFloorIndex(int index)
        {
            floorIndex = index;
            if (stageNumberText != null)
                stageNumberText.text = index.ToString();
        }

        /// <summary>
        /// 버튼 클릭 시 호출 - CSV에서 던전 데이터 로드
        /// </summary>
        public void OnDungeonButtonClicked()
        {
            if (CSVLoader.Instance == null)
            {
                Debug.LogError("[BossDungeonButton] CSVLoader가 초기화되지 않음");
                return;
            }

            var table = CSVLoader.Instance.GetTable<BossDungeonData>();
            if (table == null)
            {
                Debug.LogError("[BossDungeonButton] BossDungeonTable이 로드되지 않음");
                return;
            }

            // Floor_Index로 던전 데이터 찾기
            cachedDungeonData = table.Find(d => d.Floor_Index == floorIndex);

            if (cachedDungeonData == null)
            {
                Debug.LogError($"[BossDungeonButton] Floor {floorIndex}에 해당하는 던전을 찾을 수 없음");
                return;
            }

            // 구현되지 않은 스테이지 (11~100) 체크
            if (!cachedDungeonData.IsImplemented)
            {
                WarningUIManager.Instance.ShowWarning("준비 중인 스테이지입니다");
                return;
            }

            // SelectedBossDungeon에 저장 (씬 전환 후에도 유지)
            SelectedBossDungeon.Data = cachedDungeonData;

            Debug.Log($"[BossDungeonButton] ===== 던전 선택 완료 =====");
            Debug.Log($"[BossDungeonButton] Dungeon_ID={cachedDungeonData.Dungeon_ID}, " +
                      $"Floor={cachedDungeonData.Floor_Index}, Boss_ID={cachedDungeonData.Boss_ID}, " +
                      $"Time_Limit={cachedDungeonData.Time_Limit}초");

            // Issue #511: 씬 직접 전환 대신 확인 패널 표시
            if (popUpPanel != null)
            {
                popUpPanel.Show();
                Debug.Log("[BossDungeonButton] 확인 패널 표시");
            }
            else
            {
                Debug.LogWarning("[BossDungeonButton] popUpPanel이 연결되지 않음! Inspector에서 연결해주세요.");
            }
        }

        /// <summary>
        /// 캐시된 던전 데이터 반환
        /// </summary>
        public BossDungeonData GetDungeonData() => cachedDungeonData;
    }
}
