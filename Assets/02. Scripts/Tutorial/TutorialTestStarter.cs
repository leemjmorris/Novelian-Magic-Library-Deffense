using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 테스트용 스타터
    /// 씬 시작 시 TutorialManager를 초기화하고 지정된 튜토리얼을 실행합니다.
    /// </summary>
    public class TutorialTestStarter : MonoBehaviour
    {
        [Header("Tutorial Settings")]
        [SerializeField] private TutorialSequence tutorialSequence;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool forceStart = true; // 완료된 튜토리얼도 강제 실행

        [Header("Debug")]
        [SerializeField] private bool resetProgressOnStart = true;

        private async void Start()
        {
            if (resetProgressOnStart)
            {
                PlayerPrefs.DeleteKey($"Tutorial_{tutorialSequence?.TutorialId}_Completed");
                PlayerPrefs.Save();
                Debug.Log("[TutorialTestStarter] Tutorial progress reset");
            }

            // CSVLoader 초기화 대기
            await WaitForCSVLoaderAsync();

            // TutorialManager 생성 또는 찾기
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager == null)
            {
                var go = new GameObject("TutorialManager");
                tutorialManager = go.AddComponent<TutorialManager>();
            }

            // TutorialManager 초기화
            await tutorialManager.InitializeAsync();

            // 자동 시작
            if (autoStart && tutorialSequence != null)
            {
                Debug.Log($"[TutorialTestStarter] Starting tutorial: {tutorialSequence.TutorialId}");
                tutorialManager.StartTutorial(tutorialSequence, forceStart);
            }
        }

        private async UniTask WaitForCSVLoaderAsync()
        {
            // CSVLoader가 초기화될 때까지 대기
            while (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                await UniTask.Yield();
            }
            Debug.Log("[TutorialTestStarter] CSVLoader ready");
        }

        [ContextMenu("Start Tutorial")]
        public void StartTutorialManual()
        {
            if (TutorialManager.Instance != null && tutorialSequence != null)
            {
                TutorialManager.Instance.StartTutorial(tutorialSequence, forceStart);
            }
        }

        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            if (tutorialSequence != null)
            {
                PlayerPrefs.DeleteKey($"Tutorial_{tutorialSequence.TutorialId}_Completed");
                PlayerPrefs.Save();
                Debug.Log("[TutorialTestStarter] Progress reset");
            }
        }
    }
}
