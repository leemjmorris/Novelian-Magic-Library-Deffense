using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Demo
{
    /// <summary>
    /// 몬스터 스폰 포메이션
    /// </summary>
    public enum MonsterFormation
    {
        Random,
        Line,
        Circle
    }

    /// <summary>
    /// Demo-specific monster spawner
    /// Supports spawning from top, bottom, or both directions based on placement mode
    /// Uses direct prefab instantiation (no Addressables/CSVLoader/ObjectPoolManager)
    /// </summary>
    public class DemoMonsterSpawner : MonoBehaviour
    {
        [Header("Monster Settings")]
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private Transform targetObj;

        [Header("Spawn Areas - Top (monsters spawn from bottom)")]
        [SerializeField] private Vector3 bottomSpawnAreaMin = new Vector3(-10f, 0f, 0f);
        [SerializeField] private Vector3 bottomSpawnAreaMax = new Vector3(10f, 0f, 5f);

        [Header("Spawn Areas - Center (monsters spawn from both sides)")]
        [SerializeField] private Vector3 topSpawnAreaMin = new Vector3(-10f, 0f, 45f);
        [SerializeField] private Vector3 topSpawnAreaMax = new Vector3(10f, 0f, 50f);

        private List<GameObject> spawnedMonsters = new List<GameObject>();

        public static DemoMonsterSpawner Instance { get; private set; }
        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            // Wait for CSVLoader to be ready
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.Log("[DemoMonsterSpawner] Waiting for CSVLoader...");
                await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);
                Debug.Log("[DemoMonsterSpawner] CSVLoader ready!");
            }

            // Initialize Monster's Wall cache for targeting
            InitializeWallCache();

            IsReady = true;
            Debug.Log("[DemoMonsterSpawner] Initialized");
        }

        /// <summary>
        /// Initialize Monster's static Wall cache so monsters can attack the target
        /// </summary>
        private void InitializeWallCache()
        {
            if (targetObj == null)
            {
                // Try to find by tag
                GameObject wallObj = GameObject.FindWithTag("Wall");
                if (wallObj != null)
                {
                    targetObj = wallObj.transform;
                }
            }

            if (targetObj != null)
            {
                var wallCollider = targetObj.GetComponent<Collider>();
                var wallComponent = targetObj.GetComponent<Wall>();

                if (wallCollider != null && wallComponent != null)
                {
                    // 단일 Wall을 List로 래핑하여 전달 (다중 Wall 지원 시그니처)
                    var walls = new List<Wall> { wallComponent };
                    var transforms = new List<Transform> { targetObj };
                    var colliders = new List<Collider> { wallCollider };
                    Monster.InitializeWallCache(walls, transforms, colliders);
                    Debug.Log("[DemoMonsterSpawner] Wall cache initialized");
                }
                else
                {
                    Debug.LogWarning("[DemoMonsterSpawner] Target missing Collider or Wall component!");
                }
            }
            else
            {
                Debug.LogWarning("[DemoMonsterSpawner] Target (Wall) not found!");
            }
        }

        #region Monster Spawning

        /// <summary>
        /// Spawn a monster from specified direction
        /// </summary>
        /// <param name="fromTop">If true, spawn from top. If false, spawn from bottom.</param>
        public GameObject SpawnMonster(bool fromTop)
        {
            if (monsterPrefab == null)
            {
                Debug.LogError("[DemoMonsterSpawner] Monster prefab not assigned!");
                return null;
            }

            // Get spawn position
            Vector3 spawnPos = GetRandomSpawnPosition(fromTop);

            // Instantiate monster
            GameObject monsterObj = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
            monsterObj.name = $"DemoMonster_{spawnedMonsters.Count}";

            // Get Monster component and initialize
            var monster = monsterObj.GetComponent<Monster>();
            if (monster != null)
            {
                // Call OnSpawn to initialize health and other properties (normally done by ObjectPool)
                monster.OnSpawn();
                Debug.Log($"[DemoMonsterSpawner] Monster OnSpawn called (health initialized)");

                // Set destination if target exists
                if (targetObj != null)
                {
                    monster.SetDestination(targetObj.position);
                }
            }

            spawnedMonsters.Add(monsterObj);
            Debug.Log($"[DemoMonsterSpawner] Spawned monster from {(fromTop ? "top" : "bottom")} at {spawnPos}");

            return monsterObj;
        }

        /// <summary>
        /// Spawn monsters based on current placement mode
        /// </summary>
        public void SpawnMonstersForCurrentMode(int count)
        {
            if (DemoPlacementManager.Instance == null) return;

            var mode = DemoPlacementManager.Instance.CurrentMode;

            if (mode == DemoPlacementMode.Top)
            {
                // Top mode: spawn from bottom only
                for (int i = 0; i < count; i++)
                {
                    SpawnMonster(false);
                }
            }
            else
            {
                // Center mode: spawn from both sides
                for (int i = 0; i < count; i++)
                {
                    SpawnMonster(i % 2 == 0); // Alternate top/bottom
                }
            }
        }

        private Vector3 GetRandomSpawnPosition(bool fromTop)
        {
            Vector3 min, max;

            if (fromTop)
            {
                min = topSpawnAreaMin;
                max = topSpawnAreaMax;
            }
            else
            {
                min = bottomSpawnAreaMin;
                max = bottomSpawnAreaMax;
            }

            return new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z)
            );
        }

        #endregion

        #region Monster Management

        /// <summary>
        /// Clear all spawned monsters
        /// </summary>
        public void ClearAllMonsters()
        {
            foreach (var monsterObj in spawnedMonsters)
            {
                if (monsterObj != null)
                {
                    // Unregister from TargetRegistry before destroying
                    var monster = monsterObj.GetComponent<Monster>();
                    if (monster != null)
                    {
                        TargetRegistry.Instance.UnregisterTarget(monster);
                    }
                    Destroy(monsterObj);
                }
            }
            spawnedMonsters.Clear();

            Debug.Log("[DemoMonsterSpawner] All monsters cleared");
        }

        public int GetSpawnedMonsterCount()
        {
            // Clean up destroyed monsters
            spawnedMonsters.RemoveAll(m => m == null);
            return spawnedMonsters.Count;
        }

        /// <summary>
        /// Update spawn areas when placement mode changes
        /// </summary>
        public void UpdateSpawnAreasForMode(DemoPlacementMode mode)
        {
            // Spawn areas are already configured in inspector
            // This method can be extended if dynamic adjustment is needed
            Debug.Log($"[DemoMonsterSpawner] Spawn areas updated for {mode} mode");
        }

        /// <summary>
        /// 포메이션에 따라 몬스터 스폰
        /// </summary>
        public void SpawnMonstersInFormation(MonsterFormation formation, int count)
        {
            if (monsterPrefab == null)
            {
                Debug.LogError("[DemoMonsterSpawner] Monster prefab not assigned!");
                return;
            }

            // 스폰 중심점 계산 (bottom spawn area 중심)
            Vector3 center = (bottomSpawnAreaMin + bottomSpawnAreaMax) / 2f;

            List<Vector3> positions = GetFormationPositions(formation, count, center);

            foreach (var pos in positions)
            {
                SpawnMonsterAtPosition(pos);
            }

            Debug.Log($"[DemoMonsterSpawner] Spawned {count} monsters in {formation} formation");
        }

        private List<Vector3> GetFormationPositions(MonsterFormation formation, int count, Vector3 center)
        {
            var positions = new List<Vector3>();

            switch (formation)
            {
                case MonsterFormation.Line:
                    positions = GetLineFormationPositions(count, center);
                    break;

                case MonsterFormation.Circle:
                    positions = GetCircleFormationPositions(count, center);
                    break;

                case MonsterFormation.Random:
                default:
                    positions = GetRandomFormationPositions(count);
                    break;
            }

            return positions;
        }

        private List<Vector3> GetLineFormationPositions(int count, Vector3 center)
        {
            var positions = new List<Vector3>();
            float spacing = 2f;
            float totalWidth = (count - 1) * spacing;
            float startX = center.x - totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * spacing;
                positions.Add(new Vector3(x, center.y, center.z));
            }

            return positions;
        }

        private List<Vector3> GetCircleFormationPositions(int count, Vector3 center)
        {
            var positions = new List<Vector3>();
            float radius = Mathf.Max(2f, count * 0.5f);

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                positions.Add(new Vector3(x, center.y, z));
            }

            return positions;
        }

        private List<Vector3> GetRandomFormationPositions(int count)
        {
            var positions = new List<Vector3>();
            float minSpacing = 3f;
            int maxAttempts = 50;

            for (int i = 0; i < count; i++)
            {
                Vector3 newPos = Vector3.zero;
                bool validPosition = false;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    newPos = GetRandomSpawnPosition(false);

                    // 기존 위치들과 최소 간격 체크
                    validPosition = true;
                    foreach (var existingPos in positions)
                    {
                        float dist = Vector3.Distance(newPos, existingPos);
                        if (dist < minSpacing)
                        {
                            validPosition = false;
                            break;
                        }
                    }

                    if (validPosition) break;
                }

                positions.Add(newPos);
            }

            return positions;
        }

        private GameObject SpawnMonsterAtPosition(Vector3 position)
        {
            GameObject monsterObj = Instantiate(monsterPrefab, position, Quaternion.identity);
            monsterObj.name = $"DemoMonster_{spawnedMonsters.Count}";

            var monster = monsterObj.GetComponent<Monster>();
            if (monster != null)
            {
                monster.OnSpawn();

                if (targetObj != null)
                {
                    monster.SetDestination(targetObj.position);
                }
            }

            spawnedMonsters.Add(monsterObj);
            return monsterObj;
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
