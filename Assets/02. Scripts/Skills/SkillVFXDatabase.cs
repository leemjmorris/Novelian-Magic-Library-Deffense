using System;
using System.Collections.Generic;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 VFX 프리팹 데이터베이스
    /// 인스펙터에서 직접 프리팹 참조 (CLAUDE.md 지침 준수)
    /// 스탯 데이터는 CSV에서 관리
    /// </summary>
    [CreateAssetMenu(fileName = "SkillVFXDatabase", menuName = "Novelian/Skill VFX Database")]
    public class SkillVFXDatabase : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("MainSkillTable.csv의 skill_id와 매칭")]
            public int skillId;

            [Tooltip("스킬 이펙트 프리팹 (투사체, 빔, AOE 등)")]
            public GameObject vfxPrefab;

            [Tooltip("피격/폭발 이펙트 프리팹 (선택)")]
            public GameObject hitPrefab;
        }

        [SerializeField]
        private List<Entry> entries = new List<Entry>();

        // 런타임 캐시 (skillId -> Entry)
        private Dictionary<int, Entry> cache;

        /// <summary>
        /// 캐시 초기화 (첫 접근 시 자동 호출)
        /// </summary>
        private void BuildCache()
        {
            cache = new Dictionary<int, Entry>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && !cache.ContainsKey(entry.skillId))
                {
                    cache[entry.skillId] = entry;
                }
            }
        }

        /// <summary>
        /// skill_id로 VFX 프리팹 가져오기
        /// </summary>
        public GameObject GetVFXPrefab(int skillId)
        {
            if (cache == null) BuildCache();

            if (cache.TryGetValue(skillId, out Entry entry))
            {
                return entry.vfxPrefab;
            }

            Debug.LogWarning($"[SkillVFXDatabase] VFX prefab not found for skill_id: {skillId}");
            return null;
        }

        /// <summary>
        /// skill_id로 Hit 프리팹 가져오기
        /// </summary>
        public GameObject GetHitPrefab(int skillId)
        {
            if (cache == null) BuildCache();

            if (cache.TryGetValue(skillId, out Entry entry))
            {
                return entry.hitPrefab;
            }

            return null;
        }

        /// <summary>
        /// skill_id로 Entry 전체 가져오기
        /// </summary>
        public Entry GetEntry(int skillId)
        {
            if (cache == null) BuildCache();

            cache.TryGetValue(skillId, out Entry entry);
            return entry;
        }

        /// <summary>
        /// 캐시 강제 리빌드 (에디터에서 수정 후 호출)
        /// </summary>
        public void RefreshCache()
        {
            cache = null;
            BuildCache();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 수정 시 캐시 무효화
            cache = null;
        }
#endif
    }
}
