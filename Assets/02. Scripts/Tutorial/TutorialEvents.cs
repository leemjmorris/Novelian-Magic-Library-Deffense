using System;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 이벤트 채널
    /// ScriptableObject 기반으로 씬 간 이벤트 전달 가능
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialEvents", menuName = "Events/Tutorial Events")]
    public class TutorialEvents : ScriptableObject
    {
        // 튜토리얼 시작/종료 이벤트
        private event Action<string> onTutorialStarted;
        private event Action<string> onTutorialCompleted;
        private event Action<string> onTutorialSkipped;

        // 스텝 진행 이벤트
        private event Action<int> onStepChanged;
        private event Action<string> onStepActionCompleted;

        // UI 이벤트
        private event Action onSkipRequested;
        private event Action onDialogTouched;

        #region Raise Methods

        public void RaiseTutorialStarted(string tutorialId)
        {
            onTutorialStarted?.Invoke(tutorialId);
        }

        public void RaiseTutorialCompleted(string tutorialId)
        {
            onTutorialCompleted?.Invoke(tutorialId);
        }

        public void RaiseTutorialSkipped(string tutorialId)
        {
            onTutorialSkipped?.Invoke(tutorialId);
        }

        public void RaiseStepChanged(int stepIndex)
        {
            onStepChanged?.Invoke(stepIndex);
        }

        public void RaiseStepActionCompleted(string actionKey)
        {
            onStepActionCompleted?.Invoke(actionKey);
        }

        public void RaiseSkipRequested()
        {
            onSkipRequested?.Invoke();
        }

        public void RaiseDialogTouched()
        {
            onDialogTouched?.Invoke();
        }

        #endregion

        #region Subscribe Methods

        public void AddTutorialStartedListener(Action<string> listener) => onTutorialStarted += listener;
        public void RemoveTutorialStartedListener(Action<string> listener) => onTutorialStarted -= listener;

        public void AddTutorialCompletedListener(Action<string> listener) => onTutorialCompleted += listener;
        public void RemoveTutorialCompletedListener(Action<string> listener) => onTutorialCompleted -= listener;

        public void AddTutorialSkippedListener(Action<string> listener) => onTutorialSkipped += listener;
        public void RemoveTutorialSkippedListener(Action<string> listener) => onTutorialSkipped -= listener;

        public void AddStepChangedListener(Action<int> listener) => onStepChanged += listener;
        public void RemoveStepChangedListener(Action<int> listener) => onStepChanged -= listener;

        public void AddStepActionCompletedListener(Action<string> listener) => onStepActionCompleted += listener;
        public void RemoveStepActionCompletedListener(Action<string> listener) => onStepActionCompleted -= listener;

        public void AddSkipRequestedListener(Action listener) => onSkipRequested += listener;
        public void RemoveSkipRequestedListener(Action listener) => onSkipRequested -= listener;

        public void AddDialogTouchedListener(Action listener) => onDialogTouched += listener;
        public void RemoveDialogTouchedListener(Action listener) => onDialogTouched -= listener;

        #endregion

        private void OnDisable()
        {
            onTutorialStarted = null;
            onTutorialCompleted = null;
            onTutorialSkipped = null;
            onStepChanged = null;
            onStepActionCompleted = null;
            onSkipRequested = null;
            onDialogTouched = null;
        }
    }
}
