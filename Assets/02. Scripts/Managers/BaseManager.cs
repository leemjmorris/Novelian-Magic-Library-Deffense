using UnityEngine;

namespace NovelianMagicLibraryDefense.Core
{
    /// <summary>
    /// MonoBehaviour 기반 Manager의 베이스 클래스
    /// VContainer와 함께 사용 가능
    /// </summary>
    public abstract class BaseManager : MonoBehaviour
    {
        protected bool isInitialized;

        /// <summary>
        /// Manager 초기화 (Awake에서 자동 호출하거나 수동 호출)
        /// </summary>
        public virtual void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning($"{GetType().Name} is already initialized.");
                return;
            }

            OnInitialize();
            isInitialized = true;
            Debug.Log($"[{GetType().Name}] Initialized");
        }

        /// <summary>
        /// Manager 상태를 초기 상태로 리셋
        /// </summary>
        public virtual void Reset()
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"{GetType().Name} is not initialized yet.");
                return;
            }

            OnReset();
            Debug.Log($"[{GetType().Name}] Reset");
        }

        /// <summary>
        /// 리소스 정리 및 이벤트 구독 해제 (OnDestroy에서 자동 호출)
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (!isInitialized) return;

            OnDispose();
            isInitialized = false;
            Debug.Log($"[{GetType().Name}] Disposed");
        }

        /// <summary>
        /// 하위 클래스에서 초기화 로직 구현
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// 하위 클래스에서 리셋 로직 구현
        /// </summary>
        protected abstract void OnReset();

        /// <summary>
        /// 하위 클래스에서 정리 로직 구현
        /// </summary>
        protected abstract void OnDispose();
    }
}
