using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 투사체 - 컨테이너 방식
    /// 데미지 타입(단일/범위)과 서포트 효과가 독립적으로 동작
    /// </summary>
    public class SkillProjectile : MonoBehaviour
    {
        #region Runtime Data
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;
        private ITargetable target;
        private bool isExplosive;
        private GameObject hitPrefab;
        private float hitScale = 1f;
        #endregion

        #region State
        private bool isInitialized;
        private float damage;
        private float speed;
        private float maxRange;
        private Vector3 startPosition;
        #endregion

        #region Support State
        private int pierceCount;
        private int chainCount;
        private int splitCount;
        private float splitSpreadAngle;
        private float splitScaleMultiplier;
        private bool isHoming;
        private float homingStrength;
        #endregion

        #region Hit Tracking
        private HashSet<int> hitTargets = new HashSet<int>();
        #endregion

        #region Constants
        private const float DEFAULT_SPEED = 15f;
        private const float DEFAULT_RANGE = 20f;
        private const float HIT_EFFECT_LIFETIME = 2f;
        private const float RAYCAST_OFFSET = 0.5f;
        private const float DEFAULT_CHAIN_RANGE = 5f;
        private const float DEFAULT_SPLIT_SCALE = 0.7f;
        private const float DEFAULT_CHAIN_DECAY = 0.8f;
        #endregion

        public bool IsInitialized => isInitialized;

        #region Initialization

        public void Initialize(MainSkillData main, SupportSkillData support, ITargetable targetable, bool explosive, GameObject hitEffectPrefab = null, float hitEffectScale = 1f)
        {
            mainSkill = main;
            supportSkill = support;
            target = targetable;
            isExplosive = explosive;
            hitPrefab = hitEffectPrefab;
            hitScale = hitEffectScale > 0 ? hitEffectScale : 1f;

            damage = SkillExecutor.CalculateDamage(main, support);
            speed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_SPEED;
            maxRange = main.range > 0 ? main.range : DEFAULT_RANGE;
            startPosition = transform.position;

            ApplySupportEffects(support);

            isInitialized = true;
        }

        /// <summary>
        /// 파생 투사체 초기화 (연쇄, 분열 등에서 생성된 투사체)
        /// </summary>
        public void InitializeDerived(
            MainSkillData main,
            SupportSkillData support,
            ITargetable targetable,
            bool explosive,
            float derivedDamage,
            int remainingPierce,
            int remainingChain,
            HashSet<int> existingHitTargets,
            GameObject hitEffectPrefab,
            float hitEffectScale)
        {
            mainSkill = main;
            supportSkill = support;
            target = targetable;
            isExplosive = explosive;
            hitPrefab = hitEffectPrefab;
            hitScale = hitEffectScale > 0 ? hitEffectScale : 1f;

            damage = derivedDamage;
            speed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_SPEED;
            maxRange = main.range > 0 ? main.range : DEFAULT_RANGE;
            startPosition = transform.position;

            // 서포트 효과 (남은 카운트 직접 설정)
            pierceCount = remainingPierce;
            chainCount = remainingChain;

            // 히트 목록 복사
            hitTargets = new HashSet<int>(existingHitTargets);

            // 유도 효과
            if (support != null && support.IsHomingSupport)
            {
                isHoming = true;
                homingStrength = support.homing_strength;
            }

            // 분열은 파생 투사체에서 비활성화
            splitCount = 0;

            isInitialized = true;
        }

        private void ApplySupportEffects(SupportSkillData support)
        {
            if (support == null) return;

            if (support.IsPierceSupport)
            {
                pierceCount = support.count;
            }

            if (support.IsChainSupport)
            {
                chainCount = support.count;
            }

            if (support.IsHomingSupport)
            {
                isHoming = true;
                homingStrength = support.homing_strength;
            }

            if (support.IsSplitSupport)
            {
                splitCount = support.count > 0 ? support.count : 3;
                splitSpreadAngle = support.spread_angle > 0 ? support.spread_angle : 60f;
                splitScaleMultiplier = support.scale_multiplier > 0 ? support.scale_multiplier : DEFAULT_SPLIT_SCALE;
            }
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (!isInitialized) return;

            UpdateHoming();

            float moveDistance = speed * Time.deltaTime;

            if (TryRaycastHit(moveDistance))
            {
                return;
            }

            transform.position += transform.forward * moveDistance;

            float traveled = Vector3.Distance(startPosition, transform.position);
            if (traveled > maxRange)
            {
                DestroyProjectile();
            }
        }

        private void UpdateHoming()
        {
            if (!isHoming || target == null || !target.IsAlive()) return;

            Vector3 targetCenter = TargetableUtils.GetAimPosition(target);
            Vector3 targetDir = (targetCenter - transform.position).normalized;

            if (targetDir.sqrMagnitude > 0.001f)
            {
                Vector3 newDir = Vector3.Lerp(transform.forward, targetDir, homingStrength * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(newDir);
            }
        }

        #endregion

        #region Collision Detection

        private bool TryRaycastHit(float moveDistance)
        {
            int monsterLayerMask = LayerMask.GetMask("Monster");
            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, moveDistance + RAYCAST_OFFSET, monsterLayerMask);

            if (hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                ITargetable hitTarget = TargetableUtils.GetTargetable(hit.collider);
                if (hitTarget == null) continue;

                int hitTargetId = hitTarget.GetTransform().GetInstanceID();
                if (hitTargets.Contains(hitTargetId)) continue;

                transform.position = hit.point;
                ProcessHitAsync(hitTarget).Forget();
                return true;
            }

            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized) return;
            if (!TargetableUtils.IsValidEnemy(other)) return;

            ITargetable hitTarget = TargetableUtils.GetTargetable(other);
            if (hitTarget == null || !hitTarget.IsAlive()) return;

            int hitTargetId = hitTarget.GetTransform().GetInstanceID();
            if (hitTargets.Contains(hitTargetId)) return;

            ProcessHitAsync(hitTarget).Forget();
        }

        #endregion

        #region Hit Processing (Container Pattern)

        /// <summary>
        /// 컨테이너 방식 충돌 처리
        /// 1단계: 데미지 적용 (단일 or 범위)
        /// 2단계: 서포트 효과 순차 적용
        /// 3단계: 투사체 종료 여부 결정
        /// </summary>
        private async UniTaskVoid ProcessHitAsync(ITargetable hitTarget)
        {
            // 충돌 중 추가 처리 방지
            isInitialized = false;

            int hitTargetId = hitTarget.GetTransform().GetInstanceID();
            hitTargets.Add(hitTargetId);

            // === 1단계: 데미지 적용 ===
            ApplyDamage(hitTarget);

            // === 2단계: 서포트 효과 적용 ===
            // 분열 체크 (분열 시 현재 투사체 종료)
            if (TryApplySplit())
            {
                await UniTask.Yield();
                DestroyProjectile();
                return;
            }

            // 관통 체크 (관통 시 계속 진행)
            if (TryApplyPierce())
            {
                isInitialized = true;
                return;
            }

            // 연쇄 체크 (연쇄 시 새 투사체 생성 후 종료)
            if (TryApplyChain(hitTarget))
            {
                await UniTask.Yield();
                DestroyProjectile();
                return;
            }

            // === 3단계: 일반 종료 ===
            SpawnHitEffect(transform.position);
            await UniTask.Yield();
            DestroyProjectile();
        }

        /// <summary>
        /// 데미지 적용 (단일 or 범위)
        /// </summary>
        private void ApplyDamage(ITargetable hitTarget)
        {
            if (isExplosive && mainSkill.aoe_radius > 0)
            {
                // 범위 데미지
                float radius = mainSkill.aoe_radius;
                AOERangeIndicator.Show(transform.position, radius, AOERangeIndicator.IndicatorType.Damage);
                SpawnHitEffect(transform.position);

                float explosionDamage = damage;
                if (supportSkill != null && supportSkill.explosion_ratio > 0)
                {
                    explosionDamage *= supportSkill.explosion_ratio;
                }

                // 범위 내 모든 적에게 데미지 (히트 목록 무관)
                TargetableUtils.ApplyDamageInRadius(transform.position, radius, explosionDamage);
            }
            else
            {
                // 단일 데미지
                hitTarget.TakeDamage(damage);
            }
        }

        #endregion

        #region Support Effects

        /// <summary>
        /// 분열 적용
        /// </summary>
        private bool TryApplySplit()
        {
            if (splitCount <= 0) return false;

            SpawnSplitProjectiles();
            return true;
        }

        /// <summary>
        /// 관통 적용
        /// </summary>
        private bool TryApplyPierce()
        {
            if (pierceCount <= 0) return false;

            pierceCount--;
            ApplyDecay();

            // 비폭발형이면 히트 이펙트 표시
            if (!isExplosive)
            {
                SpawnHitEffect(transform.position);
            }

            return true;
        }

        /// <summary>
        /// 연쇄 적용
        /// </summary>
        private bool TryApplyChain(ITargetable hitTarget)
        {
            if (chainCount <= 0) return false;

            chainCount--;

            Vector3 searchPos = TargetableUtils.GetAimPosition(hitTarget);
            float chainRange = supportSkill?.range > 0 ? supportSkill.range : DEFAULT_CHAIN_RANGE;
            ITargetable nextTarget = FindNextChainTarget(searchPos, chainRange);

            // 비폭발형이면 히트 이펙트 표시
            if (!isExplosive)
            {
                SpawnHitEffect(transform.position);
            }

            if (nextTarget != null)
            {
                float chainDamage = damage * (supportSkill?.chain_decay ?? DEFAULT_CHAIN_DECAY);
                SpawnChainProjectile(searchPos, nextTarget, chainDamage);
            }

            return true;
        }

        private void ApplyDecay()
        {
            if (supportSkill != null && supportSkill.chain_decay > 0)
            {
                damage *= supportSkill.chain_decay;
            }
        }

        #endregion

        #region Projectile Spawning

        private void SpawnSplitProjectiles()
        {
            float splitDamage = damage * (supportSkill?.chain_decay ?? 0.5f);

            for (int i = 0; i < splitCount; i++)
            {
                float angleOffset = 0f;
                if (splitCount > 1)
                {
                    angleOffset = Mathf.Lerp(-splitSpreadAngle / 2f, splitSpreadAngle / 2f, (float)i / (splitCount - 1));
                }

                Vector3 splitDir = Quaternion.Euler(0, angleOffset, 0) * transform.forward;
                SpawnDerivedProjectile(transform.position, splitDir, null, splitDamage, 0, 0, new HashSet<int>(), splitScaleMultiplier);
            }
        }

        private void SpawnChainProjectile(Vector3 spawnPos, ITargetable nextTarget, float chainDamage)
        {
            Vector3 targetCenter = TargetableUtils.GetAimPosition(nextTarget);
            Vector3 dir = (targetCenter - spawnPos).normalized;

            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector3.forward;
            }

            // 연쇄 투사체는 관통/연쇄 카운트 유지, 히트 목록 전달
            SpawnDerivedProjectile(spawnPos, dir, nextTarget, chainDamage, pierceCount, chainCount, hitTargets, 1f);
        }

        /// <summary>
        /// 파생 투사체 생성 (연쇄, 분열 공용)
        /// </summary>
        private void SpawnDerivedProjectile(
            Vector3 spawnPos,
            Vector3 direction,
            ITargetable nextTarget,
            float derivedDamage,
            int remainingPierce,
            int remainingChain,
            HashSet<int> existingHitTargets,
            float scaleMultiplier)
        {
            GameObject prefab = SkillExecutor.Instance.GetVFXPrefab(mainSkill.skill_id);
            if (prefab == null)
            {
                Debug.LogError($"[SkillProjectile] Projectile prefab not found for skill_id: {mainSkill.skill_id}");
                return;
            }

            GameObject projObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));
            projObj.layer = LayerMask.NameToLayer("Projectile");

            if (scaleMultiplier != 1f)
            {
                projObj.transform.localScale = transform.localScale * scaleMultiplier;
            }

            // Collider 설정
            Collider col = projObj.GetComponent<Collider>();
            if (col == null)
            {
                var sphereCol = projObj.AddComponent<SphereCollider>();
                sphereCol.radius = 0.5f;
                sphereCol.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            // Rigidbody 설정
            Rigidbody rb = projObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projObj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            // SkillProjectile 초기화
            SkillProjectile proj = projObj.GetComponent<SkillProjectile>();
            if (proj == null)
            {
                proj = projObj.AddComponent<SkillProjectile>();
            }

            proj.InitializeDerived(
                mainSkill,
                supportSkill,
                nextTarget,
                isExplosive,
                derivedDamage,
                remainingPierce,
                remainingChain,
                existingHitTargets,
                hitPrefab,
                hitScale
            );
        }

        private ITargetable FindNextChainTarget(Vector3 position, float range)
        {
            var targets = TargetableUtils.GetTargetsInRadius(position, range);

            ITargetable nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var t in targets)
            {
                if (!t.IsAlive()) continue;

                int targetId = t.GetTransform().GetInstanceID();
                if (hitTargets.Contains(targetId)) continue;

                float dist = Vector3.Distance(position, TargetableUtils.GetAimPosition(t));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = t;
                }
            }

            return nearest;
        }

        #endregion

        #region Utility

        private void SpawnHitEffect(Vector3 position)
        {
            if (hitPrefab == null) return;

            GameObject hitEffect = Instantiate(hitPrefab, position, Quaternion.identity);

            if (hitScale != 1f)
            {
                hitEffect.transform.localScale = Vector3.one * hitScale;
            }

            Destroy(hitEffect, HIT_EFFECT_LIFETIME);
        }

        private void DestroyProjectile()
        {
            isInitialized = false;
            Destroy(gameObject);
        }

        public void ForceDisable()
        {
            isInitialized = false;
            enabled = false;
        }

        #endregion
    }
}
