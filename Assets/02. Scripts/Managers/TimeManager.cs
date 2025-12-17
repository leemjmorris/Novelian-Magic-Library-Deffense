using System.Collections.Generic;
using NovelianMagicLibraryDefense.Core;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Centralized time scale management
    /// Handles time scale changes with stack-based approach to prevent conflicts
    /// Single responsibility: Manage game time scale
    /// </summary>
    public class TimeManager : BaseManager
    {
        private Stack<float> timeScaleStack = new Stack<float>();

        protected override void OnInitialize()
        {
            // Initialize with default time scale
            timeScaleStack.Clear();
            timeScaleStack.Push(1f);
            Time.timeScale = 1f;
        }

        protected override void OnReset()
        {
            // Reset to default time scale
            timeScaleStack.Clear();
            timeScaleStack.Push(1f);
            Time.timeScale = 1f;
        }

        protected override void OnDispose()
        {
            // Cleanup
            timeScaleStack.Clear();
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
    }
}
