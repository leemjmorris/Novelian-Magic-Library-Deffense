using UnityEngine;
using Cysharp.Threading.Tasks;
using Firebase.Data;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// JML: 스테이지 진행도 관리 (Firebase 저장/로드)
    /// - 클리어한 스테이지 번호 저장
    /// - 스테이지 해금 여부 확인
    /// </summary>
    public class StageProgressManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "<color=#3EB489>[StageProgress]</color>";

        public static StageProgressManager Instance { get; private set; }

        private const int DEFAULT_UNLOCKED_STAGE = 1; // 1스테이지는 기본 해금

        private int highestClearedStage = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                // Firebase에서 로드하므로 여기서는 로드하지 않음
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Firebase 데이터로 진행도 설정 (BootScene에서 호출)
        /// </summary>
        public void SetProgressFromFirebase(ProgressionData progression)
        {
            if (progression != null)
            {
                highestClearedStage = progression.highestClearedStage;
                Debug.Log($"{LOG_PREFIX} Firebase에서 진행도 로드: 클리어한 최고 스테이지 = {highestClearedStage}");
            }
        }

        /// <summary>
        /// 진행도 저장 (Firebase)
        /// </summary>
        private void SaveProgress()
        {
            if (FirebaseSaveManager.Instance != null && FirebaseManager.Instance?.CurrentUserId != null)
            {
                var progression = new ProgressionData
                {
                    highestClearedStage = highestClearedStage,
                    playerLevel = FirebaseSaveManager.Instance.CachedData?.progression?.playerLevel ?? 1,
                    playerExp = FirebaseSaveManager.Instance.CachedData?.progression?.playerExp ?? 0
                };

                FirebaseSaveManager.Instance.SaveProgressionAsync(
                    FirebaseManager.Instance.CurrentUserId,
                    progression
                ).Forget();
            }
            Debug.Log($"{LOG_PREFIX} 진행도 저장: 클리어한 최고 스테이지 = {highestClearedStage}");
        }

        /// <summary>
        /// 스테이지 클리어 시 호출
        /// </summary>
        public void OnStageClear(int clearedStageNumber)
        {
            if (clearedStageNumber > highestClearedStage)
            {
                highestClearedStage = clearedStageNumber;
                SaveProgress();
                Debug.Log($"[StageProgressManager] 스테이지 {clearedStageNumber} 클리어! 다음 스테이지 해금됨");
            }
        }

        /// <summary>
        /// 특정 스테이지가 해금되었는지 확인
        /// </summary>
        public bool IsStageUnlocked(int stageNumber)
        {
            // 1스테이지는 항상 해금
            if (stageNumber <= DEFAULT_UNLOCKED_STAGE)
                return true;

            // 이전 스테이지를 클리어했으면 해금
            return stageNumber <= highestClearedStage + 1;
        }

        /// <summary>
        /// 클리어한 최고 스테이지 번호 반환
        /// </summary>
        public int GetHighestClearedStage()
        {
            return highestClearedStage;
        }

        /// <summary>
        /// 진행도 초기화 (디버그용)
        /// </summary>
        public void ResetProgress()
        {
            highestClearedStage = 0;
            SaveProgress();
            Debug.Log("[StageProgressManager] 진행도 초기화됨");
        }

        /// <summary>
        /// 특정 스테이지까지 해금 (디버그용)
        /// </summary>
        public void UnlockUpToStage(int stageNumber)
        {
            if (stageNumber > highestClearedStage)
            {
                highestClearedStage = stageNumber;
                SaveProgress();
                Debug.Log($"[StageProgressManager] 스테이지 {stageNumber}까지 해금됨 (디버그)");
            }
        }
    }
}
