//LMJ : Character partial class - Targeting Logic (Chain targets, AOE position)
namespace Novelian.Combat
{
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// 캐릭터 타겟팅 로직
    /// - 체인 타겟 빌드 (채널링 스킬용)
    /// - AOE 최적 위치 탐색
    /// - 타겟 필터링
    /// </summary>
    public partial class Character
    {
        #region Chain Targeting (Channeling Skills)

        /// <summary>
        /// 체인 타겟 리스트 생성 (채널링 스킬용)
        /// 서포트 스킬의 chain 효과가 있으면 추가 타겟 연결
        /// </summary>
        /// <param name="primaryTarget">1차 타겟</param>
        /// <returns>체인 타겟 리스트 (1차 타겟 포함)</returns>
        private List<ITargetable> BuildChainTargets(ITargetable primaryTarget)
        {
            List<ITargetable> targets = new List<ITargetable>();

            if (primaryTarget == null || !primaryTarget.IsAlive())
                return targets;

            targets.Add(primaryTarget);

            // 체인 서포트가 없으면 1차 타겟만 반환
            if (supportData == null || supportData.GetStatusEffectType() != StatusEffectType.Chain)
                return targets;

            // 체인 서포트가 있으면 추가 타겟 연결
            int chainCount = supportData.chain_count;
            float chainRange = supportData.chain_range;

            if (chainCount <= 0 || chainRange <= 0)
                return targets;

            Debug.Log($"[Character] Building chain targets: {chainCount} additional targets within {chainRange} range");

            // 체인 타겟 탐색
            ITargetable currentTarget = primaryTarget;
            HashSet<ITargetable> visitedTargets = new HashSet<ITargetable> { primaryTarget };

            for (int i = 0; i < chainCount; i++)
            {
                ITargetable nextTarget = FindNextChainTarget(currentTarget, chainRange, visitedTargets);

                if (nextTarget == null)
                {
                    Debug.Log($"[Character] Chain ended at {i + 1} targets (no more valid targets)");
                    break;
                }

                targets.Add(nextTarget);
                visitedTargets.Add(nextTarget);
                currentTarget = nextTarget;
            }

            Debug.Log($"[Character] Built chain with {targets.Count} targets");
            return targets;
        }

        /// <summary>
        /// 다음 체인 타겟 탐색 (가장 가까운 유효 타겟)
        /// </summary>
        private ITargetable FindNextChainTarget(ITargetable fromTarget, float chainRange, HashSet<ITargetable> excludeTargets)
        {
            if (fromTarget == null)
                return null;

            Vector3 fromPos = fromTarget.GetPosition();
            Collider[] hits = Physics.OverlapSphere(fromPos, chainRange);

            ITargetable bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                // 몬스터/보스만 타겟
                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target == null || !target.IsAlive())
                    continue;

                // 이미 방문한 타겟 제외
                if (excludeTargets.Contains(target))
                    continue;

                // 거리 계산
                float distance = Vector3.Distance(fromPos, target.GetPosition());
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        #endregion

        #region Single Target Selection

        /// <summary>
        /// 범위 내 가장 가까운 타겟 반환 (부채꼴 AOE 등에서 방향 결정용)
        /// </summary>
        /// <param name="range">탐색 범위</param>
        /// <returns>가장 가까운 타겟 (없으면 null)</returns>
        private ITargetable GetFirstTargetInRange(float range)
        {
            Vector3 characterPos = transform.position;
            Collider[] hits = Physics.OverlapSphere(characterPos, range);

            ITargetable closestTarget = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target == null || !target.IsAlive())
                    continue;

                float distance = Vector3.Distance(characterPos, target.GetPosition());
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        #endregion

        #region AOE Targeting

        /// <summary>
        /// AOE 스킬의 최적 타겟 위치 탐색 (몬스터 밀집 지역)
        /// </summary>
        /// <param name="range">탐색 범위</param>
        /// <param name="aoeRadius">AOE 반경</param>
        /// <returns>최적 위치 (타겟 없으면 Vector3.zero)</returns>
        private Vector3 FindBestAOETargetPosition(float range, float aoeRadius)
        {
            // 캐릭터 위치 기준 탐색
            Vector3 characterPos = transform.position;

            // 범위 내 모든 몬스터 탐색
            Collider[] hits = Physics.OverlapSphere(characterPos, range);
            List<ITargetable> validTargets = new List<ITargetable>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target != null && target.IsAlive())
                {
                    validTargets.Add(target);
                }
            }

            if (validTargets.Count == 0)
            {
                Debug.Log("[Character] FindBestAOETargetPosition: No valid targets");
                return Vector3.zero;
            }

            // 타겟이 1개면 해당 위치 반환
            if (validTargets.Count == 1)
            {
                return validTargets[0].GetPosition();
            }

            // 가장 많은 타겟을 포함하는 위치 탐색 (그리디 알고리즘)
            Vector3 bestPosition = Vector3.zero;
            int bestHitCount = 0;

            // 각 타겟 위치를 중심으로 AOE 범위 내 타겟 수 계산
            for (int i = 0; i < validTargets.Count; i++)
            {
                Vector3 candidatePos = validTargets[i].GetPosition();
                int hitCount = CountTargetsInRadius(validTargets, candidatePos, aoeRadius);

                if (hitCount > bestHitCount)
                {
                    bestHitCount = hitCount;
                    bestPosition = candidatePos;
                }
            }

            Debug.Log($"[Character] Best AOE position found: {bestPosition} (hits {bestHitCount} targets)");
            return bestPosition;
        }

        /// <summary>
        /// 특정 위치에서 반경 내 타겟 수 계산
        /// </summary>
        private int CountTargetsInRadius(List<ITargetable> targets, Vector3 center, float radius)
        {
            int count = 0;
            float radiusSq = radius * radius;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null || !targets[i].IsAlive())
                    continue;

                float distSq = (targets[i].GetPosition() - center).sqrMagnitude;
                if (distSq <= radiusSq)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion
    }
}
