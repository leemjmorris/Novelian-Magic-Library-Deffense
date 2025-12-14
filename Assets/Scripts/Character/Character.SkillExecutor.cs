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
                    Projectile projectile = pool.Spawn<Projectile>(spawnPos);
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

                if (hitEffectPrefab != null)
                {
                    GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
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
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);

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
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);

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
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);

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
            if (skillData == null) return;

            // Allow skill types that use AOE-style effects (범위 이펙트가 필요한 스킬 타입들)
            var skillType = skillData.GetSkillType();
            bool isValidType = skillType == SkillAssetType.AOE
                            || skillType == SkillAssetType.DOT
                            || skillType == SkillAssetType.Debuff
                            || skillType == SkillAssetType.Trap
                            || skillType == SkillAssetType.Mine
                            || skillType == SkillAssetType.InstantSingle;
            if (!isValidType) return;

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

                if (castTime > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);

                    await UniTask.Delay((int)(castTime * 1000));

                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // 2. Get target position - 밀집 지역 기반 타겟팅
                // AOE 스킬은 몬스터 origin이 아닌 가장 밀집된 Area를 타겟으로 함
                float aoeRadius = skillData.aoe_radius > 0 ? skillData.aoe_radius : 3f;
                if (supportData != null) aoeRadius *= supportData.aoe_mult;

                Vector3 targetPos = FindBestAOETargetPosition(range, aoeRadius);
                if (targetPos == Vector3.zero)
                {
                    Debug.Log("[Character] AOE cancelled: No valid targets in range");
                    return;
                }

                // 3. Ground impact position
                Vector3 impactPos = targetPos;
                Ray groundRay = new Ray(targetPos + Vector3.up * 10f, Vector3.down);
                if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
                {
                    impactPos = groundHit.point;
                }
                else
                {
                    impactPos = new Vector3(targetPos.x, 0f, targetPos.z);
                }

                // 4. Meteor Effect (only if projectile_speed > 0, otherwise instant AOE)
                if (skillData.projectile_speed > 0 && projectileEffectPrefab != null)
                {
                    Vector3 meteorStartPos = impactPos + Vector3.up * 20f;
                    meteorEffect = UnityEngine.Object.Instantiate(projectileEffectPrefab, meteorStartPos, Quaternion.identity);

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

                // 5. Hit Effect - AOE 범위에 맞춰 스케일 조절
                // (표식/CC 스킬은 각 타겟에 개별 적용하므로 중앙 이펙트 생략)
                bool skipCentralEffect = skillData.HasMarkEffect || skillData.HasCCEffect;
                if (hitEffectPrefab != null && !skipCentralEffect)
                {
                    hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, impactPos, Quaternion.identity);

                    // 기본 이펙트 크기를 25 단위로 가정하고 aoeRadius에 비례하여 스케일 조절
                    // aoe_radius 150 → 스케일 6, aoe_radius 400 → 스케일 16
                    float baseEffectSize = 25f;
                    float scaleFactor = aoeRadius / baseEffectSize;
                    hitEffect.transform.localScale = Vector3.one * scaleFactor;
                }

                // 6. AOE damage - aoeRadius는 위에서 이미 계산됨
                Collider[] hits = Physics.OverlapSphere(impactPos, aoeRadius);
                float damageToApply = damage;

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hit = hits[i];
                    if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                        continue;

                    ITargetable hitTarget = hit.GetComponent<ITargetable>();
                    if (hitTarget == null || !hitTarget.IsAlive())
                        continue;

                    // 각 대상에게 히트 이펙트 생성 (표식/CC 스킬은 ApplyMark/ApplyCC에서 처리하므로 제외)
                    if (hitEffectPrefab != null && !skipCentralEffect)
                    {
                        Vector3 hitTargetPos = hitTarget.GetPosition();
                        GameObject targetHitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitTargetPos + Vector3.up, Quaternion.identity);
                        UnityEngine.Object.Destroy(targetHitEffect, 1f);
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
                if (meteorEffect != null)
                {
                    UnityEngine.Object.Destroy(meteorEffect, 0.1f);
                }
                if (hitEffect != null)
                {
                    UnityEngine.Object.Destroy(hitEffect, 2f);
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

                if (castTime > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
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
                if (projectileEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    startEffect = UnityEngine.Object.Instantiate(projectileEffectPrefab, spawnPos, Quaternion.identity);
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

                // 3. Build chain targets (if Chain support skill is active)
                List<ITargetable> chainTargets = BuildChainTargets(target);

                // 4. Create beam effects for all targets
                // 빔 이펙트가 Wall을 통과하도록 레이어 설정 및 Collider 비활성화
                if (areaEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        Vector3 spawnPos = (i == 0) ? transform.position + spawnOffset : chainTargets[i - 1].GetPosition();
                        GameObject beamEffect = UnityEngine.Object.Instantiate(areaEffectPrefab, spawnPos, Quaternion.identity);

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
                    Debug.Log($"[Character] Created {beamEffects.Count} beam effects for {chainTargets.Count} targets (beamCollides disabled)");
                }

                // 5. Create hit effects for all targets
                // 히트 이펙트도 Wall 통과 처리
                if (hitEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);
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

                while (elapsed < finalChannelDuration)
                {
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
                            if (hitEffectPrefab != null)
                            {
                                GameObject tickHitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);

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

            try
            {
                Debug.Log($"[Character] Starting Buff skill: {skillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = prefabs?.castEffectPrefab;
                GameObject hitEffectPrefab = prefabs?.hitEffectPrefab;

                // 1. Cast Effect
                float castTime = skillData.cast_time;
                if (supportData != null) castTime *= supportData.cast_time_mult;

                if (castTime > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
                    await UniTask.Delay((int)(castTime * 1000));
                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // 2. Apply buff effect to allies in range
                float buffValue = skillData.base_buff_value;
                if (supportData != null) buffValue *= supportData.buff_value_mult;

                float buffDuration = skillData.skill_lifetime;
                if (supportData != null) buffDuration *= supportData.buff_value_mult; // 지속시간도 배율 적용

                BuffType buffType = skillData.GetBuffType();
                float buffRadius = skillData.aoe_radius > 0 ? skillData.aoe_radius : 400f; // 기본 범위

                // 범위 내 아군 캐릭터에게 버프 적용
                ApplyBuffToAlliesInRange(buffType, buffValue, buffDuration, buffRadius);

                Debug.Log($"[Character] Buff skill applied: {skillData.skill_name} (Type: {buffType}, Value: {buffValue}%, Duration: {buffDuration}s, Radius: {buffRadius})");

                // 3. 버프 지속시간 동안 이펙트 반복 재생
                if (hitEffectPrefab != null && buffDuration > 0)
                {
                    float effectTickInterval = 1f; // 1초마다 이펙트 재생
                    float elapsed = 0f;

                    while (elapsed < buffDuration)
                    {
                        Vector3 effectPos = transform.position;
                        GameObject tickEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, effectPos, Quaternion.identity);
                        UnityEngine.Object.Destroy(tickEffect, 0.8f); // 이펙트 0.8초 후 삭제

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
                        // 경험치 버프는 별도 시스템 필요
                        Debug.Log($"[Character] EXP buff applied to {ally.name}: +{buffValue}%");
                        break;
                }
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

                    // Spawn hit effect at boss center
                    if (hitEffectPrefab != null)
                    {
                        GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
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

                    if (hpRatio <= instantKillThreshold)
                    {
                        // Instant kill!
                        monster.Die();

                        // Spawn hit effect at monster center
                        if (hitEffectPrefab != null)
                        {
                            GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                            UnityEngine.Object.Destroy(hitEffect, 2f);
                        }

                        Debug.Log($"[Character] InstantKill SUCCESS: {monster.name} (HP: {hpRatio * 100:F1}% <= {instantKillThreshold * 100}%)");
                    }
                    else
                    {
                        // HP too high: apply normal damage (상성 배율 적용)
                        float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(GetGenre(), monster.GetGenre());
                        monster.TakeDamage(FinalDamage * genreMultiplier);

                        // Spawn hit effect at monster center
                        if (hitEffectPrefab != null)
                        {
                            GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
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
    }
}
