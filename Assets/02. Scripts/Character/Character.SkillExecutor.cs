//LMJ : Character partial class - Skill Execution (Projectile, AOE, Channeling, Buff, Trap/Mine)
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using NovelianMagicLibraryDefense.Managers;

    /// <summary>
    /// 캐릭터 스킬 실행
    /// - 투사체 발사 (일반/다이너마이트/전설의 지팡이)
    /// - AOE 스킬
    /// - 채널링 스킬
    /// - 버프 스킬
    /// - 트랩/지뢰 설치
    /// - 즉사 스킬
    /// </summary>
    public partial class Character
    {
        #region Projectile Skills

        //LMJ : Launch projectile(s) - extracted from old TryAttack for reuse
        private void LaunchProjectile(ITargetable target)
        {
            if (target == null || basicAttackData == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Flatten target position to horizontal plane (수평 발사)
            targetPos.y = spawnPos.y;

            // Get projectile prefab from database
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;
            GameObject hitEffectPrefab = basicAttackPrefabs?.hitEffectPrefab;

            // Launch projectile(s) - 다중 발사 지원 (add_projectiles)
            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = GameManager.Instance.Pool;

                // 발사체 개수 계산 (CSV의 projectile_count + 서포트 추가)
                // 파편화(40002)는 명중 시 분열이므로 발사 시에는 서포트 추가분 제외
                int projectileCount = basicAttackData.projectile_count;
                if (projectileCount <= 0) projectileCount = 1; // 최소 1발 보장
                if (supportData != null && supportData.support_id != 40002)
                {
                    projectileCount += supportData.add_projectiles;
                }

                // 연속 발사 (기관총처럼 순차 발사)
                if (projectileCount > 1)
                {
                    // attackCts.Token 전달하여 씬 전환 시 안전하게 취소
                    var ct = attackCts?.Token ?? System.Threading.CancellationToken.None;
                    FireBurstProjectilesAsync(pool, spawnPos, targetPos, projectileCount, ct).Forget();
                }
                else
                {
                    // 1발만 발사하는 경우 즉시 발사
                    SkillProjectile projectile = pool.Spawn<SkillProjectile>(spawnPos);
                    projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime, FinalDamage, basicAttackSkillId, supportSkillId, GetDisplayCritChance(), GetDisplayCritMultiplier(), GetGenre());
                    Debug.Log($"[Character] Fired 1 projectile {basicAttackData.skill_name} (Damage: {FinalDamage:F1}, Speed: {FinalProjectileSpeed:F1})");
                }
            }
            // Instant attack (no projectile)
            else
            {
                // Hit effect at target collider center
                Collider targetCol = target.GetTransform().GetComponent<Collider>();
                Vector3 hitPos = targetCol != null ? targetCol.bounds.center : target.GetPosition();

                // SkillEffectManager를 통해 히트 이펙트 스폰
                var effectManager = SkillEffectManager.Instance;
                if (effectManager != null && basicAttackSkillId > 0)
                {
                    effectManager.SpawnHitEffect(basicAttackSkillId, hitPos);
                }
                else if (hitEffectPrefab != null)
                {
                    // Fallback: SkillEffectManager가 없으면 직접 스폰
                    GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                    float hitScale = basicAttackPrefabs?.GetHitScale() ?? 1f;
                    hitEffect.transform.localScale = Vector3.one * hitScale;
                    UnityEngine.Object.Destroy(hitEffect, 2f);
                }

                // Apply damage (치명타 적용)
                var (finalDmg, isCrit) = DamageCalculator.CalculateCriticalDamage(FinalDamage, GetDisplayCritChance(), GetDisplayCritMultiplier());
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    if (monster != null)
                    {
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), monster.GetGenre());
                        monster.TakeDamage(finalDmg * genreMultiplier, isCrit);
                    }
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    if (boss != null)
                    {
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), boss.GetGenre());
                        boss.TakeDamage(finalDmg * genreMultiplier, isCrit);
                    }
                }
            }
        }

        //LMJ : Launch dynamite projectile (던져서 N초 후 폭발)
        private void LaunchDynamiteProjectile(ITargetable target)
        {
            if (target == null || basicAttackData == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // 다이너마이트는 타겟 위치로 던지기 (Y축 유지하지 않음 - 포물선 이동)
            // targetPos.y = spawnPos.y; // 수평 발사 대신 포물선 이동을 위해 주석처리

            // Get projectile prefab from database
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;

            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = GameManager.Instance.Pool;

                // Spawn projectile
                SkillProjectile projectile = pool.Spawn<SkillProjectile>(spawnPos);

                // Launch with dynamite parameters
                // skill_lifetime을 폭발 딜레이로 사용, Projectile에서 다이너마이트 처리
                float projectileSpeed = basicAttackData.projectile_speed > 0 ? basicAttackData.projectile_speed : 10f;
                float lifetime = basicAttackData.skill_lifetime > 0 ? basicAttackData.skill_lifetime + 1f : 6f; // 퓨즈 시간 + 여유

                projectile.Launch(spawnPos, targetPos, projectileSpeed, lifetime, FinalDamage, basicAttackSkillId, supportSkillId, GetDisplayCritChance(), GetDisplayCritMultiplier(), GetGenre());
                Debug.Log($"[Character] Launched Dynamite projectile: speed={projectileSpeed}, fuseTime={basicAttackData.skill_lifetime}, damage={FinalDamage:F1}");
            }
            else
            {
                Debug.LogWarning("[Character] LaunchDynamiteProjectile: No projectile prefab found, falling back to instant AOE");
                UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
            }
        }

        //LMJ : Launch legendary staff projectile (일직선 이동하며 경로상 AOE 데미지)
        private void LaunchLegendaryStaffProjectile(ITargetable target)
        {
            if (target == null || basicAttackData == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // 수평 발사 (Y축 동일하게)
            targetPos.y = spawnPos.y;

            // Get projectile prefab from database
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;

            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = GameManager.Instance.Pool;

                // Spawn projectile
                SkillProjectile projectile = pool.Spawn<SkillProjectile>(spawnPos);

                // Launch with legendary staff parameters
                // range를 투사체 속도로 사용 (CSV에 projectile_speed가 0)
                float projectileSpeed = basicAttackData.projectile_speed > 0 ? basicAttackData.projectile_speed : 20f;
                float lifetime = basicAttackData.range / projectileSpeed + 1f; // 사거리까지 이동 시간 + 여유

                projectile.Launch(spawnPos, targetPos, projectileSpeed, lifetime, FinalDamage, basicAttackSkillId, supportSkillId, GetDisplayCritChance(), GetDisplayCritMultiplier(), GetGenre());
                Debug.Log($"[Character] Launched LegendaryStaff projectile: speed={projectileSpeed}, range={basicAttackData.range}, aoeRadius={basicAttackData.aoe_radius}, damage={FinalDamage:F1}");
            }
            else
            {
                Debug.LogWarning("[Character] LaunchLegendaryStaffProjectile: No projectile prefab found, falling back to instant AOE");
                UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
            }
        }

        //LMJ : Launch active skill projectile(s)
        private void LaunchActiveProjectile(ITargetable target, bool isDynamite = false, bool isLegendaryStaff = false, bool isTimeBomb = false, bool isBoomerang = false)
        {
            if (target == null || activeSkillData == null) return;

            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            var pool = GameManager.Instance.Pool;
            int projectileCount = FinalActiveProjectileCount;
            float spreadAngle = 15f;

            for (int i = 0; i < projectileCount; i++)
            {
                float angleOffset = (i - (projectileCount - 1) / 2f) * spreadAngle;
                Vector3 direction = (targetPos - spawnPos).normalized;
                Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
                Vector3 spreadDirection = rotation * direction;
                Vector3 spreadTargetPos = spawnPos + spreadDirection * 1000f;

                Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                projectile.Launch(spawnPos, spreadTargetPos, FinalActiveProjectileSpeed, FinalActiveProjectileLifetime, FinalActiveDamage, activeSkillId, supportSkillId, GetDisplayCritChance(), GetDisplayCritMultiplier(), GetGenre());
            }

            Debug.Log($"[Character] Active Projectile: {activeSkillData.skill_name} x{projectileCount} (Damage: {FinalActiveDamage:F1})");
        }

        /// <summary>
        /// 연속 발사 (기관총처럼 순차 발사)
        /// projectile_count가 2 이상인 스킬에서 사용
        /// CancellationToken을 받아 씬 전환 시 안전하게 취소됨
        /// </summary>
        private async UniTaskVoid FireBurstProjectilesAsync(ObjectPoolManager pool, Vector3 spawnPos, Vector3 targetPos, int projectileCount, System.Threading.CancellationToken ct = default)
        {
            const float BURST_INTERVAL = 0.1f; // 연속 발사 간격 (100ms)

            Debug.Log($"[Character] Burst fire started: {projectileCount} projectiles ({basicAttackData.skill_name})");

            for (int i = 0; i < projectileCount; i++)
            {
                // 취소 요청 확인 (씬 전환 등)
                if (ct.IsCancellationRequested)
                {
                    Debug.Log($"[Character] Burst fire cancelled at {i}/{projectileCount}");
                    return;
                }

                // 발사 시점에 타겟 방향 재계산 (동일한 방향으로 연속 발사)
                SkillProjectile projectile = pool.Spawn<SkillProjectile>(spawnPos);

                // 풀이 클리어된 경우 안전하게 종료
                if (projectile == null)
                {
                    Debug.LogWarning($"[Character] Burst fire stopped: pool returned null at {i}/{projectileCount}");
                    return;
                }

                projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime, FinalDamage, basicAttackSkillId, supportSkillId, GetDisplayCritChance(), GetDisplayCritMultiplier(), GetGenre());

                // 마지막 발사가 아니면 대기 (CancellationToken 전달)
                if (i < projectileCount - 1)
                {
                    try
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(BURST_INTERVAL), cancellationToken: ct);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.Log($"[Character] Burst fire cancelled during delay at {i}/{projectileCount}");
                        return;
                    }
                }
            }

            Debug.Log($"[Character] Burst fire complete: {projectileCount} projectiles fired (Damage: {FinalDamage:F1}, Speed: {FinalProjectileSpeed:F1})");
        }

        #endregion

        #region AOE Skills

        //LMJ : 통합된 AOE 스킬 메서드 - 기본공격/액티브 모두 사용
        //      AOE, DOT, Debuff, Trap, Mine, InstantSingle 스킬 처리
        private async UniTaskVoid UseAOESkillAsync(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage, float range, float projectileSpeed)
        {
            Debug.Log($"[Character] UseAOESkillAsync CALLED - skillData={skillData?.skill_name ?? "NULL"}, prefabs={(prefabs != null ? "EXISTS" : "NULL")}, damage={damage}, range={range}");

            if (skillData == null)
            {
                Debug.LogWarning("[Character] UseAOESkillAsync: skillData is NULL, returning early");
                return;
            }

            // Allow skill types that use AOE-style effects (범위 이펙트가 필요한 스킬 타입들)
            var skillType = skillData.GetSkillType();
            bool isValidType = skillType == SkillAssetType.AOE
                            || skillType == SkillAssetType.DOT
                            || skillType == SkillAssetType.Debuff
                            || skillType == SkillAssetType.Trap
                            || skillType == SkillAssetType.Mine
                            || skillType == SkillAssetType.InstantSingle;

            Debug.Log($"[Character] UseAOESkillAsync: skillType={skillType}, isValidType={isValidType}");

            if (!isValidType)
            {
                Debug.LogWarning($"[Character] UseAOESkillAsync: Invalid skill type {skillType}, returning early");
                return;
            }

            GameObject castEffect = null;
            GameObject meteorEffect = null;
            GameObject hitEffect = null;

            try
            {
                Debug.Log($"[Character] Starting AOE skill: {skillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = prefabs?.castEffectPrefab;
                GameObject projectileEffectPrefab = prefabs?.projectilePrefab;
                GameObject hitEffectPrefab = prefabs?.hitEffectPrefab;

                // 1. Cast Effect - cast_time_mult 적용
                float castTime = skillData.cast_time;
                if (supportData != null) castTime *= supportData.cast_time_mult;

                if (castTime > 0f)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;

                    // SkillEffectManager를 통해 시전 이펙트 스폰
                    var effectManager = SkillEffectManager.Instance;
                    if (effectManager != null)
                    {
                        castEffect = effectManager.SpawnCastEffect(skillData.skill_id, spawnPos);
                    }
                    else if (castEffectPrefab != null)
                    {
                        // Fallback: SkillEffectManager가 없으면 직접 스폰
                        castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
                        float castScale = prefabs?.GetCastScale() ?? 1f;
                        castEffect.transform.localScale = Vector3.one * castScale;
                    }

                    await UniTask.Delay((int)(castTime * 1000));

                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // 2. Get target position - 밀집 지역 기반 타겟팅
                // AOE 스킬은 몬스터 origin이 아닌 가장 밀집된 Area를 타겟으로 함
                float aoeRadius = skillData.aoe_radius > 0 ? skillData.aoe_radius : 3f;
                if (supportData != null) aoeRadius *= supportData.aoe_mult;

                // 부채꼴 AOE 각도 (360이면 원형)
                float aoeAngle = skillData.aoe_angle > 0 ? skillData.aoe_angle : 360f;
                bool isConeAOE = aoeAngle < 360f;

                Vector3 targetPos;
                Vector3 impactPos;
                Vector3 coneForwardDir = transform.forward; // 부채꼴 기준 방향

                if (isConeAOE)
                {
                    // 부채꼴 AOE: 캐릭터 위치에서 전방으로 발사
                    // 타겟 방향으로 캐릭터 회전
                    ITargetable nearestTarget = GetFirstTargetInRange(range);
                    if (nearestTarget == null)
                    {
                        Debug.Log("[Character] Cone AOE cancelled: No valid targets in range");
                        return;
                    }
                    targetPos = nearestTarget.GetPosition();
                    impactPos = transform.position;
                    impactPos.y = 0f;

                    // 부채꼴 방향 = 캐릭터에서 타겟으로
                    coneForwardDir = (targetPos - transform.position).normalized;
                    coneForwardDir.y = 0;
                    if (coneForwardDir.sqrMagnitude > 0.001f)
                    {
                        coneForwardDir.Normalize();
                    }
                    else
                    {
                        coneForwardDir = transform.forward;
                    }
                }
                else
                {
                    // 원형 AOE: 밀집 지역 타겟팅
                    targetPos = FindBestAOETargetPosition(range, aoeRadius);
                    if (targetPos == Vector3.zero)
                    {
                        Debug.Log("[Character] AOE cancelled: No valid targets in range");
                        return;
                    }

                    // Ground impact position
                    impactPos = targetPos;
                    Ray groundRay = new Ray(targetPos + Vector3.up * 10f, Vector3.down);
                    if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
                    {
                        impactPos = groundHit.point;
                    }
                    else
                    {
                        impactPos = new Vector3(targetPos.x, 0f, targetPos.z);
                    }
                }

                Debug.Log($"[Character] AOE impactPos calculated: {impactPos}, aoeRadius={aoeRadius}, aoeAngle={aoeAngle}, isCone={isConeAOE}");

                // 3.5 런타임 AOE Gizmo 업데이트 (디버그용)
                UpdateAOEGizmoInfo(impactPos, aoeRadius);

                // 4. Meteor Effect (only if projectile_speed > 0, otherwise instant AOE)
                if (skillData.projectile_speed > 0)
                {
                    Vector3 meteorStartPos = impactPos + Vector3.up * 20f;

                    // SkillEffectManager를 통해 메테오 이펙트 스폰
                    var effectManager = SkillEffectManager.Instance;
                    if (effectManager != null)
                    {
                        meteorEffect = effectManager.SpawnMainEffect(skillData.skill_id, meteorStartPos);
                    }
                    else if (projectileEffectPrefab != null)
                    {
                        // Fallback
                        meteorEffect = UnityEngine.Object.Instantiate(projectileEffectPrefab, meteorStartPos, Quaternion.identity);
                    }

                    if (meteorEffect == null)
                    {
                        Debug.LogWarning($"[Character] Failed to spawn meteor effect for skill {skillData.skill_id}");
                    }

                    float meteorSpeed = projectileSpeed > 0 ? projectileSpeed : 10f;
                    float distance = Vector3.Distance(meteorStartPos, impactPos);
                    float travelTime = distance / meteorSpeed;

                    float elapsed = 0f;
                    while (elapsed < travelTime && meteorEffect != null)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / travelTime);
                        meteorEffect.transform.position = Vector3.Lerp(meteorStartPos, impactPos, t);

                        Vector3 direction = (impactPos - meteorStartPos).normalized;
                        if (direction != Vector3.zero)
                        {
                            meteorEffect.transform.rotation = Quaternion.LookRotation(direction);
                        }

                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }

                    if (meteorEffect != null)
                    {
                        meteorEffect.transform.position = impactPos;
                    }
                }

                // 5. Hit Effect - SkillEffectManager 또는 SkillEffectDatabase에서 이펙트 로드
                // (표식/CC 스킬은 각 타겟에 개별 적용하므로 중앙 이펙트 생략)
                bool skipCentralEffect = skillData.HasMarkEffect || skillData.HasCCEffect;
                bool effectFromManager = false; // SkillEffectManager로 생성된 이펙트인지 추적

                Debug.Log($"[Character] AOE Effect Loading - skillId={skillData.skill_id}, skipCentral={skipCentralEffect}");

                if (!skipCentralEffect)
                {
                    GameObject effectPrefabToUse = null;

                    // 1. SkillEffectManager를 통한 독립 이펙트 스폰 시도
                    var effectManager = SkillEffectManager.Instance;
                    Debug.Log($"[Character] SkillEffectManager.Instance = {(effectManager != null ? "EXISTS" : "NULL")}");

                    if (effectManager != null)
                    {
                        hitEffect = await effectManager.PlayEffectAtPosition(skillData.skill_id, impactPos);

                        if (hitEffect != null)
                        {
                            effectFromManager = true;
                            // SkillEffectManager 이펙트는 스케일 조정 불필요 (Database에서 설정된 스케일 사용)
                            Debug.Log($"[Character] AOE effect spawned via SkillEffectManager: skill={skillData.skill_id}, effect={hitEffect.name}");
                        }
                        else
                        {
                            Debug.Log($"[Character] SkillEffectManager returned null for skill {skillData.skill_id}");
                        }
                    }

                    // 2. SkillEffectManager가 없거나 이펙트가 없으면 SkillEffectDatabase에서 직접 로드
                    if (hitEffect == null)
                    {
                        var effectDb = SkillEffectDatabase.Instance;
                        Debug.Log($"[Character] SkillEffectDatabase.Instance = {(effectDb != null ? "EXISTS" : "NULL")}");

                        if (effectDb != null)
                        {
                            effectPrefabToUse = effectDb.GetMainEffect(skillData.skill_id);
                            Debug.Log($"[Character] SkillEffectDatabase.GetMainEffect({skillData.skill_id}) = {(effectPrefabToUse != null ? effectPrefabToUse.name : "NULL")}");
                        }

                        // 3. Database에도 없으면 prefabs에서 로드
                        if (effectPrefabToUse == null)
                        {
                            effectPrefabToUse = hitEffectPrefab;
                            Debug.Log($"[Character] Fallback to hitEffectPrefab = {(effectPrefabToUse != null ? effectPrefabToUse.name : "NULL")}");
                        }

                        // 이펙트 스폰
                        if (effectPrefabToUse != null)
                        {
                            hitEffect = UnityEngine.Object.Instantiate(effectPrefabToUse, impactPos, Quaternion.identity);

                            // 이펙트 스케일 = aoe_radius / baseEffectRadius
                            // baseEffectRadius: 이펙트 프리팹이 scale=1일 때의 시각적 반경
                            float effectScale = 1f;
                            var effectEntry = effectDb?.GetEntry(skillData.skill_id);
                            if (effectEntry != null && effectEntry.baseEffectRadius > 0)
                            {
                                effectScale = aoeRadius / effectEntry.baseEffectRadius;
                            }
                            hitEffect.transform.localScale = Vector3.one * effectScale;

                            Debug.Log($"[Character] AOE effect spawned: skill={skillData.skill_id}, effect={hitEffect.name}, scale={effectScale:F2} (aoe={aoeRadius}/base={effectEntry?.baseEffectRadius ?? 1}), pos={impactPos}");
                        }
                        else
                        {
                            Debug.LogWarning($"[Character] No effect prefab found for AOE skill {skillData.skill_id} - ALL FALLBACKS FAILED!");
                        }
                    }
                }

                // 6. AOE damage - aoeRadius는 위에서 이미 계산됨
                Collider[] hits = Physics.OverlapSphere(impactPos, aoeRadius);

                // 마법 집중 (39034) 배율 적용
                float magicFocusMultiplier = ConsumeMagicFocusMultiplier();
                float damageToApply = damage * magicFocusMultiplier;

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hit = hits[i];
                    if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                        continue;

                    ITargetable hitTarget = hit.GetComponent<ITargetable>();
                    if (hitTarget == null || !hitTarget.IsAlive())
                        continue;

                    // 부채꼴 AOE: 각도 체크
                    if (isConeAOE && !IsInConeAngle(hitTarget.GetPosition(), impactPos, coneForwardDir, aoeAngle))
                        continue;

                    // 각 대상에게 히트 이펙트 생성 (표식/CC 스킬은 ApplyMark/ApplyCC에서 처리하므로 제외)
                    if (!skipCentralEffect)
                    {
                        Vector3 hitTargetPos = hitTarget.GetPosition();

                        // SkillEffectManager를 통해 히트 이펙트 스폰
                        var hitEffectManager = SkillEffectManager.Instance;
                        if (hitEffectManager != null)
                        {
                            hitEffectManager.SpawnHitEffect(skillData.skill_id, hitTargetPos + Vector3.up);
                        }
                        else if (hitEffectPrefab != null)
                        {
                            // Fallback
                            GameObject targetHitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitTargetPos + Vector3.up, Quaternion.identity);
                            float hitScale = prefabs?.GetHitScale() ?? 1f;
                            targetHitEffect.transform.localScale = Vector3.one * hitScale;
                            UnityEngine.Object.Destroy(targetHitEffect, 1f);
                        }
                    }

                    // 데미지 적용 (디버프 스킬은 데미지 0일 수 있음)
                    if (damageToApply > 0)
                    {
                        // 상성 배율 적용
                        Genre defenderGenre = Genre.Horror;
                        if (hit.CompareTag(Tag.Monster))
                        {
                            Monster monster = hit.GetComponent<Monster>();
                            if (monster != null) defenderGenre = monster.GetGenre();
                        }
                        else if (hit.CompareTag(Tag.BossMonster))
                        {
                            BossMonster boss = hit.GetComponent<BossMonster>();
                            if (boss != null) defenderGenre = boss.GetGenre();
                        }
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), defenderGenre);
                        hitTarget.TakeDamage(damageToApply * genreMultiplier);
                    }

                    // MainSkillData 자체 효과 적용 (CC/DOT/표식/디버프)
                    ApplyMainSkillEffectsToTarget(hitTarget, skillData);

                    // Support 효과 적용
                    if (supportData != null && supportData.GetStatusEffectType() != StatusEffectType.None)
                    {
                        ApplyStatusEffect(hitTarget);
                    }
                }

                // Cleanup
                // meteorEffect는 수동 정리 (아직 SkillEffectManager 적용 안 됨)
                if (meteorEffect != null)
                {
                    UnityEngine.Object.Destroy(meteorEffect, 0.1f);
                }

                // hitEffect 정리
                // SkillEffectManager로 스폰된 경우: AutoDespawnEffectAsync에서 자동 정리됨
                // Fallback으로 직접 생성된 경우: 수동 정리 필요
                if (hitEffect != null && !effectFromManager)
                {
                    // ParticleSystem 기반 이펙트의 경우 duration 계산
                    float effectDuration = 3f; // 기본 3초
                    var particleSystems = hitEffect.GetComponentsInChildren<ParticleSystem>();
                    if (particleSystems.Length > 0)
                    {
                        foreach (var ps in particleSystems)
                        {
                            float psDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                            if (psDuration > effectDuration) effectDuration = psDuration;
                        }
                    }
                    UnityEngine.Object.Destroy(hitEffect, effectDuration);
                    Debug.Log($"[Character] AOE fallback effect scheduled for destroy in {effectDuration}s");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Character] AOE skill cancelled");
            }
            finally
            {
                if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
            }
        }

        #endregion

        #region Channeling Skills

        //LMJ : Use channeling skill (laser/beam style)
        //LMJ : 통합된 채널링 스킬 메서드 - 기본공격/액티브 모두 사용
        private async UniTaskVoid UseChannelingSkillAsync(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage)
        {
            if (skillData == null || skillData.GetSkillType() != SkillAssetType.Channeling) return;

            isChanneling = true;
            channelingCts?.Cancel();
            channelingCts?.Dispose();
            channelingCts = new System.Threading.CancellationTokenSource();
            var ct = channelingCts.Token;

            GameObject castEffect = null;
            GameObject startEffect = null;
            List<GameObject> beamEffects = new List<GameObject>();
            List<GameObject> hitEffects = new List<GameObject>();

            try
            {
                Debug.Log($"[Character] Starting channeling skill: {skillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = prefabs?.castEffectPrefab;
                GameObject projectileEffectPrefab = prefabs?.projectilePrefab;
                GameObject areaEffectPrefab = prefabs?.areaEffectPrefab;
                GameObject hitEffectPrefab = prefabs?.hitEffectPrefab;

                // 1. Cast Effect (시전 준비) - cast_time_mult 적용
                float castTime = skillData.cast_time;
                if (supportData != null) castTime *= supportData.cast_time_mult;

                if (castTime > 0f)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;

                    // SkillEffectManager를 통해 시전 이펙트 스폰
                    var effectManager = SkillEffectManager.Instance;
                    if (effectManager != null)
                    {
                        castEffect = effectManager.SpawnCastEffect(skillData.skill_id, spawnPos);
                    }
                    else if (castEffectPrefab != null)
                    {
                        // Fallback
                        castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
                        float castScale = prefabs?.GetCastScale() ?? 1f;
                        castEffect.transform.localScale = Vector3.one * castScale;
                    }
                    Debug.Log($"[Character] Cast Effect started ({castTime:F1}s)");

                    await UniTask.Delay((int)(castTime * 1000), cancellationToken: ct);

                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // Check if target is still valid after cast time
                if (target == null || !target.IsAlive())
                {
                    Debug.Log("[Character] Channeling cancelled: Target died during cast");
                    return;
                }

                // 모든 Channeling 이펙트가 Wall을 통과하도록 레이어 설정용
                int projectileLayer = LayerMask.NameToLayer("Projectile");

                // 2. Start Effect (빔 발사 지점)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;

                    // SkillEffectManager를 통해 시작 이펙트 스폰
                    var startEffectManager = SkillEffectManager.Instance;
                    if (startEffectManager != null)
                    {
                        startEffect = startEffectManager.SpawnMainEffect(skillData.skill_id, spawnPos);
                    }
                    else if (projectileEffectPrefab != null)
                    {
                        // Fallback
                        startEffect = UnityEngine.Object.Instantiate(projectileEffectPrefab, spawnPos, Quaternion.identity);
                    }

                    if (startEffect != null)
                    {
                        startEffect.transform.SetParent(transform);

                        // Wall 통과를 위해 Projectile 레이어 설정 및 Collider 비활성화
                        if (projectileLayer != -1)
                        {
                            CharacterEffectUtils.SetLayerRecursively(startEffect, projectileLayer);
                        }
                        CharacterEffectUtils.DisableCollidersRecursively(startEffect);

                        // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                        CharacterEffectUtils.SetBeamRenderingOrder(startEffect, 100);

                        Debug.Log("[Character] Start Effect spawned (Layer: Projectile, Colliders disabled, RenderQueue: 3100)");
                    }
                }

                // 3. Build chain targets (if Chain support skill is active)
                List<ITargetable> chainTargets = BuildChainTargets(target);

                // 4. Create beam effects for all targets
                // 빔 이펙트가 Wall을 통과하도록 레이어 설정 및 Collider 비활성화
                if (areaEffectPrefab != null)
                {
                    // Get area effect scale
                    float areaScale = prefabs?.GetAreaScale() ?? 1f;

                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        Vector3 spawnPos = (i == 0) ? transform.position + spawnOffset : chainTargets[i - 1].GetPosition();
                        GameObject beamEffect = UnityEngine.Object.Instantiate(areaEffectPrefab, spawnPos, Quaternion.identity);
                        // Apply area effect scale
                        beamEffect.transform.localScale = Vector3.one * areaScale;

                        // Wall 통과를 위해 Projectile 레이어 설정
                        if (projectileLayer != -1)
                        {
                            CharacterEffectUtils.SetLayerRecursively(beamEffect, projectileLayer);
                        }

                        // Collider 비활성화 (빔은 시각적 효과만, 물리 충돌 불필요)
                        CharacterEffectUtils.DisableCollidersRecursively(beamEffect);

                        // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                        CharacterEffectUtils.SetBeamRenderingOrder(beamEffect, 100);

                        // RetroBeamStatic의 beamCollides 비활성화 (Wall Raycast 충돌 방지)
                        CharacterEffectUtils.DisableBeamCollision(beamEffect);

                        beamEffects.Add(beamEffect);
                    }
                    Debug.Log($"[Character] Created {beamEffects.Count} beam effects for {chainTargets.Count} targets (beamCollides disabled), scale={areaScale}");
                }

                // 5. Create hit effects for all targets
                // 히트 이펙트도 Wall 통과 처리
                {
                    var hitEffectManager = SkillEffectManager.Instance;

                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        GameObject hitEffect = null;

                        // SkillEffectManager를 통해 히트 이펙트 스폰
                        if (hitEffectManager != null)
                        {
                            hitEffect = hitEffectManager.SpawnHitEffect(skillData.skill_id, chainTargets[i].GetPosition());
                        }
                        else if (hitEffectPrefab != null)
                        {
                            // Fallback
                            hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);
                            float hitScale = prefabs?.GetHitScale() ?? 1f;
                            hitEffect.transform.localScale = Vector3.one * hitScale;
                        }

                        if (hitEffect != null)
                        {
                            hitEffect.transform.SetParent(chainTargets[i].GetTransform());

                            // Wall 통과를 위해 Projectile 레이어 설정 및 Collider 비활성화
                            if (projectileLayer != -1)
                            {
                                CharacterEffectUtils.SetLayerRecursively(hitEffect, projectileLayer);
                            }
                            CharacterEffectUtils.DisableCollidersRecursively(hitEffect);

                            // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                            CharacterEffectUtils.SetBeamRenderingOrder(hitEffect, 100);

                            hitEffects.Add(hitEffect);
                        }
                    }
                }

                // 6. Channeling loop
                float elapsed = 0f;
                float nextTickTime = 0f;
                int tickCount = 0;
                bool firstTick = true;

                // Issue #362 - 채널링 지속시간 배율 적용
                float finalChannelDuration = skillData.channel_duration;
                if (supportData != null && supportData.channel_duration_mult > 0)
                {
                    finalChannelDuration = DamageCalculator.CalculateChannelDuration(
                        skillData.channel_duration, supportData.channel_duration_mult);
                }

                // interruptible 체크를 위한 변수 (CSV의 interruptible 필드)
                bool isInterruptible = skillData.interruptible;
                float skillRange = skillData.range > 0 ? skillData.range : 10f;

                while (elapsed < finalChannelDuration)
                {
                    // interruptible 스킬: 타겟이 범위를 벗어나면 채널링 중단
                    if (isInterruptible && target != null)
                    {
                        float distanceToTarget = Vector3.Distance(transform.position, target.GetPosition());
                        if (distanceToTarget > skillRange * 1.2f) // 범위의 120% 초과 시 중단
                        {
                            Debug.Log($"[Character] Channeling interrupted: Target out of range ({distanceToTarget:F1} > {skillRange * 1.2f:F1})");
                            break;
                        }
                    }
                    // Update beam effects and clean up dead targets
                    for (int i = 0; i < beamEffects.Count && i < chainTargets.Count; i++)
                    {
                        if (chainTargets[i] == null || !chainTargets[i].IsAlive())
                        {
                            if (beamEffects[i] != null) UnityEngine.Object.Destroy(beamEffects[i]);
                            beamEffects[i] = null;

                            if (i < hitEffects.Count && hitEffects[i] != null)
                            {
                                UnityEngine.Object.Destroy(hitEffects[i]);
                                hitEffects[i] = null;
                            }
                            continue;
                        }

                        Vector3 startPos = (i == 0) ? transform.position + spawnOffset : chainTargets[i - 1].GetPosition();
                        Vector3 endPos = chainTargets[i].GetPosition();
                        CharacterEffectUtils.UpdateBeamEffect(beamEffects[i], startPos, endPos);
                    }

                    // Apply damage at tick intervals
                    if (elapsed >= nextTickTime)
                    {
                        float currentDamage = damage;

                        for (int i = 0; i < chainTargets.Count; i++)
                        {
                            if (chainTargets[i] == null || !chainTargets[i].IsAlive())
                                continue;

                            // Apply chain damage reduction
                            if (i > 0 && supportData != null && supportData.GetStatusEffectType() == StatusEffectType.Chain)
                            {
                                currentDamage *= (1f - supportData.chain_damage_reduction / 100f);
                            }

                            // Apply status effects (only on first tick)
                            if (firstTick && supportData != null && supportData.GetStatusEffectType() != StatusEffectType.Chain)
                            {
                                ApplyStatusEffect(chainTargets[i]);
                            }

                            // Apply damage (상성 배율 적용)
                            Genre defenderGenre = Genre.Horror;
                            if (chainTargets[i].GetTransform().CompareTag(Tag.Monster))
                            {
                                Monster monster = chainTargets[i].GetTransform().GetComponent<Monster>();
                                if (monster != null) defenderGenre = monster.GetGenre();
                            }
                            else if (chainTargets[i].GetTransform().CompareTag(Tag.BossMonster))
                            {
                                BossMonster boss = chainTargets[i].GetTransform().GetComponent<BossMonster>();
                                if (boss != null) defenderGenre = boss.GetGenre();
                            }
                            float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), defenderGenre);
                            chainTargets[i].TakeDamage(currentDamage * genreMultiplier);

                            // 틱마다 히트 이펙트 재생 (타겟 위치에 새로 생성)
                            // Wall 통과 처리 포함
                            {
                                GameObject tickHitEffect = null;
                                var tickEffectManager = SkillEffectManager.Instance;

                                if (tickEffectManager != null)
                                {
                                    tickHitEffect = tickEffectManager.SpawnHitEffect(skillData.skill_id, chainTargets[i].GetPosition());
                                }
                                else if (hitEffectPrefab != null)
                                {
                                    // Fallback
                                    tickHitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);
                                    float tickHitScale = prefabs?.GetHitScale() ?? 1f;
                                    tickHitEffect.transform.localScale = Vector3.one * tickHitScale;
                                }

                                if (tickHitEffect != null)
                                {
                                    // Wall 통과를 위해 Projectile 레이어 설정 및 Collider 비활성화
                                    if (projectileLayer != -1)
                                    {
                                        CharacterEffectUtils.SetLayerRecursively(tickHitEffect, projectileLayer);
                                    }
                                    CharacterEffectUtils.DisableCollidersRecursively(tickHitEffect);

                                    // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                                    CharacterEffectUtils.SetBeamRenderingOrder(tickHitEffect, 100);

                                    UnityEngine.Object.Destroy(tickHitEffect, 0.5f); // 짧은 시간 후 자동 삭제
                                }
                            }
                        }

                        tickCount++;
                        nextTickTime += skillData.channel_tick_interval;
                        firstTick = false;
                    }

                    await UniTask.Yield(ct);
                    elapsed += Time.deltaTime;
                }

                Debug.Log($"[Character] Channeling completed: {tickCount} ticks, {chainTargets.Count} targets");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Character] Channeling cancelled");
            }
            finally
            {
                if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                if (startEffect != null) UnityEngine.Object.Destroy(startEffect);
                foreach (var beam in beamEffects)
                {
                    if (beam != null) UnityEngine.Object.Destroy(beam);
                }
                foreach (var hitEffect in hitEffects)
                {
                    if (hitEffect != null) UnityEngine.Object.Destroy(hitEffect);
                }

                isChanneling = false;
            }
        }

        #endregion

        #region Buff Skills

        //LMJ : Use Buff skill - apply buff to self or allies
        //LMJ : 통합된 버프 스킬 메서드 - 기본공격/액티브 모두 사용
        private async UniTaskVoid UseBuffSkillAsync(MainSkillData skillData, MainSkillPrefabEntry prefabs)
        {
            if (skillData == null) return;

            var skillType = skillData.GetSkillType();
            if (skillType != SkillAssetType.Buff) return;

            GameObject castEffect = null;
            GameObject mainEffect = null;

            try
            {
                Debug.Log($"[Character] Starting Buff skill: {skillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = prefabs?.castEffectPrefab;
                GameObject hitEffectPrefab = prefabs?.hitEffectPrefab;

                // 1. Cast Effect
                float castTime = skillData.cast_time;
                if (supportData != null) castTime *= supportData.cast_time_mult;

                if (castTime > 0f)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;

                    // SkillEffectManager를 통해 시전 이펙트 스폰
                    var buffEffectManager = SkillEffectManager.Instance;
                    if (buffEffectManager != null)
                    {
                        castEffect = buffEffectManager.SpawnCastEffect(skillData.skill_id, spawnPos);
                    }
                    else if (castEffectPrefab != null)
                    {
                        // Fallback
                        castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
                        float castScale = prefabs?.GetCastScale() ?? 1f;
                        castEffect.transform.localScale = Vector3.one * castScale;
                    }

                    await UniTask.Delay((int)(castTime * 1000));
                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // 2. Special Buff: 마법 집중 (39034) - 다음 3번 AOE 스킬 50% 데미지 증가
                if (skillData.skill_id == 39034)
                {
                    // 마법 집중 특수 효과 활성화
                    // 설명: "해당 스킬을 시전한 이후 다음 3번동안 시전되는 광역 스킬 공격력의 데미지가 50% 증가한다"
                    const int MAGIC_FOCUS_AOE_COUNT = 3;
                    const float MAGIC_FOCUS_DAMAGE_MULT = 1.5f; // 50% 증가

                    ActivateMagicFocus(MAGIC_FOCUS_AOE_COUNT, MAGIC_FOCUS_DAMAGE_MULT);
                    Debug.Log($"[Character] 마법 집중 (39034) 시전: 다음 {MAGIC_FOCUS_AOE_COUNT}번의 AOE 스킬 데미지 50% 증가");
                }

                // 3. Apply buff effect to allies in range
                float buffValue = skillData.base_buff_value;
                if (supportData != null) buffValue *= supportData.buff_value_mult;

                float buffDuration = skillData.skill_lifetime;
                if (supportData != null) buffDuration *= supportData.buff_value_mult; // 지속시간도 배율 적용

                BuffType buffType = skillData.GetBuffType();
                float buffRadius = skillData.aoe_radius > 0 ? skillData.aoe_radius : 400f; // 기본 범위

                // 범위 내 아군 캐릭터에게 버프 적용
                ApplyBuffToAlliesInRange(buffType, buffValue, buffDuration, buffRadius);

                Debug.Log($"[Character] Buff skill applied: {skillData.skill_name} (Type: {buffType}, Value: {buffValue}%, Duration: {buffDuration}s, Radius: {buffRadius})");

                // 4. Main Effect 스폰 (SkillEffectManager를 통해)
                // 버프 스킬의 mainEffectPrefab은 캐릭터 위치에 스폰
                var effectManager = SkillEffectManager.Instance;
                if (effectManager != null)
                {
                    Vector3 effectPos = transform.position;
                    mainEffect = await effectManager.PlayEffectAtPosition(skillData.skill_id, effectPos);
                    if (mainEffect != null)
                    {
                        Debug.Log($"[Character] Buff main effect spawned via SkillEffectManager: skill={skillData.skill_id}, effect={mainEffect.name}");
                    }
                }

                // 5. Fallback: SkillEffectManager가 없거나 mainEffect가 없으면 hitEffectPrefab 사용
                // 버프 지속시간 동안 이펙트 반복 재생
                if (mainEffect == null && buffDuration > 0)
                {
                    float effectTickInterval = 1f; // 1초마다 이펙트 재생
                    float elapsed = 0f;
                    var tickEffectManager = SkillEffectManager.Instance;

                    while (elapsed < buffDuration)
                    {
                        Vector3 effectPos = transform.position;
                        GameObject tickEffect = null;

                        if (tickEffectManager != null)
                        {
                            tickEffect = tickEffectManager.SpawnHitEffect(skillData.skill_id, effectPos);
                        }
                        else if (hitEffectPrefab != null)
                        {
                            // Fallback
                            tickEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, effectPos, Quaternion.identity);
                            float hitScale = prefabs?.GetHitScale() ?? 1f;
                            tickEffect.transform.localScale = Vector3.one * hitScale;
                        }

                        if (tickEffect != null)
                        {
                            UnityEngine.Object.Destroy(tickEffect, 0.8f); // 이펙트 0.8초 후 삭제
                        }

                        await UniTask.Delay((int)(effectTickInterval * 1000));
                        elapsed += effectTickInterval;
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Character] Buff skill cancelled");
            }
            finally
            {
                if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                // mainEffect는 SkillEffectManager가 자동으로 정리함 (AutoDespawnEffectAsync)
            }
        }

        /// <summary>
        /// 범위 내 아군에게 버프 적용
        /// includeSelf: true면 자기 자신도 포함 (DevScene 테스트용)
        /// </summary>
        private void ApplyBuffToAlliesInRange(BuffType buffType, float buffValue, float duration, float radius, bool includeSelf = false)
        {
            // 범위 내 모든 캐릭터 찾기
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);

            // 아군이 없는 경우 (DevScene 테스트 등) 자기 자신에게 버프 적용
            bool hasAllies = false;
            foreach (var hit in hits)
            {
                if (hit.CompareTag(Tag.Character))
                {
                    Character ally = hit.GetComponent<Character>();
                    if (ally != null && ally != this)
                    {
                        hasAllies = true;
                        break;
                    }
                }
            }

            // 아군이 없으면 자동으로 자기 자신 포함
            if (!hasAllies)
            {
                includeSelf = true;
                Debug.Log("[Character] 범위 내 아군 없음 - 자기 자신에게 버프 적용");
            }

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(Tag.Character)) continue;

                Character ally = hit.GetComponent<Character>();
                if (ally == null) continue;

                // 자신 제외 (세레나데 등) - includeSelf가 true면 자기 자신도 포함
                if (ally == this && !includeSelf) continue;

                // 버프 타입에 따라 스탯 적용
                float percentValue = buffValue / 100f; // % → 소수
                switch (buffType)
                {
                    case BuffType.ATK_Damage_UP:
                        ally.ApplyTemporaryBuff(StatType.Damage, percentValue, duration);
                        break;
                    case BuffType.ATK_Speed_UP:
                        ally.ApplyTemporaryBuff(StatType.AttackSpeed, percentValue, duration);
                        break;
                    case BuffType.ATK_Range_UP:
                        ally.ApplyTemporaryBuff(StatType.Range, percentValue, duration);
                        break;
                    case BuffType.Critical_Damage_UP:
                        ally.ApplyTemporaryBuff(StatType.CritMultiplier, percentValue, duration);
                        break;
                    case BuffType.Battle_Exp_UP:
                        // 경험치 버프: StageManager의 expMultiplier에 반영
                        var stageManager = GameManager.Instance?.Stage;
                        if (stageManager != null)
                        {
                            float expBonus = buffValue / 100f; // % → 소수
                            stageManager.AddExpMultiplier(expBonus);

                            // 지속시간 후 배율 제거
                            RemoveExpMultiplierAfterDurationAsync(stageManager, expBonus, duration).Forget();

                            Debug.Log($"[Character] Battle_Exp_UP buff applied: +{buffValue}% for {duration}s");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Battle_Exp_UP 버프 지속시간 후 배율 제거
        /// </summary>
        private async UniTaskVoid RemoveExpMultiplierAfterDurationAsync(StageManager stageManager, float expBonus, float duration)
        {
            await UniTask.Delay((int)(duration * 1000));

            if (stageManager != null)
            {
                stageManager.RemoveExpMultiplier(expBonus);
                Debug.Log($"[Character] Battle_Exp_UP buff expired: -{expBonus * 100f}%");
            }
        }

        #endregion

        #region Trap and Mine Skills

        //LMJ : 통합된 트랩 배치 메서드 - 기본공격/액티브 모두 사용
        private void PlaceTrapObject(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage)
        {
            if (skillData == null || target == null) return;

            // Get placement position
            Vector3 placementPos = target.GetPosition();

            // Raycast to ground for proper placement
            Ray groundRay = new Ray(placementPos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
            {
                placementPos = groundHit.point;
            }
            else
            {
                placementPos.y = 0f;
            }

            // Create trap object with attacker genre (상성 시스템)
            GameObject trapObj = new GameObject($"Trap_{skillData.skill_name}");
            TrapObject trap = trapObj.AddComponent<TrapObject>();
            trap.Initialize(skillData, prefabs, supportData, damage, placementPos, GetGenre());

            Debug.Log($"[Character] Placed Trap: {skillData.skill_name} at {placementPos}");
        }

        //LMJ : 통합된 지뢰 배치 메서드 - 기본공격/액티브 모두 사용
        private void PlaceMineObject(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage)
        {
            if (skillData == null || target == null) return;

            // Get placement position
            Vector3 placementPos = target.GetPosition();

            // Raycast to ground for proper placement
            Ray groundRay = new Ray(placementPos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
            {
                placementPos = groundHit.point;
            }
            else
            {
                placementPos.y = 0f;
            }

            // Create mine object with attacker genre (상성 시스템)
            GameObject mineObj = new GameObject($"Mine_{skillData.skill_name}");
            MineObject mine = mineObj.AddComponent<MineObject>();
            mine.Initialize(skillData, prefabs, supportData, damage, placementPos, GetGenre());

            Debug.Log($"[Character] Placed Mine: {skillData.skill_name} at {placementPos}");
        }

        #endregion

        #region Instant Kill Skills

        //LMJ : Use instant kill skill (심장마비 - 체력 10% 이하 적 즉사, 보스 제외)
        private void UseInstantKillSkill(ITargetable target)
        {
            if (basicAttackData == null || target == null) return;

            // Get hit effect prefab
            GameObject hitEffectPrefab = basicAttackPrefabs?.hitEffectPrefab;

            // Get target's collider for proper effect positioning
            Collider targetCol = target.GetTransform().GetComponent<Collider>();
            Vector3 hitPos = targetCol != null ? targetCol.bounds.center : target.GetPosition();

            // Check if target is boss (cannot be instant killed)
            if (target.GetTransform().CompareTag(Tag.BossMonster))
            {
                // Boss: apply normal damage instead
                BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                if (boss != null)
                {
                    // 상성 배율 적용
                    float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), boss.GetGenre());
                    boss.TakeDamage(FinalDamage * genreMultiplier);

                    // SkillEffectManager를 통해 히트 이펙트 스폰
                    var effectManager = SkillEffectManager.Instance;
                    if (effectManager != null && basicAttackSkillId > 0)
                    {
                        effectManager.SpawnHitEffect(basicAttackSkillId, hitPos);
                    }
                    else if (hitEffectPrefab != null)
                    {
                        // Fallback
                        GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                        float hitScale = basicAttackPrefabs?.GetHitScale() ?? 1f;
                        hitEffect.transform.localScale = Vector3.one * hitScale;
                        UnityEngine.Object.Destroy(hitEffect, 2f);
                    }

                    Debug.Log($"[Character] InstantKill on Boss: Normal damage {FinalDamage} (bosses cannot be instant killed)");
                }
                return;
            }

            // Regular monster: check HP threshold (10%)
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                if (monster != null)
                {
                    float hpRatio = monster.GetHealth() / monster.GetMaxHealth();
                    float instantKillThreshold = 0.1f; // 10%

                    // SkillEffectManager 참조
                    var instantKillEffectManager = SkillEffectManager.Instance;

                    if (hpRatio <= instantKillThreshold)
                    {
                        // Instant kill!
                        monster.Die();

                        // SkillEffectManager를 통해 히트 이펙트 스폰
                        if (instantKillEffectManager != null && basicAttackSkillId > 0)
                        {
                            instantKillEffectManager.SpawnHitEffect(basicAttackSkillId, hitPos);
                        }
                        else if (hitEffectPrefab != null)
                        {
                            // Fallback
                            GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                            float hitScale = basicAttackPrefabs?.GetHitScale() ?? 1f;
                            hitEffect.transform.localScale = Vector3.one * hitScale;
                            UnityEngine.Object.Destroy(hitEffect, 2f);
                        }

                        Debug.Log($"[Character] InstantKill SUCCESS: {monster.name} (HP: {hpRatio * 100:F1}% <= {instantKillThreshold * 100}%)");
                    }
                    else
                    {
                        // HP too high: apply normal damage (상성 배율 적용)
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), monster.GetGenre());
                        monster.TakeDamage(FinalDamage * genreMultiplier);

                        // SkillEffectManager를 통해 히트 이펙트 스폰
                        if (instantKillEffectManager != null && basicAttackSkillId > 0)
                        {
                            instantKillEffectManager.SpawnHitEffect(basicAttackSkillId, hitPos);
                        }
                        else if (hitEffectPrefab != null)
                        {
                            // Fallback
                            GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                            float hitScale = basicAttackPrefabs?.GetHitScale() ?? 1f;
                            hitEffect.transform.localScale = Vector3.one * hitScale;
                            UnityEngine.Object.Destroy(hitEffect, 2f);
                        }

                        Debug.Log($"[Character] InstantKill FAILED: {monster.name} HP {hpRatio * 100:F1}% > {instantKillThreshold * 100}%, applied {FinalDamage} damage");
                    }
                }
            }
        }

        //LMJ : Use active instant kill skill (심장마비)
        private void UseActiveInstantKillSkill(ITargetable target)
        {
            if (activeSkillData == null || target == null) return;

            Debug.Log($"[Character] Active InstantKill: {activeSkillData.skill_name}");

            // 범위 내 체력 10% 이하인 적 즉사 (보스 제외)
            float aoeRadius = activeSkillData.aoe_radius > 0 ? activeSkillData.aoe_radius : 100f;
            Collider[] hits = Physics.OverlapSphere(target.GetPosition(), aoeRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag(Tag.Monster))
                {
                    Monster monster = hits[i].GetComponent<Monster>();
                    if (monster != null && monster.IsAlive())
                    {
                        float hpPercent = monster.GetHealth() / monster.GetMaxHealth();
                        if (hpPercent <= 0.1f)
                        {
                            Debug.Log($"[Character] InstantKill 즉사: {monster.name} (HP: {hpPercent * 100:F1}%)");
                            monster.Die();
                        }
                        else
                        {
                            // 10% 초과면 일반 데미지 (상성 배율 적용)
                            float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), monster.GetGenre());
                            monster.TakeDamage(FinalActiveDamage * genreMultiplier);
                        }
                    }
                }
                // 보스는 즉사 불가, 일반 데미지만
                else if (hits[i].CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = hits[i].GetComponent<BossMonster>();
                    if (boss != null && boss.IsAlive())
                    {
                        // 상성 배율 적용
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), boss.GetGenre());
                        boss.TakeDamage(FinalActiveDamage * genreMultiplier);
                    }
                }
            }
        }

        #endregion

        #region AOE Angle Helper

        /// <summary>
        /// 부채꼴 AOE 각도 체크
        /// 캐릭터의 전방 방향 기준으로 대상이 지정된 각도 내에 있는지 확인
        /// </summary>
        /// <param name="targetPos">대상 위치</param>
        /// <param name="centerPos">AOE 중심 위치 (캐릭터 위치)</param>
        /// <param name="forwardDir">캐릭터 전방 방향</param>
        /// <param name="angleInDegrees">부채꼴 각도 (360이면 원형)</param>
        /// <returns>대상이 부채꼴 범위 내에 있으면 true</returns>
        private bool IsInConeAngle(Vector3 targetPos, Vector3 centerPos, Vector3 forwardDir, float angleInDegrees)
        {
            // 360도면 원형 AOE - 항상 true
            if (angleInDegrees >= 360f) return true;

            // 중심에서 대상으로의 방향 (Y축 무시하고 수평면에서 계산)
            Vector3 dirToTarget = targetPos - centerPos;
            dirToTarget.y = 0;
            forwardDir.y = 0;

            if (dirToTarget.sqrMagnitude < 0.001f) return true; // 중심에 있으면 포함

            dirToTarget.Normalize();
            forwardDir.Normalize();

            // 두 벡터 사이의 각도 계산
            float angle = Vector3.Angle(forwardDir, dirToTarget);

            // 부채꼴은 전방 기준 좌우로 퍼지므로 halfAngle과 비교
            float halfAngle = angleInDegrees / 2f;
            return angle <= halfAngle;
        }

        #endregion
    }
}
