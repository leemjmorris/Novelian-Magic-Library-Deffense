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

        // Issue #576 - 기능 해금 조건 상수
        private const int BOOKMARK_UNLOCK_STAGE = 2;      // 책갈피 제작/장착 해금 스테이지
        private const int BOSS_DUNGEON_UNLOCK_STAGE = 5;  // 도전던전 해금 스테이지

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
                var cachedProgression = FirebaseSaveManager.Instance.CachedData?.progression;
                var progression = new ProgressionData
                {
                    highestClearedStage = highestClearedStage,
                    playerLevel = cachedProgression?.playerLevel ?? 1,
                    playerExp = cachedProgression?.playerExp ?? 0,
                    bossDungeonProgress = cachedProgression?.bossDungeonProgress ?? 1,
                    totalKilledMonsters = cachedProgression?.totalKilledMonsters ?? 0,
                    stageRanks = cachedProgression?.stageRanks ?? new System.Collections.Generic.Dictionary<string, int>()
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

        #region Issue #645 - 스테이지 랭크 시스템

        /// <summary>
        /// 스테이지 클리어 랭크 저장 (더 높은 랭크일 때만 갱신)
        /// </summary>
        /// <param name="stageNumber">스테이지 번호</param>
        /// <param name="rankIndex">랭크 인덱스 (0=S, 1=A, 2=B, 3=F - 낮을수록 높은 랭크)</param>
        public void SaveStageRank(int stageNumber, int rankIndex)
        {
            if (FirebaseSaveManager.Instance?.CachedData?.progression == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Firebase 캐시 없음 - 랭크 저장 스킵");
                return;
            }

            var progression = FirebaseSaveManager.Instance.CachedData.progression;

            // stageRanks가 null이면 초기화
            if (progression.stageRanks == null)
            {
                progression.stageRanks = new System.Collections.Generic.Dictionary<string, int>();
            }

            string key = stageNumber.ToString();

            // 기존 랭크가 있으면 더 높은 랭크(낮은 인덱스)일 때만 갱신
            if (progression.stageRanks.TryGetValue(key, out int existingRank))
            {
                if (rankIndex < existingRank)
                {
                    progression.stageRanks[key] = rankIndex;
                    Debug.Log($"{LOG_PREFIX} 스테이지 {stageNumber} 랭크 갱신: {GetRankString(existingRank)} → {GetRankString(rankIndex)}");
                    SaveProgress();
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX} 스테이지 {stageNumber} 기존 랭크 {GetRankString(existingRank)}가 더 높음 (신규: {GetRankString(rankIndex)})");
                }
            }
            else
            {
                // 새로운 랭크 저장
                progression.stageRanks[key] = rankIndex;
                Debug.Log($"{LOG_PREFIX} 스테이지 {stageNumber} 첫 클리어 랭크: {GetRankString(rankIndex)}");
                SaveProgress();
            }
        }

        /// <summary>
        /// 스테이지 클리어 랭크 조회
        /// </summary>
        /// <param name="stageNumber">스테이지 번호</param>
        /// <returns>랭크 인덱스 (0=S, 1=A, 2=B, 3=F), 클리어 안 했으면 -1</returns>
        public int GetStageRank(int stageNumber)
        {
            if (FirebaseSaveManager.Instance?.CachedData?.progression?.stageRanks == null)
            {
                return -1;
            }

            string key = stageNumber.ToString();
            if (FirebaseSaveManager.Instance.CachedData.progression.stageRanks.TryGetValue(key, out int rankIndex))
            {
                return rankIndex;
            }

            return -1;
        }

        /// <summary>
        /// 랭크 인덱스를 문자열로 변환
        /// </summary>
        private string GetRankString(int rankIndex)
        {
            return rankIndex switch
            {
                0 => "S",
                1 => "A",
                2 => "B",
                3 => "F",
                _ => "-"
            };
        }

        #endregion

        #region Issue #576 - 기능 해금 시스템

        /// <summary>
        /// 책갈피 제작/장착 기능 해금 여부 확인
        /// </summary>
        public bool IsBookmarkUnlocked()
        {
            return highestClearedStage >= BOOKMARK_UNLOCK_STAGE;
        }

        /// <summary>
        /// 도전던전 기능 해금 여부 확인
        /// </summary>
        public bool IsBossDungeonUnlocked()
        {
            return highestClearedStage >= BOSS_DUNGEON_UNLOCK_STAGE;
        }

        /// <summary>
        /// 책갈피 해금에 필요한 스테이지 번호 반환
        /// </summary>
        public int GetBookmarkUnlockStage() => BOOKMARK_UNLOCK_STAGE;

        /// <summary>
        /// 도전던전 해금에 필요한 스테이지 번호 반환
        /// </summary>
        public int GetBossDungeonUnlockStage() => BOSS_DUNGEON_UNLOCK_STAGE;

        #endregion
    }
}
