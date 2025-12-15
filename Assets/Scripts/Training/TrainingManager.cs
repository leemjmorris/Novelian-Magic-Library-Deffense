// 훈련소 매니저 (Issue #458)
// 훈련소 전체 로직 제어
namespace Novelian.Training
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using Novelian.Combat;
    using NovelianMagicLibraryDefense.Managers;

    /// <summary>
    /// 훈련소 전체 제어
    /// - 캐릭터 스폰/설정
    /// - 허수아비 스폰/관리
    /// - 시작/정지/리셋 제어
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Spawn Points")]
        [SerializeField, Tooltip("캐릭터 스폰 위치")]
        private Transform characterSpawnPoint;

        [SerializeField, Tooltip("허수아비 스폰 위치들")]
        private Transform[] dummySpawnPoints;

        [Header("Prefabs")]
        [SerializeField, Tooltip("허수아비 프리팹 (임시 Capsule)")]
        private GameObject dummyPrefab;

        [Header("References")]
        [SerializeField]
        private DPSCalculator dpsCalculator;

        #endregion

        #region Private Fields

        // 상태
        private bool isRunning = false;
        private Character currentCharacter;
        private List<DummyTarget> activeDummies = new List<DummyTarget>();

        // 설정 값
        private int selectedCharacterId = 0;
        private int selectedGrade = 1;
        private int selectedEnhancement = 0;
        private int selectedMainSkillBookmark = 0;
        private int selectedSupportSkillBookmark = 0;
        private int selectedStatBookmark = 0;
        private int dummyCount = 1;

        // 캐릭터 프리팹 핸들
        private AsyncOperationHandle<GameObject> characterHandle;

        #endregion

        #region Events

        public event System.Action OnTrainingStarted;
        public event System.Action OnTrainingStopped;
        public event System.Action OnTrainingReset;

        #endregion

        #region Properties

        public bool IsRunning => isRunning;
        public int DummyCount => dummyCount;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // DPSCalculator가 없으면 자동 생성
            if (dpsCalculator == null)
            {
                dpsCalculator = gameObject.AddComponent<DPSCalculator>();
            }
        }

        private void OnDestroy()
        {
            // 캐릭터 프리팹 핸들 해제
            if (characterHandle.IsValid())
            {
                Addressables.Release(characterHandle);
            }
        }

        #endregion

        #region Public API - Settings

        /// <summary>
        /// 캐릭터 선택
        /// </summary>
        public void SetCharacter(int characterId)
        {
            selectedCharacterId = characterId;
            Debug.Log($"[TrainingManager] 캐릭터 설정: {characterId}");
        }

        /// <summary>
        /// 등급(성급) 선택
        /// </summary>
        public void SetGrade(int grade)
        {
            selectedGrade = Mathf.Clamp(grade, 1, 3);
            Debug.Log($"[TrainingManager] 등급 설정: {selectedGrade}성");
        }

        /// <summary>
        /// 강화 단계 선택
        /// </summary>
        public void SetEnhancement(int level)
        {
            selectedEnhancement = Mathf.Clamp(level, 0, 10);
            Debug.Log($"[TrainingManager] 강화 단계 설정: {selectedEnhancement}");
        }

        /// <summary>
        /// 메인 스킬 책갈피 선택
        /// </summary>
        public void SetMainSkillBookmark(int bookmarkId)
        {
            selectedMainSkillBookmark = bookmarkId;
            Debug.Log($"[TrainingManager] 메인 스킬 책갈피 설정: {bookmarkId}");
        }

        /// <summary>
        /// 보조 스킬 책갈피 선택
        /// </summary>
        public void SetSupportSkillBookmark(int bookmarkId)
        {
            selectedSupportSkillBookmark = bookmarkId;
            Debug.Log($"[TrainingManager] 보조 스킬 책갈피 설정: {bookmarkId}");
        }

        /// <summary>
        /// 스탯 책갈피 선택
        /// </summary>
        public void SetStatBookmark(int bookmarkId)
        {
            selectedStatBookmark = bookmarkId;
            Debug.Log($"[TrainingManager] 스탯 책갈피 설정: {bookmarkId}");
        }

        /// <summary>
        /// 허수아비 수량 설정
        /// </summary>
        public void SetDummyCount(int count)
        {
            dummyCount = Mathf.Clamp(count, 1, 10);
            Debug.Log($"[TrainingManager] 허수아비 수량 설정: {dummyCount}");

            // 실행 중이면 허수아비 재스폰
            if (isRunning)
            {
                SpawnDummies();
            }
        }

        /// <summary>
        /// 허수아비 수량 증가
        /// </summary>
        public void IncreaseDummyCount()
        {
            SetDummyCount(dummyCount + 1);
        }

        /// <summary>
        /// 허수아비 수량 감소
        /// </summary>
        public void DecreaseDummyCount()
        {
            SetDummyCount(dummyCount - 1);
        }

        #endregion

        #region Public API - Control

        /// <summary>
        /// 훈련 시작
        /// </summary>
        public async void StartTraining()
        {
            if (isRunning) return;

            Debug.Log("[TrainingManager] 훈련 시작");
            isRunning = true;

            // 캐릭터 스폰
            await SpawnCharacterAsync();

            // 허수아비 스폰
            SpawnDummies();

            // DPS 측정 시작
            dpsCalculator.StartMeasurement();

            OnTrainingStarted?.Invoke();
        }

        /// <summary>
        /// 훈련 정지 (일시정지, 설정 변경 가능)
        /// </summary>
        public void StopTraining()
        {
            if (!isRunning) return;

            Debug.Log("[TrainingManager] 훈련 정지");
            isRunning = false;

            // 캐릭터 비활성화
            if (currentCharacter != null)
            {
                currentCharacter.SetAutoAttackEnabled(false);
                currentCharacter.gameObject.SetActive(false);
            }

            // 허수아비 비활성화
            DespawnDummies();

            // DPS 측정 일시정지
            dpsCalculator.PauseMeasurement();

            OnTrainingStopped?.Invoke();
        }

        /// <summary>
        /// 훈련 리셋 (모든 데이터 초기화)
        /// </summary>
        public void ResetTraining()
        {
            Debug.Log("[TrainingManager] 훈련 리셋");

            // DPS 측정 리셋
            dpsCalculator.Reset();

            // 실행 중이면 타이머 재시작
            if (isRunning)
            {
                dpsCalculator.StartMeasurement();
            }

            OnTrainingReset?.Invoke();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 캐릭터 스폰 (Addressables 사용)
        /// </summary>
        private async UniTask SpawnCharacterAsync()
        {
            // 기존 캐릭터 제거
            if (currentCharacter != null)
            {
                Destroy(currentCharacter.gameObject);
                currentCharacter = null;
            }

            if (selectedCharacterId <= 0)
            {
                Debug.LogWarning("[TrainingManager] 캐릭터가 선택되지 않음");
                return;
            }

            // CSV에서 캐릭터 데이터 로드
            var characterData = CSVLoader.Instance?.GetData<CharacterData>(selectedCharacterId);
            if (characterData == null)
            {
                Debug.LogError($"[TrainingManager] CharacterData를 찾을 수 없음: {selectedCharacterId}");
                return;
            }

            // Addressables에서 프리팹 로드
            string prefabPath = characterData.Path_ID.ToString();
            try
            {
                // 이전 핸들 해제
                if (characterHandle.IsValid())
                {
                    Addressables.Release(characterHandle);
                }

                characterHandle = Addressables.LoadAssetAsync<GameObject>(prefabPath);
                await characterHandle.ToUniTask();

                if (characterHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[TrainingManager] 캐릭터 프리팹 로드 실패: {prefabPath}");
                    return;
                }

                // 스폰
                Vector3 spawnPos = characterSpawnPoint != null ? characterSpawnPoint.position : Vector3.zero;
                GameObject charObj = Instantiate(characterHandle.Result, spawnPos, Quaternion.identity);
                currentCharacter = charObj.GetComponent<Character>();

                if (currentCharacter != null)
                {
                    // 자동 공격 비활성화 (Initialize 전에)
                    currentCharacter.SetAutoAttackEnabled(false);

                    // 캐릭터 초기화
                    currentCharacter.Initialize(selectedCharacterId);

                    // TODO: 성급, 강화, 책갈피 적용
                    // currentCharacter.SetGrade(selectedGrade);
                    // currentCharacter.SetEnhancement(selectedEnhancement);
                    // ...

                    // 자동 공격 활성화
                    currentCharacter.SetAutoAttackEnabled(true);

                    Debug.Log($"[TrainingManager] 캐릭터 스폰 완료: {characterData.Character_Name_ID}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TrainingManager] 캐릭터 스폰 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 허수아비 스폰
        /// </summary>
        private void SpawnDummies()
        {
            // 기존 허수아비 제거
            DespawnDummies();

            if (dummyPrefab == null)
            {
                Debug.LogWarning("[TrainingManager] 허수아비 프리팹이 설정되지 않음");
                return;
            }

            // 스폰 위치 계산
            for (int i = 0; i < dummyCount; i++)
            {
                Vector3 spawnPos;
                if (dummySpawnPoints != null && i < dummySpawnPoints.Length && dummySpawnPoints[i] != null)
                {
                    spawnPos = dummySpawnPoints[i].position;
                }
                else
                {
                    // 기본 위치: 캐릭터 앞 5m, 가로로 1m 간격
                    float xOffset = (i - (dummyCount - 1) / 2f) * 1f;
                    spawnPos = (characterSpawnPoint != null ? characterSpawnPoint.position : Vector3.zero)
                               + new Vector3(xOffset, 0f, 5f);
                }

                GameObject dummyObj = Instantiate(dummyPrefab, spawnPos, Quaternion.identity);
                DummyTarget dummy = dummyObj.GetComponent<DummyTarget>();
                if (dummy == null)
                {
                    dummy = dummyObj.AddComponent<DummyTarget>();
                }
                activeDummies.Add(dummy);
            }

            Debug.Log($"[TrainingManager] 허수아비 {dummyCount}마리 스폰 완료");
        }

        /// <summary>
        /// 허수아비 제거
        /// </summary>
        private void DespawnDummies()
        {
            for (int i = 0; i < activeDummies.Count; i++)
            {
                if (activeDummies[i] != null)
                {
                    Destroy(activeDummies[i].gameObject);
                }
            }
            activeDummies.Clear();
        }

        #endregion

        #region Getters for UI

        public DPSCalculator GetDPSCalculator() => dpsCalculator;

        #endregion
    }
}
