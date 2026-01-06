using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #476 - 도전던전 스크롤 뷰 관리
    /// 100개 스테이지 버튼 관리 및 해금 상태 처리
    /// </summary>
    public class BossDungeonScrollManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private List<BossDungeonButton> dungeonButtons = new List<BossDungeonButton>();

        [Header("Settings")]
        [SerializeField] private int unlockedFloorCount = 1; // 해금된 스테이지 수

        private void Start()
        {
            LoadUnlockedFloorCount();
            InitializeDungeonButtons();
            ScrollToCurrentFloor();
        }

        /// <summary>
        /// 서버/로컬에서 해금된 스테이지 수 로드
        /// </summary>
        private void LoadUnlockedFloorCount()
        {
            if (FirebaseSaveManager.Instance?.CachedData?.progression != null)
            {
                unlockedFloorCount = FirebaseSaveManager.Instance.CachedData.progression.bossDungeonProgress;
                GameLog.Log($"[BossDungeonScrollManager] 해금 상태 로드: {unlockedFloorCount}층");
            }
            else
            {
                unlockedFloorCount = 1; // 기본값
                GameLog.Log("[BossDungeonScrollManager] Firebase 데이터 없음, 기본값(1층) 사용");
            }
        }

        /// <summary>
        /// 모든 던전 버튼 초기화
        /// </summary>
        private void InitializeDungeonButtons()
        {
            for (int i = 0; i < dungeonButtons.Count; i++)
            {
                if (dungeonButtons[i] != null)
                {
                    int floorIndex = i + 1;
                    dungeonButtons[i].SetFloorIndex(floorIndex);

                    // 해금된 스테이지까지만 잠금 해제
                    bool isLocked = floorIndex > unlockedFloorCount;
                    dungeonButtons[i].SetLocked(isLocked);
                }
            }
        }

        /// <summary>
        /// 현재 진행 중인 스테이지로 스크롤
        /// </summary>
        public void ScrollToCurrentFloor()
        {
            if (scrollRect == null || dungeonButtons.Count == 0)
                return;

            float normalizedPosition = (float)(unlockedFloorCount - 1) / (dungeonButtons.Count - 1);
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            // 스크롤 위치 설정 (1 = 상단, 0 = 하단)
            scrollRect.verticalNormalizedPosition = 1f - normalizedPosition;
        }

        /// <summary>
        /// 특정 스테이지 해금
        /// </summary>
        public void UnlockFloor(int floorIndex)
        {
            if (floorIndex > unlockedFloorCount)
            {
                unlockedFloorCount = floorIndex;
                InitializeDungeonButtons();

                // TODO: Firebase에 저장
                SaveUnlockedFloorCount();
            }
        }

        /// <summary>
        /// 해금된 스테이지 수 설정
        /// </summary>
        public void SetUnlockedFloorCount(int count)
        {
            unlockedFloorCount = Mathf.Max(1, count);
            InitializeDungeonButtons();
        }

        /// <summary>
        /// 해금된 스테이지 수 반환
        /// </summary>
        public int GetUnlockedFloorCount() => unlockedFloorCount;

        /// <summary>
        /// 해금 상태 저장
        /// </summary>
        private void SaveUnlockedFloorCount()
        {
            if (FirebaseSaveManager.Instance?.CachedData?.progression != null &&
                FirebaseManager.Instance != null)
            {
                FirebaseSaveManager.Instance.CachedData.progression.bossDungeonProgress = unlockedFloorCount;
                FirebaseSaveManager.Instance.SaveProgressionAsync(
                    FirebaseManager.Instance.CurrentUserId,
                    FirebaseSaveManager.Instance.CachedData.progression
                ).Forget();
                GameLog.Log($"[BossDungeonScrollManager] 해금 상태 저장: {unlockedFloorCount}층");
            }
        }
    }
}
