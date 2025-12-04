//LMJ : Trap object that is placed on the field
//      Continuously affects enemies within range for skill_lifetime duration
//      Used by Trap type skills (3000807): 장미의가시, 슬랩스틱존, 현장보존, 트릭와이어, 피웅덩이, 깜짝카메라
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;
    using NovelianMagicLibraryDefense.Managers;

    public class TrapObject : MonoBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField] private float tickInterval = 1f; // 효과 적용 간격

        // Skill data
        private MainSkillData skillData;
        private MainSkillPrefabEntry skillPrefabs;
        private SupportSkillData supportData;
        private float damage;
        private float lifetime;
        private float aoeRadius;
        private float elapsedTime;
        private bool isActive = false;

        // Effect tracking
        private GameObject areaEffectInstance;
        private HashSet<int> affectedTargets = new HashSet<int>(); // CC 중복 방지용
        private CancellationTokenSource lifetimeCts;

        //LMJ : Initialize and activate trap
        public void Initialize(MainSkillData data, MainSkillPrefabEntry prefabs, SupportSkillData support, float trapDamage, Vector3 position)
        {
            skillData = data;
            skillPrefabs = prefabs;
            supportData = support;
            damage = trapDamage;

            // Calculate lifetime and radius with support modifiers
            lifetime = data.skill_lifetime > 0 ? data.skill_lifetime : 10f;
            aoeRadius = data.aoe_radius > 0 ? data.aoe_radius : 3f;
            if (support != null)
            {
                aoeRadius *= support.aoe_mult;
            }

            // Set tick interval from skill data (DOT tick interval or default)
            if (data.dot_tick_interval > 0)
            {
                tickInterval = data.dot_tick_interval;
            }

            // Position the trap
            transform.position = position;

            // Spawn area effect (projectile prefab as trap visual)
            if (prefabs?.projectilePrefab != null)
            {
                areaEffectInstance = Object.Instantiate(prefabs.projectilePrefab, position, Quaternion.identity);
                areaEffectInstance.transform.SetParent(transform);

                // Scale effect to match aoe_radius
                float baseSize = 100f;
                float scaleFactor = aoeRadius / baseSize;
                areaEffectInstance.transform.localScale = Vector3.one * scaleFactor;
            }

            // Add trigger collider for detection
            SphereCollider triggerCol = gameObject.AddComponent<SphereCollider>();
            triggerCol.isTrigger = true;
            triggerCol.radius = aoeRadius;

            // Set layer
            int trapLayer = LayerMask.NameToLayer("Projectile");
            if (trapLayer >= 0) gameObject.layer = trapLayer;

            isActive = true;
            elapsedTime = 0f;
            affectedTargets.Clear();

            Debug.Log($"[TrapObject] Initialized: {data.skill_name}, lifetime={lifetime}s, radius={aoeRadius}, damage={damage}");

            // Start lifetime tracking
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();
            TrapLifetimeAsync(lifetimeCts.Token).Forget();
        }

        //LMJ : Trap lifetime and periodic effect application
        private async UniTaskVoid TrapLifetimeAsync(CancellationToken ct)
        {
            float nextTickTime = 0f;

            try
            {
                while (elapsedTime < lifetime && !ct.IsCancellationRequested)
                {
                    // Apply effects at tick intervals
                    if (elapsedTime >= nextTickTime)
                    {
                        ApplyEffectsToTargetsInRange();
                        nextTickTime += tickInterval;
                    }

                    await UniTask.Yield(ct);
                    elapsedTime += Time.deltaTime;
                }

                Debug.Log($"[TrapObject] Lifetime expired, destroying");
            }
            catch (System.OperationCanceledException)
            {
                // Expected
            }
            finally
            {
                DestroyTrap();
            }
        }

        //LMJ : Apply effects to all enemies within range
        private void ApplyEffectsToTargetsInRange()
        {
            if (!isActive || skillData == null) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius);
            int hitCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                if (hit.CompareTag(Tag.Monster))
                {
                    Monster monster = hit.GetComponent<Monster>();
                    if (monster != null && monster.IsAlive())
                    {
                        ApplyTrapEffectToMonster(monster, hit);
                        hitCount++;
                    }
                }
                else if (hit.CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = hit.GetComponent<BossMonster>();
                    if (boss != null && boss.IsAlive())
                    {
                        ApplyTrapEffectToBoss(boss, hit);
                        hitCount++;
                    }
                }
            }

            if (hitCount > 0)
            {
                Debug.Log($"[TrapObject] Applied effects to {hitCount} targets");
            }
        }

        //LMJ : Apply trap effect to monster
        private void ApplyTrapEffectToMonster(Monster monster, Collider col)
        {
            int instanceId = monster.GetInstanceID();

            // Apply damage (DOT damage per tick)
            float damageToApply = skillData.dot_damage_per_tick > 0 ? skillData.dot_damage_per_tick : damage;
            if (damageToApply > 0)
            {
                monster.TakeDamage(damageToApply);
            }

            // Apply CC effect (only once per target to prevent stacking)
            if (skillData.HasCCEffect && !affectedTargets.Contains(instanceId))
            {
                affectedTargets.Add(instanceId);
                GameObject ccEffectPrefab = skillPrefabs?.hitEffectPrefab;
                monster.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
                Debug.Log($"[TrapObject] Applied CC to {monster.name}: {skillData.GetCCType()}, duration={skillData.cc_duration}s");
            }

            // Apply DOT effect (continuous burn/poison)
            if (skillData.HasDOTEffect)
            {
                // DOT is already applied as tick damage, but we can also apply a status effect
                GameObject dotEffectPrefab = skillPrefabs?.hitEffectPrefab;
                // Don't stack DOT - it's handled by tick damage
            }

            // Spawn hit effect at monster center
            SpawnHitEffectAtTarget(col);
        }

        //LMJ : Apply trap effect to boss
        private void ApplyTrapEffectToBoss(BossMonster boss, Collider col)
        {
            int instanceId = boss.GetInstanceID();

            // Apply damage (DOT damage per tick)
            float damageToApply = skillData.dot_damage_per_tick > 0 ? skillData.dot_damage_per_tick : damage;
            if (damageToApply > 0)
            {
                boss.TakeDamage(damageToApply);
            }

            // Apply CC effect (only once per target)
            if (skillData.HasCCEffect && !affectedTargets.Contains(instanceId))
            {
                affectedTargets.Add(instanceId);
                GameObject ccEffectPrefab = skillPrefabs?.hitEffectPrefab;
                boss.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
            }

            // Spawn hit effect at boss center
            SpawnHitEffectAtTarget(col);
        }

        //LMJ : Spawn hit effect at target's collider center
        private void SpawnHitEffectAtTarget(Collider col)
        {
            if (skillPrefabs?.hitEffectPrefab == null) return;

            // Use collider bounds center for proper positioning
            Vector3 hitPos = col.bounds.center;
            GameObject hitEffect = Object.Instantiate(skillPrefabs.hitEffectPrefab, hitPos, Quaternion.identity);
            Object.Destroy(hitEffect, 1f);
        }

        //LMJ : Clean up and destroy trap
        private void DestroyTrap()
        {
            isActive = false;

            if (areaEffectInstance != null)
            {
                Object.Destroy(areaEffectInstance);
            }

            Object.Destroy(gameObject);
        }

        private void OnDestroy()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
        }
    }
}
