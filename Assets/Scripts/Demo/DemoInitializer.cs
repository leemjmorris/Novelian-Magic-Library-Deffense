using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Demo
{
    /// <summary>
    /// Demo scene initializer
    /// Ensures CSVLoader is ready before other Demo components start
    /// Must be placed in DemoScene
    /// </summary>
    public class DemoInitializer : MonoBehaviour
    {
        public static DemoInitializer Instance { get; private set; }
        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            Debug.Log("[DemoInitializer] Starting initialization...");

            // Wait for CSVLoader instance
            if (CSVLoader.Instance == null)
            {
                Debug.Log("[DemoInitializer] Waiting for CSVLoader instance...");
                await UniTask.WaitUntil(() => CSVLoader.Instance != null);
            }

            // Wait for CSVLoader to finish loading (it auto-loads in Start)
            if (!CSVLoader.Instance.IsInit)
            {
                Debug.Log("[DemoInitializer] Waiting for CSVLoader to finish loading...");
                await UniTask.WaitUntil(() => CSVLoader.Instance.IsInit);
            }

            Debug.Log("[DemoInitializer] CSVLoader ready!");
            IsReady = true;
            Debug.Log("[DemoInitializer] Demo initialization complete!");
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
