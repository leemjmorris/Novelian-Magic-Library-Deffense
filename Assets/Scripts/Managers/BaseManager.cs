using UnityEngine;

namespace NovelianMagicLibraryDefense.Core
{
    /// <summary>
    /// LMJ : Abstract base class providing common functionality for all managers
    /// </summary>
    public abstract class BaseManager : IManager
    {
        protected bool isInitialized;

        /// <summary>
        /// LMJ : Initialize manager resources with idempotent guard
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
        }

        /// <summary>
        /// LMJ : Reset manager to initial state with initialization check
        /// </summary>
        public virtual void Reset()
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"{GetType().Name} is not initialized yet.");
                return;
            }

            OnReset();
        }

        /// <summary>
        /// LMJ : Clean up resources and release references
        /// </summary>
        public virtual void Dispose()
        {
            if (!isInitialized)
            {
                return;
            }

            OnDispose();
            isInitialized = false;
        }

        /// <summary>
        /// LMJ : Override to implement manager-specific initialization logic
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// LMJ : Override to implement manager-specific reset logic
        /// </summary>
        protected abstract void OnReset();

        /// <summary>
        /// LMJ : Override to implement manager-specific disposal logic
        /// </summary>
        protected abstract void OnDispose();
    }
}