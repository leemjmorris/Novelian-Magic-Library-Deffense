using System.Collections.Generic;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// ITargetable 관련 유틸리티 메서드
    /// 스킬 시스템 전반에서 사용되는 중복 코드 통합
    /// </summary>
    public static class TargetableUtils
    {
        /// <summary>
        /// Collider에서 ITargetable 가져오기
        /// Monster 또는 BossMonster 컴포넌트 반환
        /// </summary>
        public static ITargetable GetTargetable(Collider col)
        {
            if (col == null) return null;

            if (col.CompareTag(Tag.Monster))
            {
                return col.GetComponent<Monster>();
            }

            if (col.CompareTag(Tag.BossMonster))
            {
                return col.GetComponent<BossMonster>();
            }

            return null;
        }

        /// <summary>
        /// Collider가 유효한 적인지 확인
        /// </summary>
        public static bool IsValidEnemy(Collider col)
        {
            return col.CompareTag(Tag.Monster) || col.CompareTag(Tag.BossMonster);
        }

        /// <summary>
        /// 범위 내 모든 적에게 데미지 적용
        /// </summary>
        public static void ApplyDamageInRadius(Vector3 center, float radius, float damage)
        {
            ApplyDamageInRadius(center, radius, damage, false);
        }

        /// <summary>
        /// 범위 내 모든 적에게 데미지 적용 (크리티컬 여부 포함)
        /// </summary>
        public static void ApplyDamageInRadius(Vector3 center, float radius, float damage, bool isCritical)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);

            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (!IsValidEnemy(col)) continue;

                ITargetable target = GetTargetable(col);
                if (target != null && target.IsAlive())
                {
                    target.TakeDamage(damage, isCritical);
                }
            }
        }

        /// <summary>
        /// 범위 내 모든 적에게 데미지 적용 + 콜백
        /// </summary>
        public static void ApplyDamageInRadius(Vector3 center, float radius, float damage, System.Action<ITargetable> onHit)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);

            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (!IsValidEnemy(col)) continue;

                ITargetable target = GetTargetable(col);
                if (target != null && target.IsAlive())
                {
                    target.TakeDamage(damage);
                    onHit?.Invoke(target);
                }
            }
        }

        /// <summary>
        /// 범위 내 모든 적에게 데미지 적용 + 맞은 적 목록 반환
        /// </summary>
        public static List<ITargetable> ApplyDamageInRadiusAndGetTargets(Vector3 center, float radius, float damage)
        {
            List<ITargetable> hitTargets = new List<ITargetable>();
            Collider[] colliders = Physics.OverlapSphere(center, radius);

            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (!IsValidEnemy(col)) continue;

                ITargetable target = GetTargetable(col);
                if (target != null && target.IsAlive())
                {
                    target.TakeDamage(damage);
                    hitTargets.Add(target);
                }
            }

            return hitTargets;
        }

        /// <summary>
        /// 범위 내 적 목록 반환
        /// </summary>
        public static List<ITargetable> GetTargetsInRadius(Vector3 center, float radius)
        {
            List<ITargetable> targets = new List<ITargetable>();
            Collider[] colliders = Physics.OverlapSphere(center, radius);

            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (!IsValidEnemy(col)) continue;

                ITargetable target = GetTargetable(col);
                if (target != null && target.IsAlive())
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        /// <summary>
        /// 범위 내 가장 가까운 적 찾기 (특정 타겟 제외)
        /// </summary>
        public static ITargetable FindNearestTarget(Vector3 center, float radius, ITargetable excludeTarget = null)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);

            ITargetable nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (!IsValidEnemy(col)) continue;

                ITargetable target = GetTargetable(col);
                if (target == null || !target.IsAlive() || target == excludeTarget)
                    continue;

                float dist = Vector3.Distance(center, target.GetTransform().position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = target;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 타겟의 조준점 위치 반환 (Collider 중심 또는 위치 + 1m)
        /// </summary>
        public static Vector3 GetAimPosition(ITargetable target)
        {
            if (target == null) return Vector3.zero;

            Vector3 pos = target.GetTransform().position;
            Collider col = target.GetTransform().GetComponent<Collider>();

            if (col != null)
            {
                return col.bounds.center;
            }

            return pos + Vector3.up * 1f;
        }
    }
}
