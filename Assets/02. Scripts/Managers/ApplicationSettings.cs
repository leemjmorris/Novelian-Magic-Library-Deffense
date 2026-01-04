using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// 앱 전체 설정 관리 싱글톤
    /// 화면 꺼짐 방지, 화면 방향, 프레임레이트 등 Unity 앱 설정 관리
    /// </summary>
    public class ApplicationSettings : MonoBehaviour
    {
        private static ApplicationSettings instance;
        public static ApplicationSettings Instance => instance;

        [Header("Screen Settings")]
        [SerializeField] private bool preventScreenSleep = true;

        private void Awake()
        {
            // 싱글톤 패턴
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // 앱 설정 초기화
            InitializeSettings();
        }

        /// <summary>
        /// 앱 전체 설정 초기화
        /// </summary>
        private void InitializeSettings()
        {
            // 모바일 화면 꺼짐 방지
            if (preventScreenSleep)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Debug.Log("[ApplicationSettings] 화면 꺼짐 방지 활성화");
            }

            // 추후 추가 가능한 설정들:
            // - Screen.orientation (화면 방향)
            // - Application.targetFrameRate (목표 프레임레이트)
            // - QualitySettings (그래픽 품질)
        }

        /// <summary>
        /// 화면 꺼짐 방지 설정 변경
        /// </summary>
        public void SetPreventScreenSleep(bool prevent)
        {
            preventScreenSleep = prevent;
            Screen.sleepTimeout = prevent ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            Debug.Log($"[ApplicationSettings] 화면 꺼짐 방지: {prevent}");
        }
    }
}
