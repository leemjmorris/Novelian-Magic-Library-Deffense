using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// JML: 스테이지 진행도 관리 (PlayerPrefs 저장/로드)
    /// - 클리어한 스테이지 번호 저장
    /// - 스테이지 해금 여부 확인
    /// </summary>
    public class StageProgressManager : MonoBehaviour
    {
        public static StageProgressManager Instance { get; private set; }

        private const string CLEARED_STAGE_KEY = "ClearedStageNumber";
        private const int DEFAULT_UNLOCKED_STAGE = 1; // 1스테이지는 기본 해금

        private int highestClearedStage = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadProgress();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// PlayerPrefs에서 진행도 로드
        /// </summary>
        private void LoadProgress()
        {
            highestClearedStage = PlayerPrefs.GetInt(CLEARED_STAGE_KEY, 0);
            Debug.Log($"[StageProgressManager] 진행도 로드: 클리어한 최고 스테이지 = {highestClearedStage}");
        }

        /// <summary>
        /// 진행도 저장
        /// </summary>
        private void SaveProgress()
        {
            PlayerPrefs.SetInt(CLEARED_STAGE_KEY, highestClearedStage);
            PlayerPrefs.Save();
            Debug.Log($"[StageProgressManager] 진행도 저장: 클리어한 최고 스테이지 = {highestClearedStage}");
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
