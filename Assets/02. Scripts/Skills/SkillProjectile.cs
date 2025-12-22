using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 투사체 - 이동, 충돌, 데미지 처리
    /// </summary>
    public class SkillProjectile : MonoBehaviour
    {
        #region Runtime Data
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;
        private ITargetable target;
        private bool isExplosive;
        private GameObject hitPrefab;
        #endregion

        #region State
        private bool isInitialized;
        private int pierceCount;
        private int chainCount;
        private float damage;
        private float speed;
        private float maxRange;
        private Vector3 startPosition;
        #endregion

        #region Homing
        private bool isHoming;
        private float homingStrength;
        #endregion

        #region Constants
        private const float DEFAULT_SPEED = 15f;
        private const float DEFAULT_RANGE = 20f;
        private const float HIT_EFFECT_LIFETIME = 2f;
        private const float RAYCAST_OFFSET = 0.5f;
        private const float DEFAULT_CHAIN_RANGE = 5f;
        private const float DEFAULT_SPLIT_SCALE = 0.7f;
        #endregion

        #region Split Support
        private bool isSplitProjectile;
        private int splitCount;
        private float splitSpreadAngle;
        private float splitScaleMultiplier;
        #endregion

        #region Chain Support
        private System.Collections.Generic.HashSet<int> chainHitTargets = new System.Collections.Generic.HashSet<int>();
        #endregion

        public void Initialize(MainSkillData main, SupportSkillData support, ITargetable targetable, bool explosive, GameObject hitEffectPrefab = null)
        {
            mainSkill = main;
            supportSkill = support;
            target = targetable;
            isExplosive = explosive;
            hitPrefab = hitEffectPrefab;

            // 기본 값 설정
            damage = SkillExecutor.CalculateDamage(main, support);
            speed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_SPEED;
            maxRange = main.range > 0 ? main.range : DEFAULT_RANGE;
            startPosition = transform.position;

            // 서포트 스킬 효과 적용
            ApplySupportEffects(support);

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
                isSplitProjectile = true;
                splitCount = support.count > 0 ? support.count : 3;
                splitSpreadAngle = support.spread_angle > 0 ? support.spread_angle : 60f;
                splitScaleMultiplier = support.scale_multiplier > 0 ? support.scale_multiplier : DEFAULT_SPLIT_SCALE;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            UpdateHoming();

            float moveDistance = speed * Time.deltaTime;

            // Raycast로 충돌 체크 (RaycastAll 사용하여 이미 맞은 적 통과)
            if (TryRaycastHit(moveDistance))
            {
                return;
            }

            // 이동
            transform.position += transform.forward * moveDistance;

            // 최대 사거리 체크
            float traveled = Vector3.Distance(startPosition, transform.position);
            if (traveled > maxRange)
            {
                DestroyProjectile();
            }
        }

        private void UpdateHoming()
        {
            if (!isHoming || target == null || !target.IsAlive()) return;

            // Collider 중심을 향해 유도
            Vector3 targetCenter = TargetableUtils.GetAimPosition(target);
            Vector3 targetDir = (targetCenter - transform.position).normalized;

            if (targetDir.sqrMagnitude > 0.001f)
            {
                Vector3 newDir = Vector3.Lerp(transform.forward, targetDir, homingStrength * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(newDir);
            }
        }

        private bool TryRaycastHit(float moveDistance)
        {
            int monsterLayerMask = LayerMask.GetMask("Monster");

            // RaycastAll로 모든 충돌 검사 (이미 맞은 적 통과)
            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, moveDistance + RAYCAST_OFFSET, monsterLayerMask);

            if (hits.Length == 0) return false;

            // 거리순 정렬
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // 이미 맞은 적이 아닌 첫 번째 적 찾기
            foreach (var hit in hits)
            {
                ITargetable hitTarget = TargetableUtils.GetTargetable(hit.collider);
                if (hitTarget == null) continue;

                int hitTargetId = hitTarget.GetTransform().GetInstanceID();
                if (chainHitTargets.Contains(hitTargetId))
                {
                    // 이미 맞은 적은 스킵
                    continue;
                }

                // 유효한 타겟에 충돌
                transform.position = hit.point;
                ProcessHit(hit.collider);
                return true;
            }

            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized) return;
            if (!TargetableUtils.IsValidEnemy(other)) return;

            ProcessHit(other);
        }

        /// <summary>
        /// 충돌 처리 (Raycast, Trigger 공용)
        /// </summary>
        private void ProcessHit(Collider other)
        {
            ITargetable hitTarget = TargetableUtils.GetTargetable(other);
            if (hitTarget == null || !hitTarget.IsAlive()) return;

            // 이미 맞은 적인지 체크 (연쇄 스킬에서 중복 타격 방지)
            int hitTargetId = hitTarget.GetTransform().GetInstanceID();
            if (chainHitTargets.Contains(hitTargetId)) return;

            // 데미지 적용
            hitTarget.TakeDamage(damage);

            // 폭발형 처리
            if (isExplosive && mainSkill.aoe_radius > 0)
            {
                ExplodeAsync().Forget();
                return;
            }

            // 분열 처리
            if (TrySplit()) return;

            // 관통 처리
            if (TryPierce()) return;

            // 연쇄 처리
            if (TryChain(hitTarget)) return;

            // 일반 충돌
            SpawnHitEffect(transform.position);
            DestroyProjectile();
        }

        private bool TrySplit()
        {
            if (!isSplitProjectile || splitCount <= 0) return false;

            SpawnSplitProjectiles();
            DestroyProjectile();
            return true;
        }

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
                SpawnSplitProjectile(splitDir, splitDamage);
            }
        }

        private void SpawnSplitProjectile(Vector3 direction, float splitDamage)
        {
            // VFX 프리팹 가져오기 (현재 투사체와 동일)
            GameObject splitPrefab = gameObject;

            // 새 투사체 생성
            GameObject splitObj = Instantiate(splitPrefab, transform.position, Quaternion.LookRotation(direction));
            splitObj.transform.localScale = transform.localScale * splitScaleMultiplier;

            SkillProjectile splitProjectile = splitObj.GetComponent<SkillProjectile>();
            if (splitProjectile != null)
            {
                // 분열 투사체는 더 이상 분열하지 않음
                splitProjectile.InitializeAsSplit(mainSkill, damage * (supportSkill?.chain_decay ?? 0.5f), hitPrefab);
            }
        }

        /// <summary>
        /// 분열 투사체 초기화 (분열 서포트 없이)
        /// </summary>
        public void InitializeAsSplit(MainSkillData main, float splitDamage, GameObject hitEffectPrefab)
        {
            mainSkill = main;
            supportSkill = null;
            target = null;
            isExplosive = false;
            hitPrefab = hitEffectPrefab;

            damage = splitDamage;
            speed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_SPEED;
            maxRange = main.range > 0 ? main.range : DEFAULT_RANGE;
            startPosition = transform.position;

            isSplitProjectile = false;
            isInitialized = true;
        }

        private bool TryPierce()
        {
            if (pierceCount <= 0) return false;

            pierceCount--;
            ApplyDecay();
            return true;
        }

        private bool TryChain(ITargetable hitTarget)
        {
            if (chainCount <= 0) return false;

            // 현재 맞은 타겟을 히트 목록에 추가 (이미 ProcessHit에서 체크했지만 한번 더)
            int hitTargetId = hitTarget.GetTransform().GetInstanceID();
            chainHitTargets.Add(hitTargetId);

            // 연쇄 처리 시작 전에 현재 투사체의 추가 충돌 방지
            isInitialized = false;

            ChainToNextTarget(hitTarget).Forget();
            return true;
        }

        private void ApplyDecay()
        {
            if (supportSkill != null && supportSkill.chain_decay > 0)
            {
                damage *= supportSkill.chain_decay;
            }
        }

        private async UniTaskVoid ExplodeAsync()
        {
            float radius = mainSkill.aoe_radius;

            // 폭발 이펙트 스폰
            SpawnHitEffect(transform.position);

            // 폭발 데미지 계산
            float explosionDamage = damage;
            if (supportSkill != null && supportSkill.explosion_ratio > 0)
            {
                explosionDamage *= supportSkill.explosion_ratio;
            }

            // 범위 내 적에게 데미지
            TargetableUtils.ApplyDamageInRadius(transform.position, radius, explosionDamage);

            await UniTask.Yield();
            DestroyProjectile();
        }

        private async UniTaskVoid ChainToNextTarget(ITargetable hitTarget)
        {
            chainCount--;

            // 맞은 적의 Collider 중심에서 다음 타겟 검색
            Vector3 hitTargetCenter = TargetableUtils.GetAimPosition(hitTarget);

            float chainRange = supportSkill?.range > 0 ? supportSkill.range : DEFAULT_CHAIN_RANGE;
            ITargetable nextTarget = FindNextChainTarget(hitTargetCenter, chainRange);

            // 히트 이펙트 스폰
            SpawnHitEffect(transform.position);

            if (nextTarget != null)
            {
                // 맞은 적의 Collider 중심에서 새 투사체 스폰
                // 주의: nextTarget은 히트 목록에 미리 추가하지 않음 - 새 투사체가 맞았을 때 자체적으로 추가
                SpawnChainProjectile(hitTargetCenter, nextTarget, chainCount);
            }

            // 현재 투사체는 파괴
            await UniTask.Yield();
            DestroyProjectile();
        }

        private ITargetable FindNextChainTarget(Vector3 position, float range)
        {
            var targets = TargetableUtils.GetTargetsInRadius(position, range);

            ITargetable nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var t in targets)
            {
                if (!t.IsAlive()) continue;

                // 이미 맞은 적은 제외
                int targetId = t.GetTransform().GetInstanceID();
                if (chainHitTargets.Contains(targetId)) continue;

                float dist = Vector3.Distance(position, TargetableUtils.GetAimPosition(t));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = t;
                }
            }

            return nearest;
        }

        private void SpawnChainProjectile(Vector3 spawnPos, ITargetable nextTarget, int remainingChains)
        {
            // 연쇄 데미지 감쇠 적용
            float chainDamage = damage * (supportSkill?.chain_decay ?? 0.8f);

            // 타겟 Collider 중심
            Vector3 targetCenter = TargetableUtils.GetAimPosition(nextTarget);

            // 스폰 → 타겟 방향 (Collider 중심 간 직선)
            Vector3 dir = (targetCenter - spawnPos).normalized;

            // 방향이 0이면 (같은 위치) 앞으로
            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector3.forward;
            }

            // VFXDatabase에서 원본 prefab 가져오기
            GameObject prefab = SkillExecutor.Instance.GetVFXPrefab(mainSkill.skill_id);
            if (prefab == null)
            {
                Debug.LogError($"[SkillProjectile] Chain projectile prefab not found for skill_id: {mainSkill.skill_id}");
                return;
            }

            // prefab에서 새 투사체 생성 (깨끗한 상태)
            GameObject chainObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(dir));

            // Layer 설정 (Physics 충돌을 위해 필수)
            chainObj.layer = LayerMask.NameToLayer("Projectile");

            // Collider 설정
            Collider col = chainObj.GetComponent<Collider>();
            if (col == null)
            {
                var sphereCol = chainObj.AddComponent<SphereCollider>();
                sphereCol.radius = 0.5f;
                sphereCol.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            // Rigidbody 설정
            Rigidbody rb = chainObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = chainObj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            // SkillProjectile 컴포넌트 추가 및 초기화
            SkillProjectile chainProjectile = chainObj.GetComponent<SkillProjectile>();
            if (chainProjectile == null)
            {
                chainProjectile = chainObj.AddComponent<SkillProjectile>();
            }

            chainProjectile.InitializeAsChain(mainSkill, supportSkill, nextTarget, chainDamage, remainingChains, hitPrefab, chainHitTargets);
        }

        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 연쇄 투사체 초기화
        /// </summary>
        public void InitializeAsChain(MainSkillData main, SupportSkillData support, ITargetable targetable, float chainDamage, int remainingChains, GameObject hitEffectPrefab, System.Collections.Generic.HashSet<int> hitTargets)
        {
            mainSkill = main;
            supportSkill = support;
            target = targetable;
            isExplosive = false;
            hitPrefab = hitEffectPrefab;

            damage = chainDamage;
            speed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_SPEED;
            maxRange = main.range > 0 ? main.range : DEFAULT_RANGE;
            startPosition = transform.position;

            chainCount = remainingChains;

            // 이미 맞은 적 목록 복사
            chainHitTargets = new System.Collections.Generic.HashSet<int>(hitTargets);

            // 유도 효과는 서포트 스킬에 유도가 있을 때만 적용
            if (support != null && support.IsHomingSupport)
            {
                isHoming = true;
                homingStrength = support.homing_strength;
            }

            isInitialized = true;
        }

        private void DestroyProjectile()
        {
            isInitialized = false;
            Destroy(gameObject);
        }

        /// <summary>
        /// 강제 비활성화 (연쇄 투사체 생성 시 기존 컴포넌트 즉시 비활성화용)
        /// </summary>
        public void ForceDisable()
        {
            isInitialized = false;
            enabled = false;
        }

        private void SpawnHitEffect(Vector3 position)
        {
            if (hitPrefab == null) return;

            GameObject hitEffect = Instantiate(hitPrefab, position, Quaternion.identity);
            Destroy(hitEffect, HIT_EFFECT_LIFETIME);
        }
    }
}
