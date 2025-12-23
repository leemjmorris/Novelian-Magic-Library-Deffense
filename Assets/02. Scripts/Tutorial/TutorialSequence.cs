using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 시퀀스 ScriptableObject
    /// 에디터에서 튜토리얼 흐름을 설정
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialSequence", menuName = "Tutorial/Tutorial Sequence")]
    public class TutorialSequence : ScriptableObject
    {
        [Header("시퀀스 정보")]
        [Tooltip("튜토리얼 고유 ID")]
        public string TutorialId;

        [Tooltip("튜토리얼 이름 (에디터용)")]
        public string TutorialName;

        [Tooltip("튜토리얼 설명")]
        [TextArea(2, 4)]
        public string Description;

        [Header("시퀀스 설정")]
        [Tooltip("튜토리얼 스텝 목록")]
        public List<TutorialStep> Steps = new List<TutorialStep>();

        [Header("완료 설정")]
        [Tooltip("완료 후 저장 키 (PlayerPrefs)")]
        public string CompletionSaveKey;

        [Tooltip("스킵 가능 여부")]
        public bool CanSkip = true;

        [Tooltip("완료 시 다음 튜토리얼 ID (없으면 비워두기)")]
        public string NextTutorialId;

        /// <summary>
        /// 튜토리얼 완료 여부 확인
        /// </summary>
        public bool IsCompleted()
        {
            if (string.IsNullOrEmpty(CompletionSaveKey))
                return false;

            return PlayerPrefs.GetInt(CompletionSaveKey, 0) == 1;
        }

        /// <summary>
        /// 튜토리얼 완료 처리
        /// </summary>
        public void MarkAsCompleted()
        {
            if (!string.IsNullOrEmpty(CompletionSaveKey))
            {
                PlayerPrefs.SetInt(CompletionSaveKey, 1);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 튜토리얼 완료 상태 리셋
        /// </summary>
        public void ResetCompletion()
        {
            if (!string.IsNullOrEmpty(CompletionSaveKey))
            {
                PlayerPrefs.DeleteKey(CompletionSaveKey);
                PlayerPrefs.Save();
            }
        }
    }
}
