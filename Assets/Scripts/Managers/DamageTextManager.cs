using UnityEngine;
using Cysharp.Threading.Tasks;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manager for spawning floating damage text
    /// Uses object pooling for performance
    /// </summary>
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private string damageTextAddressableKey = "FloatingDamageText";
        [SerializeField] private int poolDefaultCapacity = 20;
        [SerializeField] private int poolMaxSize = 100;
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float randomOffsetRange = 0.3f;

        private bool isPoolInitialized = false;
        private ObjectPoolManager poolManager;

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
            poolManager = GameManager.Instance?.Pool;
            if (poolManager != null)
            {
                InitializePoolAsync().Forget();
            }
        }

        private async UniTaskVoid InitializePoolAsync()
        {
            try
            {
                await poolManager.CreatePoolAsync<FloatingDamageText>(
                    damageTextAddressableKey,
                    poolDefaultCapacity,
                    poolMaxSize
                );
                isPoolInitialized = true;
                Debug.Log("[DamageTextManager] Damage text pool initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DamageTextManager] Failed to initialize pool: {e.Message}");
                isPoolInitialized = false;
            }
        }

        /// <summary>
        /// Show damage text at the specified position
        /// </summary>
        public void ShowDamage(Vector3 position, float damage, bool isCritical = false)
        {
            if (!isPoolInitialized || poolManager == null) return;

            // Add random offset to prevent overlap
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                0f,
                Random.Range(-randomOffsetRange, randomOffsetRange)
            );

            Vector3 spawnPos = position + spawnOffset + randomOffset;

            var damageText = poolManager.Spawn<FloatingDamageText>(spawnPos, Quaternion.identity);
            if (damageText != null)
            {
                damageText.Initialize(damage, isCritical, false);
            }
        }

        /// <summary>
        /// Show heal text at the specified position
        /// </summary>
        public void ShowHeal(Vector3 position, float healAmount)
        {
            if (!isPoolInitialized || poolManager == null) return;

            Vector3 randomOffset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                0f,
                Random.Range(-randomOffsetRange, randomOffsetRange)
            );

            Vector3 spawnPos = position + spawnOffset + randomOffset;

            var damageText = poolManager.Spawn<FloatingDamageText>(spawnPos, Quaternion.identity);
            if (damageText != null)
            {
                damageText.Initialize(healAmount, false, true);
            }
        }

        /// <summary>
        /// Show status text (STUN, IMMUNE, etc.) at the specified position
        /// </summary>
        public void ShowStatus(Vector3 position, string statusText, Color color)
        {
            if (!isPoolInitialized || poolManager == null) return;

            Vector3 spawnPos = position + spawnOffset;

            var damageText = poolManager.Spawn<FloatingDamageText>(spawnPos, Quaternion.identity);
            if (damageText != null)
            {
                damageText.InitializeStatus(statusText, color);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
