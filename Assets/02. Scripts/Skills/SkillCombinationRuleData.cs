using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 메인 스킬과 서포트 스킬의 조합 규칙 데이터
    /// 어떤 behavior_type에 어떤 support_type이 적용 가능한지 정의
    /// 하이브리드 방식: CSV가 원본, ScriptableObject는 런타임 캐시
    /// </summary>
    [CreateAssetMenu(fileName = "SkillCombinationRuleData", menuName = "Novelian/Skill Combination Rules")]
    public class SkillCombinationRuleData : ScriptableObject
    {
        [Serializable]
        public class CombinationRule
        {
            [Tooltip("메인 스킬의 behavior_type")]
            public string behaviorType;

            [Tooltip("이 behavior_type에 적용 가능한 support_type 목록")]
            public List<string> allowedSupportTypes = new List<string>();
        }

        [SerializeField]
        private List<CombinationRule> rules = new List<CombinationRule>();

        // 런타임 캐시
        private Dictionary<string, HashSet<string>> ruleCache;

        // CSV 경로
        public const string CSV_PATH = "Assets/Data/CSV/Skill/SkillCombinationRules.csv";

        /// <summary>
        /// 모든 규칙 가져오기 (에디터용)
        /// </summary>
        public List<CombinationRule> Rules => rules;

        /// <summary>
        /// 캐시 빌드
        /// </summary>
        private void BuildCache()
        {
            ruleCache = new Dictionary<string, HashSet<string>>();
            foreach (var rule in rules)
            {
                if (!ruleCache.ContainsKey(rule.behaviorType))
                {
                    ruleCache[rule.behaviorType] = new HashSet<string>(rule.allowedSupportTypes);
                }
            }
        }

        /// <summary>
        /// 특정 조합이 유효한지 확인
        /// </summary>
        public bool IsValidCombination(string behaviorType, string supportType)
        {
            if (ruleCache == null) BuildCache();

            if (ruleCache.TryGetValue(behaviorType, out var allowedTypes))
            {
                return allowedTypes.Contains(supportType);
            }

            return false;
        }

        /// <summary>
        /// 특정 behavior_type에 허용된 support_type 목록 반환
        /// </summary>
        public List<string> GetAllowedSupportTypes(string behaviorType)
        {
            if (ruleCache == null) BuildCache();

            if (ruleCache.TryGetValue(behaviorType, out var allowedTypes))
            {
                return new List<string>(allowedTypes);
            }

            return new List<string>();
        }

        /// <summary>
        /// 규칙 설정 (에디터용)
        /// </summary>
        public void SetRule(string behaviorType, string supportType, bool isAllowed)
        {
            var rule = rules.Find(r => r.behaviorType == behaviorType);

            if (rule == null)
            {
                rule = new CombinationRule { behaviorType = behaviorType };
                rules.Add(rule);
            }

            if (isAllowed)
            {
                if (!rule.allowedSupportTypes.Contains(supportType))
                {
                    rule.allowedSupportTypes.Add(supportType);
                }
            }
            else
            {
                rule.allowedSupportTypes.Remove(supportType);
            }

            // 캐시 무효화
            ruleCache = null;
        }

        /// <summary>
        /// 특정 조합의 현재 설정 확인 (에디터용)
        /// </summary>
        public bool GetRule(string behaviorType, string supportType)
        {
            var rule = rules.Find(r => r.behaviorType == behaviorType);
            return rule != null && rule.allowedSupportTypes.Contains(supportType);
        }

        /// <summary>
        /// 모든 규칙 초기화 (기본값으로)
        /// </summary>
        public void InitializeDefaultRules(string[] behaviorTypes, string[] supportTypes)
        {
            rules.Clear();

            foreach (var behaviorType in behaviorTypes)
            {
                var rule = new CombinationRule { behaviorType = behaviorType };

                // 기본 규칙 설정
                foreach (var supportType in supportTypes)
                {
                    if (ShouldEnableByDefault(behaviorType, supportType))
                    {
                        rule.allowedSupportTypes.Add(supportType);
                    }
                }

                rules.Add(rule);
            }

            ruleCache = null;
        }

        /// <summary>
        /// 기본적으로 활성화해야 하는 조합인지 판단
        /// 신규 시스템: Projectile, BeamRay, AOE 3개 behavior_type
        /// 신규 서포트: Pierce, Homing, MultiShot, CC, DOT, Enhance, Bounce, Split 8개
        /// </summary>
        private bool ShouldEnableByDefault(string behaviorType, string supportType)
        {
            // 투사체 전용 서포트 (Projectile에만 적용)
            bool isProjectileOnlySupport = supportType == "Pierce" || supportType == "Homing" ||
                                           supportType == "MultiShot" || supportType == "Bounce" ||
                                           supportType == "Split";

            if (isProjectileOnlySupport)
            {
                // Projectile은 모든 투사체 전용 서포트 사용 가능
                if (behaviorType == "Projectile") return true;

                // BeamRay는 투사체 전용 서포트 사용 불가 (이미 관통형으로 구현됨)
                // AOE도 투사체 전용 서포트 사용 불가
                return false;
            }

            // 범용 서포트 (CC, DOT, Enhance) - 모든 behavior_type에 적용 가능
            bool isUniversalSupport = supportType == "CC" || supportType == "DOT" || supportType == "Enhance";
            if (isUniversalSupport)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 캐시 강제 리빌드
        /// </summary>
        public void RefreshCache()
        {
            ruleCache = null;
            BuildCache();
        }

#if UNITY_EDITOR
        /// <summary>
        /// CSV 파일에서 규칙 로드
        /// </summary>
        public bool LoadFromCSV(string csvPath = null)
        {
            string path = csvPath ?? CSV_PATH;

            if (!File.Exists(path))
            {
                GameLog.LogWarning($"[SkillCombinationRuleData] CSV 파일을 찾을 수 없습니다: {path}");
                return false;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                if (lines.Length < 2)
                {
                    GameLog.LogWarning("[SkillCombinationRuleData] CSV 파일이 비어있습니다.");
                    return false;
                }

                // 헤더 파싱 (첫 번째 열은 behavior_type, 나머지는 support_type)
                string[] headers = lines[0].Split(',');
                string[] supportTypes = new string[headers.Length - 1];
                for (int i = 1; i < headers.Length; i++)
                {
                    supportTypes[i - 1] = headers[i].Trim();
                }

                // 규칙 초기화
                rules.Clear();

                // 데이터 행 파싱
                for (int lineIdx = 1; lineIdx < lines.Length; lineIdx++)
                {
                    string line = lines[lineIdx].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    string[] values = line.Split(',');
                    if (values.Length < 2) continue;

                    string behaviorType = values[0].Trim();
                    var rule = new CombinationRule { behaviorType = behaviorType };

                    for (int i = 1; i < values.Length && i <= supportTypes.Length; i++)
                    {
                        string value = values[i].Trim().ToLower();
                        if (value == "true" || value == "1" || value == "yes")
                        {
                            rule.allowedSupportTypes.Add(supportTypes[i - 1]);
                        }
                    }

                    rules.Add(rule);
                }

                ruleCache = null;
                GameLog.Log($"[SkillCombinationRuleData] CSV 로드 완료: {rules.Count}개 행, {supportTypes.Length}개 서포트 타입");
                return true;
            }
            catch (Exception e)
            {
                GameLog.LogError($"[SkillCombinationRuleData] CSV 로드 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 규칙을 CSV 파일로 저장
        /// </summary>
        public bool SaveToCSV(string[] supportTypes, string csvPath = null)
        {
            string path = csvPath ?? CSV_PATH;

            try
            {
                // 폴더 확인
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                StringBuilder sb = new StringBuilder();

                // 헤더 행
                sb.Append("behavior_type");
                foreach (var supportType in supportTypes)
                {
                    sb.Append(",");
                    sb.Append(supportType);
                }
                sb.AppendLine();

                // 데이터 행
                foreach (var rule in rules)
                {
                    sb.Append(rule.behaviorType);
                    foreach (var supportType in supportTypes)
                    {
                        sb.Append(",");
                        sb.Append(rule.allowedSupportTypes.Contains(supportType) ? "true" : "false");
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                GameLog.Log($"[SkillCombinationRuleData] CSV 저장 완료: {path}");
                return true;
            }
            catch (Exception e)
            {
                GameLog.LogError($"[SkillCombinationRuleData] CSV 저장 실패: {e.Message}");
                return false;
            }
        }

        private void OnValidate()
        {
            ruleCache = null;
        }
#endif
    }
}
