using System.Collections.Generic;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// Issue #602: Centralized time scale management (DontDestroyOnLoad Singleton)
    /// Handles time scale changes with stack-based approach to prevent conflicts
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        private static TimeManager instance;
        public static TimeManager Instance => instance;

        private Stack<float> timeScaleStack = new Stack<float>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize with default time scale
            timeScaleStack.Clear();
            timeScaleStack.Push(1f);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Push a new time scale onto the stack and apply it
        /// Use this when temporarily changing time scale (e.g., opening pause menu)
        /// </summary>
        public void PushTimeScale(float newScale)
        {
            timeScaleStack.Push(newScale);
            Time.timeScale = newScale;
        }

        /// <summary>
        /// Pop the current time scale and restore previous one
        /// Use this when closing pause menu or ending temporary time scale change
        /// </summary>
        public void PopTimeScale()
        {
            if (timeScaleStack.Count > 1)
            {
                timeScaleStack.Pop();
                Time.timeScale = timeScaleStack.Peek();
            }
            else
            {
                Debug.LogWarning("[TimeManager] Cannot pop time scale - only one value remaining");
            }
        }

        /// <summary>
        /// Set time scale directly (replaces top of stack)
        /// Use this for game speed changes (1x, 1.5x, 2x)
        /// </summary>
        public void SetTimeScale(float scale)
        {
            if (timeScaleStack.Count > 0)
            {
                timeScaleStack.Pop();
            }
            timeScaleStack.Push(scale);
            Time.timeScale = scale;
        }

        /// <summary>
        /// Get current time scale
        /// </summary>
        public float CurrentTimeScale => Time.timeScale;

        /// <summary>
        /// Get previous time scale (before last push)
        /// </summary>
        public float PreviousTimeScale
        {
            get
            {
                if (timeScaleStack.Count > 1)
                {
                    float current = timeScaleStack.Pop();
                    float previous = timeScaleStack.Peek();
                    timeScaleStack.Push(current);
                    return previous;
                }
                return 1f;
            }
        }

        /// <summary>
        /// Pause the game (push 0 time scale)
        /// </summary>
        public void Pause()
        {
            PushTimeScale(0f);
        }

        /// <summary>
        /// Resume the game (pop time scale)
        /// </summary>
        public void Resume()
        {
            PopTimeScale();
        }

        /// <summary>
        /// Issue #602: Reset time scale stack and set to 1f
        /// Use this when transitioning scenes to ensure clean state
        /// </summary>
        public void ResetTimeScale()
        {
            timeScaleStack.Clear();
            timeScaleStack.Push(1f);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Issue #602: Check if currently paused (TimeScale pushed to 0)
        /// </summary>
        public bool IsPaused => timeScaleStack.Count > 1 && Time.timeScale == 0f;

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
