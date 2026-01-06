using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine.AddressableAssets;

namespace NovelianMagicLibraryDefense.Lobby
{
    /// <summary>
    /// 로비 동반자 캐릭터 스폰/관리 싱글톤
    /// - DeckManager에서 현재 프리셋 4명 가져오기
    /// - 카메라 뷰포트 기준 랜덤 위치에 스폰
    /// - Character 전투 컴포넌트 비활성화
    /// </summary>
    public class LobbyCompanionManager : MonoBehaviour
    {
        private static LobbyCompanionManager instance;
        public static LobbyCompanionManager Instance => instance;

        [Header("스폰 설정")]
        [SerializeField] private float spawnY = 0f; // 바닥 높이

        [Header("이동 영역 제한")]
        [SerializeField, Tooltip("이동 가능한 영역을 나타내는 BoxCollider (Is Trigger 체크 필수!)")]
        private BoxCollider movementArea; // Scene View에서 BoxCollider 크기 조정으로 영역 설정

        [Header("카메라")]
        [SerializeField] private Camera mainCamera;

        // 계산된 이동 범위 (BoxCollider Bounds에서 자동 계산)
        private float minX, maxX, minZ, maxZ;

        // 캐릭터 프리팹 캐시 (CharacterPlacementManager와 동일한 방식)
        private Dictionary<string, GameObject> loadedCharacterPrefabs = new Dictionary<string, GameObject>();
        private bool isPreloadComplete = false;

        // 스폰된 동반자 목록
        private List<GameObject> spawnedCompanions = new List<GameObject>();

        private async void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // 카메라 자동 찾기
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // BoxCollider에서 이동 범위 계산
            CalculateMovementBounds();

            // 캐릭터 프리팹 프리로드
            await PreloadCharacterPrefabs();
        }

        private async void Start()
        {
            // DeckManager 초기화 대기
            await UniTask.WaitUntil(() => DeckManager.Instance != null);
            await UniTask.Delay(500); // 추가 대기 (안정성)

            // 프리셋 변경 이벤트 구독
            DeckManager.Instance.OnPresetChanged += OnPresetChanged;

            // 캐릭터 프리팹 프리로드 완료 대기
            await UniTask.WaitUntil(() => isPreloadComplete);

            // 동반자 스폰
            SpawnCompanions();
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnPresetChanged -= OnPresetChanged;
            }
        }

        /// <summary>
        /// 프리셋 변경 시 호출 - 동반자 재스폰
        /// </summary>
        private void OnPresetChanged(int newPresetIndex)
        {
            GameLog.Log($"<color=cyan>[LobbyCompanionManager]</color> 프리셋 변경 감지: {newPresetIndex + 1}번 프리셋");
            RefreshCompanions();
        }

        /// <summary>
        /// 동반자 새로고침 (기존 제거 + 새 프리셋으로 재스폰)
        /// </summary>
        public void RefreshCompanions()
        {
            ClearCompanions();
            SpawnCompanions();
        }

        /// <summary>
        /// 현재 덱 프리셋 4명을 로비에 스폰
        /// </summary>
        private void SpawnCompanions()
        {
            if (DeckManager.Instance == null)
            {
                GameLog.LogError("[LobbyCompanionManager] DeckManager.Instance가 null입니다!");
                return;
            }

            if (!isPreloadComplete)
            {
                GameLog.LogError("[LobbyCompanionManager] 캐릭터 프리팹 프리로드가 완료되지 않았습니다!");
                return;
            }

            // 현재 프리셋의 덱 가져오기
            List<int> deck = DeckManager.Instance.GetDeck();
            if (deck == null || deck.Count == 0)
            {
                GameLog.LogWarning("[LobbyCompanionManager] 덱이 비어있습니다.");
                return;
            }

            GameLog.Log($"<color=cyan>[LobbyCompanionManager]</color> 동반자 스폰 시작: {deck.Count}명");

            // 각 캐릭터 스폰
            for (int i = 0; i < deck.Count; i++)
            {
                int characterId = deck[i];
                if (characterId <= 0) continue; // 빈 슬롯 건너뛰기

                SpawnCompanion(characterId);
            }

            GameLog.Log($"<color=cyan>[LobbyCompanionManager]</color> 동반자 스폰 완료: {spawnedCompanions.Count}명");
        }

        /// <summary>
        /// 개별 동반자 스폰
        /// </summary>
        private void SpawnCompanion(int characterId)
        {
            // 캐릭터별 프리팹 가져오기
            string prefabKey = GetCharacterPrefabKey(characterId);
            if (string.IsNullOrEmpty(prefabKey) || !loadedCharacterPrefabs.ContainsKey(prefabKey))
            {
                GameLog.LogError($"[LobbyCompanionManager] 캐릭터 프리팹을 찾을 수 없습니다: CharacterID {characterId}, PrefabKey {prefabKey}");
                return;
            }

            GameObject characterPrefab = loadedCharacterPrefabs[prefabKey];

            // 랜덤 스폰 위치 생성 (카메라 뷰포트 기준)
            Vector3 spawnPosition = GetRandomSpawnPosition();

            // 캐릭터 프리팹 생성
            GameObject companion = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);
            companion.name = $"LobbyCompanion_{characterId}";

            // Character 전투 컴포넌트 비활성화
            DisableCombatComponents(companion);

            // LobbyCompanion 컴포넌트 추가 및 초기화
            LobbyCompanion companionScript = companion.GetComponent<LobbyCompanion>();
            if (companionScript == null)
            {
                companionScript = companion.AddComponent<LobbyCompanion>();
            }
            companionScript.Initialize(characterId);

            spawnedCompanions.Add(companion);

            GameLog.Log($"<color=cyan>[LobbyCompanionManager]</color> 동반자 스폰: CharacterID {characterId} at {spawnPosition}");
        }

        /// <summary>
        /// 지정된 영역 내 랜덤 스폰 위치 생성 (바닥 고정)
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            // 지정된 범위 내에서 랜덤 위치 생성
            float randomX = Random.Range(minX, maxX);
            float randomZ = Random.Range(minZ, maxZ);

            // 최종 스폰 위치 (Y는 고정)
            return new Vector3(randomX, spawnY, randomZ);
        }

        /// <summary>
        /// BoxCollider에서 이동 범위 계산 (Scene View에서 BoxCollider 크기 조정)
        /// 캐릭터 반경(0.5f)만큼 안쪽으로 여유를 둠
        /// </summary>
        private void CalculateMovementBounds()
        {
            if (movementArea != null)
            {
                Bounds bounds = movementArea.bounds;
                const float characterRadius = 0.5f; // CapsuleCollider 반경

                // 캐릭터가 완전히 안쪽에 있도록 반경만큼 마진 추가
                minX = bounds.min.x + characterRadius;
                maxX = bounds.max.x - characterRadius;
                minZ = bounds.min.z + characterRadius;
                maxZ = bounds.max.z - characterRadius;

                GameLog.Log($"<color=cyan>[LobbyCompanionManager]</color> 이동 영역 설정: X({minX:F2} ~ {maxX:F2}), Z({minZ:F2} ~ {maxZ:F2}) [마진: {characterRadius}]");
            }
            else
            {
                GameLog.LogWarning("[LobbyCompanionManager] MovementArea BoxCollider가 할당되지 않았습니다! 기본값 사용.");
                minX = -5f;
                maxX = 5f;
                minZ = -5f;
                maxZ = 5f;
            }
        }

        /// <summary>
        /// 이동 가능한 영역 범위 반환
        /// </summary>
        public Vector4 GetMovementBounds()
        {
            return new Vector4(minX, maxX, minZ, maxZ);
        }

        /// <summary>
        /// Character 전투 컴포넌트 비활성화
        /// - 공격 루프, 타겟팅, 스킬 실행 등 비활성화
        /// </summary>
        private void DisableCombatComponents(GameObject companion)
        {
            // Character.cs 전체 비활성화 (공격 루프 중지)
            var character = companion.GetComponent<Novelian.Combat.Character>();
            if (character != null)
            {
                character.enabled = false;
                GameLog.Log("[LobbyCompanionManager] Character 컴포넌트 비활성화");
            }

            // 추가로 비활성화할 컴포넌트가 있다면 여기에 추가
            // 예: SkillExecutor, TargetingSystem 등

            // Rigidbody는 LobbyCompanion에서 사용하므로 유지
        }

        /// <summary>
        /// 모든 캐릭터 프리팹 프리로드 (CharacterPlacementManager와 동일한 방식)
        /// CharacterTable → PathTable → "Prefab_" + Addressable_Key
        /// </summary>
        private async UniTask PreloadCharacterPrefabs()
        {
            GameLog.Log("[LobbyCompanionManager] 캐릭터 프리팹 프리로딩 시작...");

            // CSVLoader 초기화 대기
            while (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                await UniTask.Delay(100);
            }

            // CharacterTable에서 모든 캐릭터 데이터 조회
            var characterTable = CSVLoader.Instance.GetTable<CharacterData>();
            if (characterTable == null || characterTable.Count == 0)
            {
                GameLog.LogError("[LobbyCompanionManager] CharacterTable 데이터를 찾을 수 없습니다!");
                isPreloadComplete = true;
                return;
            }

            var allCharacterData = characterTable.GetAll();
            int loadedCount = 0;
            int failedCount = 0;

            foreach (var characterData in allCharacterData)
            {
                // PathTable에서 Addressable_Key 조회
                var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
                if (pathData == null)
                {
                    GameLog.LogWarning($"[LobbyCompanionManager] PathData를 찾을 수 없습니다. Path_ID: {characterData.Path_ID}");
                    failedCount++;
                    continue;
                }

                // "Prefab_" + Addressable_Key 로 프리팹 키 생성
                string prefabKey = $"Prefab_{pathData.Addressable_Key}";

                // 이미 로드된 프리팹은 스킵
                if (loadedCharacterPrefabs.ContainsKey(prefabKey))
                {
                    continue;
                }

                try
                {
                    GameObject prefab = await Addressables.LoadAssetAsync<GameObject>(prefabKey).Task;
                    loadedCharacterPrefabs[prefabKey] = prefab;
                    loadedCount++;
                    GameLog.Log($"[LobbyCompanionManager] 로드 완료: {prefabKey} (Character_ID: {characterData.Character_ID})");
                }
                catch (System.Exception e)
                {
                    GameLog.LogError($"[LobbyCompanionManager] 프리팹 로드 실패: '{prefabKey}' - {e.Message}");
                    failedCount++;
                }
            }

            GameLog.Log($"[LobbyCompanionManager] 캐릭터 프리팹 프리로드 완료. 성공: {loadedCount}, 실패: {failedCount}");
            isPreloadComplete = true;
        }

        /// <summary>
        /// Character_ID로 프리팹 키 가져오기
        /// CharacterTable → PathTable → "Prefab_" + Addressable_Key
        /// </summary>
        private string GetCharacterPrefabKey(int characterId)
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                GameLog.LogWarning("[LobbyCompanionManager] CSVLoader가 초기화되지 않았습니다.");
                return null;
            }

            // CharacterTable에서 캐릭터 데이터 조회
            var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
            if (characterData == null)
            {
                GameLog.LogWarning($"[LobbyCompanionManager] CharacterData를 찾을 수 없습니다. Character_ID: {characterId}");
                return null;
            }

            // PathTable에서 Addressable_Key 조회
            var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
            if (pathData == null)
            {
                GameLog.LogWarning($"[LobbyCompanionManager] PathData를 찾을 수 없습니다. Path_ID: {characterData.Path_ID}");
                return null;
            }

            return $"Prefab_{pathData.Addressable_Key}";
        }

        /// <summary>
        /// 모든 동반자 제거
        /// </summary>
        public void ClearCompanions()
        {
            foreach (var companion in spawnedCompanions)
            {
                if (companion != null)
                {
                    Destroy(companion);
                }
            }
            spawnedCompanions.Clear();

            GameLog.Log("<color=cyan>[LobbyCompanionManager]</color> 모든 동반자 제거 완료");
        }

        private void OnDestroy()
        {
            ClearCompanions();
        }
    }
}
