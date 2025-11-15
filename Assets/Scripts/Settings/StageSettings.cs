using UnityEngine;

namespace NovelianMagicLibraryDefense.Settings
{
    /// <summary>
    /// Stage 관련 설정값들을 관리하는 ScriptableObject
    /// Inspector에서 쉽게 조정 가능
    /// </summary>
    [CreateAssetMenu(fileName = "StageSettings", menuName = "Settings/Stage Settings")]
    public class StageSettings : ScriptableObject
    {
        [Header("Timer Settings")]
        [Tooltip("스테이지 제한 시간 (초)")]
        public float stageDuration = 600f; // 10 minutes

        [Header("Level Settings")]
        [Tooltip("레벨업에 필요한 경험치")]
        public int expPerLevel = 100;

        [Tooltip("최대 레벨 (0 = 제한 없음)")]
        public int maxLevel = 0;
    }
}
