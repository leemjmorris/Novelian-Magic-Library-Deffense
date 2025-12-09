using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Demo
{
    /// <summary>
    /// Demo scene initializer
    /// Initializes CSVLoader without depending on GameManager
    /// Must be placed in DemoScene and execute before other Demo components
    /// </summary>
    public class DemoInitializer : MonoBehaviour
    {
        [Header("Execution Order")]
        [SerializeField, Tooltip("Script Execution Order should be set to run before other Demo scripts")]
        private bool showExecutionOrderWarning = true;

        private void Awake()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            Debug.Log("[DemoInitializer] Starting initialization...");

            // Initialize CSVLoader if not already initialized
            if (CSVLoader.Instance == null)
            {
                // CSVLoader should be a singleton that auto-creates
                Debug.Log("[DemoInitializer] Waiting for CSVLoader instance...");
                await UniTask.WaitUntil(() => CSVLoader.Instance != null);
            }

            if (!CSVLoader.Instance.IsInit)
            {
                Debug.Log("[DemoInitializer] Initializing CSVLoader...");
                await CSVLoader.Instance.InitCSV();
                Debug.Log("[DemoInitializer] CSVLoader initialized!");
            }
            else
            {
                Debug.Log("[DemoInitializer] CSVLoader already initialized");
            }

            Debug.Log("[DemoInitializer] Demo initialization complete!");
        }
    }
}
