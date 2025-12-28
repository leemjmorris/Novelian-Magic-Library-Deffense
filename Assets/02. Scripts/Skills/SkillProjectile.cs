using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Audio;
using NovelianMagicLibraryDefense.Managers;
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
        private Character casterCharacter; // 캐스터 캐릭터 (스탯 적용용)
        private bool isCritical; // 치명타 여부
        #endregion

        #region State
        private bool isInitialized;
        private float damage;
        private float speed;
        private float maxRange;
        private Vector3 startPosition;
        #endregion

        #region Support State
        private int chainCount;
        private float chainDecay;
        private float chainRange;
        private int pierceCount;
        private int splitCount;
        private float splitSpreadAngle;
        private float splitScaleMultiplier;
        private bool isHoming;
        private float homingStrength;
        private bool isBouncing;
        private float bounceEndTime;
        private float bounceDecay;
        private Vector3 moveDirection;
        #endregion

        #region Hit Tracking
        private HashSet<int> hitTargets = new HashSet<int>();
        #endregion

        #region Constants
        private const float DEFAULT_SPEED = 15f;
        private const float DEFAULT_RANGE = 20f;
        private const float HIT_EFFECT_LIFETIME = 2f;
        private const float RAYCAST_OFFSET = 0.5f;
        private const float DEFAULT_SPLIT_SCALE = 0.7f;
        private const float DEFAULT_SPLIT_DECAY = 0.5f;
        private const float DEFAULT_CHAIN_DECAY = 0.8f;
        private const float DEFAULT_CHAIN_RANGE = 5f;
        private const float DEFAULT_BOUNCE_DURATION = 10f;
        private const float DEFAULT_BOUNCE_DECAY = 0.9f;
        private const float BOUNCE_HIT_COOLDOWN = 0.1f;
        private const float SPLIT_SPAWN_OFFSET = 1.5f;
        private const float DEFAULT_DOT_DAMAGE_RATIO = 0.1f;
        private const float DEFAULT_DOT_DURATION = 5f;
        private const float DEFAULT_DOT_TICK_INTERVAL = 0.5f;
        private const float DEFAULT_CC_DURATION = 3f;
        #endregion

        public bool IsInitialized => isInitialized;

        #region Initialization

        public void Initialize(MainSkillData main, SupportSkillData support, ITargetable targetable, bool explosive, GameObject hitEffectPrefab = null, float hitEffectScale = 1f, Character caster = null)
        {
            mainSkill = main;
            supportSkill = support;
            target = targetable;
            isExplosive = explosive;
            hitPrefab = hitEffectPrefab;
            hitScale = hitEffectScale > 0 ? hitEffectScale : 1f;
            casterCharacter = caster;

            // 데미지 계산 (Character 스탯 적용, 치명타 판정 포함)
            damage = SkillExecutor.CalculateDamage(main, support, caster, out isCritical);

            // 투사체 속도 (Character 스탯 적용)
            float baseSpeed = main.projectile_speed > 0 ? main.projectile_speed : DEFAULT_SPEED;
            if (caster != null)
            {
                float speedModifier = caster.GetProjectileSpeedModifier();
                speed = baseSpeed * (1f + speedModifier / 100f);
            }
            else
            {
                speed = baseSpeed;
            }

            // 사거리 (Character 스탯 적용)
            float baseRange = main.range > 0 ? main.range : DEFAULT_RANGE;
            if (caster != null)
            {
                float rangeModifier = caster.GetRangeModifier();
                maxRange = baseRange * (1f + rangeModifier / 100f);
            }
            else
            {
                maxRange = baseRange;
            }

            startPosition = transform.position;

            ApplySupportEffects(support);

            isInitialized = true;
        }

        /// <summary>
        /// 파생 투사체 초기화 (분열, 연쇄 등에서 생성된 투사체)
        /// </summary>
        public void InitializeDerived(
            MainSkillData main,
            SupportSkillData support,
            ITargetable targetable,
            bool explosive,
            float derivedDamage,
            int remainingPierce,
            HashSet<int> existingHitTargets,
            GameObject hitEffectPrefab,
            float hitEffectScale,
            int remainingChain = 0,
            float derivedChainDecay = 0f,
            float derivedChainRange = 0f)
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

            // 연쇄 효과 (남은 카운트 직접 설정)
            chainCount = remainingChain;
            chainDecay = derivedChainDecay > 0 ? derivedChainDecay : DEFAULT_CHAIN_DECAY;
            chainRange = derivedChainRange > 0 ? derivedChainRange : DEFAULT_CHAIN_RANGE;

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

            if (support.IsChainSupport)
            {
                chainCount = support.count > 0 ? support.count : 3;
                chainDecay = support.chain_decay > 0 ? support.chain_decay : DEFAULT_CHAIN_DECAY;
                chainRange = support.range > 0 ? support.range : DEFAULT_CHAIN_RANGE;
            }

            if (support.IsPierceSupport)
            {
                pierceCount = support.count;
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

            if (support.IsBounceSupport)
            {
                isBouncing = true;
                float duration = support.duration > 0 ? support.duration : DEFAULT_BOUNCE_DURATION;
                bounceEndTime = Time.time + duration;
                bounceDecay = support.chain_decay > 0 ? support.chain_decay : DEFAULT_BOUNCE_DECAY;
                moveDirection = transform.forward;
            }
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (!isInitialized) return;

            // Bounce 모드
            if (isBouncing)
            {
                UpdateBounce();
                return;
            }

            UpdateHoming();

            float moveDistance = speed * Time.deltaTime;

            if (TryRaycastHit(moveDistance))
            {
                return;
            }

            Vector3 newPos = transform.position + transform.forward * moveDistance;
            newPos.y = startPosition.y; // Y축 고정
            transform.position = newPos;

            float traveled = Vector3.Distance(startPosition, transform.position);
            if (traveled > maxRange)
            {
                DestroyProjectile();
            }
        }

        private void UpdateBounce()
        {
            // 시간 초과 시 소멸
            if (Time.time > bounceEndTime)
            {
                DestroyProjectile();
                return;
            }

            float moveDistance = speed * Time.deltaTime;

            // 적 관통 충돌 체크
            TryBounceHit(moveDistance);

            // 이동
            Vector3 nextPos = transform.position + moveDirection * moveDistance;

            // 화면 경계 반사 체크
            CheckBoundsReflection(ref nextPos);

            nextPos.y = startPosition.y; // Y축 고정
            transform.position = nextPos;
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        private void TryBounceHit(float moveDistance)
        {
            int monsterLayerMask = LayerMask.GetMask("Monster");
            RaycastHit[] hits = Physics.RaycastAll(transform.position, moveDirection, moveDistance + RAYCAST_OFFSET, monsterLayerMask);

            foreach (var hit in hits)
            {
                ITargetable hitTarget = TargetableUtils.GetTargetable(hit.collider);
                if (hitTarget == null || !hitTarget.IsAlive()) continue;

                int hitTargetId = hitTarget.GetTransform().GetInstanceID();
                if (hitTargets.Contains(hitTargetId)) continue;

                // 데미지 적용 (관통이므로 멈추지 않음)
                hitTarget.TakeDamage(damage);
                hitTargets.Add(hitTargetId);
                SpawnHitEffect(hit.point);

                // 상태효과 적용 (CC/DOT)
                ApplyStatusEffects(hitTarget);

                // 데미지 감소
                damage *= bounceDecay;
            }
        }

        private void CheckBoundsReflection(ref Vector3 nextPos)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // 현재 위치를 뷰포트 좌표로 변환
            Vector3 viewportPos = mainCam.WorldToViewportPoint(nextPos);

            bool reflected = false;

            // 좌우 경계 반사
            if (viewportPos.x < 0f || viewportPos.x > 1f)
            {
                moveDirection.x = -moveDirection.x;
                viewportPos.x = Mathf.Clamp(viewportPos.x, 0.01f, 0.99f);
                reflected = true;
            }

            // 상하 경계 반사 (Y축 → Z축 월드 좌표)
            if (viewportPos.y < 0f || viewportPos.y > 1f)
            {
                moveDirection.z = -moveDirection.z;
                viewportPos.y = Mathf.Clamp(viewportPos.y, 0.01f, 0.99f);
                reflected = true;
            }

            if (reflected)
            {
                // 히트 목록 초기화 (같은 적 다시 타격 가능)
                hitTargets.Clear();
                nextPos = mainCam.ViewportToWorldPoint(viewportPos);
                nextPos.y = transform.position.y; // Y축 유지
            }
        }

        private void UpdateHoming()
        {
            if (!isHoming || target == null || !target.IsAlive()) return;

            Vector3 targetCenter = TargetableUtils.GetAimPosition(target);
            // Y축 무시하고 XZ 평면에서만 방향 계산
            Vector3 flatTarget = new Vector3(targetCenter.x, startPosition.y, targetCenter.z);
            Vector3 targetDir = (flatTarget - transform.position).normalized;

            if (targetDir.sqrMagnitude > 0.001f)
            {
                Vector3 newDir = Vector3.Lerp(transform.forward, targetDir, homingStrength * Time.deltaTime);
                newDir.y = 0f; // Y축 방향 제거
                if (newDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(newDir.normalized);
                }
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

            // 연쇄 체크 (연쇄 시 다음 타겟으로 이동)
            if (TryApplyChain(hitTarget))
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
            // 히트 사운드 재생
            PlayHitSound();

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

                // 범위 내 모든 적에게 데미지 + 상태효과
                TargetableUtils.ApplyDamageInRadius(transform.position, radius, explosionDamage, ApplyStatusEffects);
            }
            else
            {
                // 단일 데미지
                hitTarget.TakeDamage(damage);
                // 상태효과 적용
                ApplyStatusEffects(hitTarget);
            }
        }

        /// <summary>
        /// CC/DOT 상태효과 적용
        /// </summary>
        private void ApplyStatusEffects(ITargetable target)
        {
            if (supportSkill == null) return;

            Transform targetTransform = target.GetTransform();
            if (targetTransform == null) return;

            // CC 효과 (Stun)
            if (supportSkill.IsCCSupport)
            {
                ApplyCCEffect(targetTransform);
            }

            // DOT 효과
            if (supportSkill.IsDOTSupport)
            {
                ApplyDOTEffect(targetTransform);
            }
        }

        /// <summary>
        /// CC 효과 적용 (Stun/Slow)
        /// </summary>
        private void ApplyCCEffect(Transform targetTransform)
        {
            float ccDuration = supportSkill.duration > 0 ? supportSkill.duration : DEFAULT_CC_DURATION;
            CCType ccType = supportSkill.GetCCType();

            Debug.Log($"[SkillProjectile] CC 효과 시도: {targetTransform.name}, 타입: {ccType}, 지속시간: {ccDuration:F1}초");

            // 일반 몬스터
            if (targetTransform.CompareTag(Tag.Monster))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                if (monster != null)
                {
                    if (ccType == CCType.Slow)
                    {
                        monster.ApplySlow(supportSkill.slow_rate, ccDuration);
                    }
                    else
                    {
                        monster.ApplyDizzy(ccDuration);
                    }
                }
                else
                {
                    Debug.LogWarning($"[SkillProjectile] Monster 컴포넌트 없음: {targetTransform.name}");
                }
            }
            // 보스 몬스터
            else if (targetTransform.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                if (boss != null)
                {
                    boss.ApplyStun(ccDuration);
                }
            }
            else
            {
                Debug.LogWarning($"[SkillProjectile] CC 대상이 Monster/BossMonster 태그가 아님: {targetTransform.name}, Tag: {targetTransform.tag}");
            }
        }

        /// <summary>
        /// DOT 효과 적용
        /// </summary>
        private void ApplyDOTEffect(Transform targetTransform)
        {
            // DOT 파라미터 설정
            float dotDamage = supportSkill.tick_damage > 0
                ? supportSkill.tick_damage
                : damage * DEFAULT_DOT_DAMAGE_RATIO;
            float dotInterval = supportSkill.tick_interval > 0
                ? supportSkill.tick_interval
                : DEFAULT_DOT_TICK_INTERVAL;
            float dotDuration = supportSkill.duration > 0
                ? supportSkill.duration
                : DEFAULT_DOT_DURATION;

            // 일반 몬스터
            if (targetTransform.CompareTag(Tag.Monster))
            {
                Monster monster = targetTransform.GetComponent<Monster>();
                monster?.ApplyDOT(DOTType.Poison, dotDamage, dotInterval, dotDuration, null);
            }
            // 보스 몬스터
            else if (targetTransform.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = targetTransform.GetComponent<BossMonster>();
                boss?.ApplyDOT(DOTType.Poison, dotDamage, dotInterval, dotDuration, null);
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
        /// 연쇄 적용
        /// </summary>
        private bool TryApplyChain(ITargetable currentTarget)
        {
            if (chainCount <= 0) return false;

            // 다음 연쇄 타겟 찾기
            ITargetable nextTarget = FindNextChainTarget(currentTarget);
            if (nextTarget == null) return false;

            chainCount--;
            float chainedDamage = damage * chainDecay;

            // 연쇄 투사체 스폰
            SpawnChainProjectile(nextTarget, chainedDamage);

            // 현재 투사체 히트 이펙트
            SpawnHitEffect(transform.position);

            return true;
        }

        /// <summary>
        /// 관통 적용
        /// </summary>
        private bool TryApplyPierce()
        {
            if (pierceCount <= 0) return false;

            pierceCount--;

            // 비폭발형이면 히트 이펙트 표시
            if (!isExplosive)
            {
                SpawnHitEffect(transform.position);
            }

            return true;
        }


        #endregion

        #region Projectile Spawning

        /// <summary>
        /// 다음 연쇄 타겟 찾기
        /// </summary>
        private ITargetable FindNextChainTarget(ITargetable currentTarget)
        {
            Vector3 currentPos = currentTarget.GetTransform().position;
            int monsterLayerMask = LayerMask.GetMask("Monster");

            Collider[] colliders = Physics.OverlapSphere(currentPos, chainRange, monsterLayerMask);

            ITargetable nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                ITargetable potentialTarget = TargetableUtils.GetTargetable(col);
                if (potentialTarget == null || !potentialTarget.IsAlive()) continue;

                int targetId = potentialTarget.GetTransform().GetInstanceID();
                if (hitTargets.Contains(targetId)) continue;

                float distance = Vector3.Distance(currentPos, potentialTarget.GetTransform().position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = potentialTarget;
                }
            }

            return nearestTarget;
        }

        /// <summary>
        /// 연쇄 투사체 생성
        /// </summary>
        private void SpawnChainProjectile(ITargetable nextTarget, float chainedDamage)
        {
            Vector3 spawnPos = transform.position;
            Vector3 targetPos = TargetableUtils.GetAimPosition(nextTarget);
            Vector3 direction = (targetPos - spawnPos).normalized;

            SpawnDerivedProjectile(spawnPos, direction, nextTarget, chainedDamage, 0, hitTargets, 1f, chainCount, chainDecay, chainRange);
        }

        private void SpawnSplitProjectiles()
        {
            float splitDamage = damage * DEFAULT_SPLIT_DECAY;

            for (int i = 0; i < splitCount; i++)
            {
                float angleOffset = 0f;
                if (splitCount > 1)
                {
                    angleOffset = Mathf.Lerp(-splitSpreadAngle / 2f, splitSpreadAngle / 2f, (float)i / (splitCount - 1));
                }

                Vector3 splitDir = Quaternion.Euler(0, angleOffset, 0) * transform.forward;
                // 충돌 지점에서 진행 방향으로 오프셋하여 스폰 (몬스터 내부 스폰 방지)
                Vector3 spawnPos = transform.position + splitDir * SPLIT_SPAWN_OFFSET;
                spawnPos.y = startPosition.y; // Y축 고정
                // hitTargets 상속하여 방금 맞힌 적 즉시 재타격 방지
                SpawnDerivedProjectile(spawnPos, splitDir, null, splitDamage, 0, new HashSet<int>(hitTargets), splitScaleMultiplier);
            }
        }

        /// <summary>
        /// 파생 투사체 생성 (분열용)
        /// </summary>
        private void SpawnDerivedProjectile(
            Vector3 spawnPos,
            Vector3 direction,
            ITargetable nextTarget,
            float derivedDamage,
            int remainingPierce,
            HashSet<int> existingHitTargets,
            float scaleMultiplier,
            int remainingChain = 0,
            float derivedChainDecay = 0f,
            float derivedChainRange = 0f)
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
                existingHitTargets,
                hitPrefab,
                hitScale,
                remainingChain,
                derivedChainDecay,
                derivedChainRange
            );
        }

        #endregion

        #region Utility

        /// <summary>
        /// 히트 사운드 재생
        /// </summary>
        private void PlayHitSound()
        {
            if (mainSkill == null) return;

            string hitSoundKey = SkillSoundHelper.GetHitSoundKey(mainSkill.skill_id);
            Debug.Log($"[HitSound] skillId={mainSkill.skill_id}, hitSoundKey={hitSoundKey ?? "null"}, AudioManager={AudioManager.Instance != null}");
            if (!string.IsNullOrEmpty(hitSoundKey))
            {
                AudioManager.Instance?.PlaySFX(hitSoundKey);
            }
        }

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
