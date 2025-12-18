//LMJ : Mine object that is placed on the field
//      Explodes when an enemy steps on it (OnTriggerEnter)
//      Used by Mine type skills (3000908): 저주 마법, 봉인 마법진, 얼음 결정, 마검 지역 생성, 섬광구
//      Supports chain explosion (40019 연쇄폭발) - spawns additional mines on explosion
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using NovelianMagicLibraryDefense.Managers;

    public class MineObject : MonoBehaviour
    {
        // Skill data
        private MainSkillData skillData;
        private MainSkillPrefabEntry skillPrefabs;
        private SupportSkillData supportData;
        private float damage;
        private float lifetime;
        private float aoeRadius;
        private bool isActive = false;
        private bool hasExploded = false;

        // Effect tracking
        private GameObject areaEffectInstance;
        private CancellationTokenSource lifetimeCts;

        // Genre system (상성 시스템)
        private Genre attackerGenre = Genre.Horror;

        // Chain explosion (연쇄폭발)
        private int remainingChains = 0;

        //LMJ : Initialize and arm the mine
        public void Initialize(MainSkillData data, MainSkillPrefabEntry prefabs, SupportSkillData support, float mineDamage, Vector3 position, Genre genre = Genre.Horror)
        {
            skillData = data;
            skillPrefabs = prefabs;
            supportData = support;
            damage = mineDamage;
            attackerGenre = genre;

            // Calculate lifetime and radius with support modifiers
            lifetime = data.skill_lifetime > 0 ? data.skill_lifetime : 30f; // Mines last longer
            aoeRadius = data.aoe_radius > 0 ? data.aoe_radius : 3f;
            if (support != null)
            {
                aoeRadius *= support.aoe_mult;
            }

            // Position the mine
            transform.position = position;

            // Spawn mine visual (projectile prefab)
            if (prefabs?.projectilePrefab != null)
            {
                areaEffectInstance = Object.Instantiate(prefabs.projectilePrefab, position, Quaternion.identity);
                areaEffectInstance.transform.SetParent(transform);

                // Mines are smaller visually (not scaled to aoe_radius)
                // Keep original scale or slightly scale down
            }

            // Add trigger collider for step detection
            SphereCollider triggerCol = gameObject.AddComponent<SphereCollider>();
            triggerCol.isTrigger = true;
            triggerCol.radius = 1.5f; // Detection radius (smaller than explosion radius)

            // Add Rigidbody for trigger detection
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Set layer
            int mineLayer = LayerMask.NameToLayer("Projectile");
            if (mineLayer >= 0) gameObject.layer = mineLayer;

            // 연쇄폭발 (40019) 설정
            if (support != null && support.chain_count > 0)
            {
                remainingChains = support.chain_count;
            }

            isActive = true;
            hasExploded = false;

            Debug.Log($"[MineObject] Initialized: {data.skill_name}, lifetime={lifetime}s, explosionRadius={aoeRadius}, damage={damage}, chains={remainingChains}");

            // Start lifetime tracking
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();
            MineLifetimeAsync(lifetimeCts.Token).Forget();
        }

        //LMJ : Mine lifetime tracking (auto-destroy if not triggered)
        private async UniTaskVoid MineLifetimeAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay((int)(lifetime * 1000), cancellationToken: ct);

                if (!hasExploded && !ct.IsCancellationRequested)
                {
                    Debug.Log($"[MineObject] Lifetime expired without trigger, destroying");
                    DestroyMine();
                }
            }
            catch (System.OperationCanceledException)
            {
                // Expected when mine explodes before lifetime ends
            }
        }

        //LMJ : Trigger detection - explode when enemy steps on mine
        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || hasExploded) return;

            // Check if it's an enemy
            if (other.CompareTag(Tag.Monster) || other.CompareTag(Tag.BossMonster))
            {
                Debug.Log($"[MineObject] Triggered by {other.name}");
                Explode();
            }
        }

        //LMJ : Explode the mine - damage and effects to all enemies in aoe_radius
        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;
            isActive = false;
            lifetimeCts?.Cancel();

            Vector3 explosionPos = transform.position;
            Debug.Log($"[MineObject] Exploding at {explosionPos}, radius={aoeRadius}");

            // Spawn explosion effect (hit effect)
            if (skillPrefabs?.hitEffectPrefab != null)
            {
                GameObject explosionEffect = Object.Instantiate(skillPrefabs.hitEffectPrefab, explosionPos, Quaternion.identity);

                // Scale explosion effect to match aoe_radius (baseSize 25로 이펙트 크기 확대)
                float baseSize = 25f;
                float scaleFactor = aoeRadius / baseSize;
                explosionEffect.transform.localScale = Vector3.one * scaleFactor;

                Object.Destroy(explosionEffect, 2f);
            }

            // Find all enemies in explosion radius
            Collider[] hits = Physics.OverlapSphere(explosionPos, aoeRadius);
            int hitCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                if (hit.CompareTag(Tag.Monster))
                {
                    Monster monster = hit.GetComponent<Monster>();
                    if (monster != null && monster.IsAlive())
                    {
                        ApplyMineEffectToMonster(monster, hit);
                        hitCount++;
                    }
                }
                else if (hit.CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = hit.GetComponent<BossMonster>();
                    if (boss != null && boss.IsAlive())
                    {
                        ApplyMineEffectToBoss(boss, hit);
                        hitCount++;
                    }
                }
            }

            Debug.Log($"[MineObject] Explosion hit {hitCount} targets");

            // 연쇄폭발: 폭발 시 주변에 추가 지뢰 생성
            if (remainingChains > 0)
            {
                SpawnChainMines();
            }

            // Destroy mine after explosion
            DestroyMine();
        }

        //LMJ : Apply mine explosion effect to monster
        private void ApplyMineEffectToMonster(Monster monster, Collider col)
        {
            // Apply damage with genre affinity (상성 시스템)
            if (damage > 0)
            {
                float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, monster.GetGenre());
                monster.TakeDamage(damage * genreMultiplier);
            }

            // Apply CC effect (Stun/Slow)
            if (skillData.HasCCEffect)
            {
                GameObject ccEffectPrefab = skillPrefabs?.hitEffectPrefab;
                monster.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
                Debug.Log($"[MineObject] Applied CC to {monster.name}: {skillData.GetCCType()}, duration={skillData.cc_duration}s");
            }

            // Apply Debuff effect (저주받은인형 등)
            if (skillData.HasDebuffEffect)
            {
                // Apply debuff through monster's debuff system
                // The debuff makes the target take more damage
                Debug.Log($"[MineObject] Applied Debuff to {monster.name}: {skillData.GetDeBuffType()}, value={skillData.base_debuff_value}");
            }

            // Spawn individual hit effect at monster center
            SpawnHitEffectAtTarget(col);
        }

        //LMJ : Apply mine explosion effect to boss
        private void ApplyMineEffectToBoss(BossMonster boss, Collider col)
        {
            // Apply damage with genre affinity (상성 시스템)
            if (damage > 0)
            {
                float genreMultiplier = DamageCalculator.CalculateGenreMultiplier(attackerGenre, boss.GetGenre());
                boss.TakeDamage(damage * genreMultiplier);
            }

            // Apply CC effect (Stun/Slow)
            if (skillData.HasCCEffect)
            {
                GameObject ccEffectPrefab = skillPrefabs?.hitEffectPrefab;
                boss.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
            }

            // Spawn individual hit effect at boss center
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

        //LMJ : Clean up and destroy mine
        private void DestroyMine()
        {
            if (areaEffectInstance != null)
            {
                Object.Destroy(areaEffectInstance);
            }

            Object.Destroy(gameObject);
        }

        //LMJ : 연쇄폭발 - 주변에 추가 지뢰 생성
        private void SpawnChainMines()
        {
            if (supportData == null || remainingChains <= 0) return;

            float chainRange = supportData.chain_range > 0 ? supportData.chain_range : 10f;
            float damageReduction = supportData.chain_damage_reduction / 100f;
            float chainDamage = damage * (1f - damageReduction);

            Debug.Log($"[MineObject] Spawning {remainingChains} chain mines, range={chainRange}, damage={chainDamage}");

            // 주변 랜덤 위치에 연쇄 지뢰 생성
            for (int i = 0; i < remainingChains; i++)
            {
                // 원형 범위 내 랜덤 위치
                Vector2 randomCircle = Random.insideUnitCircle * chainRange;
                Vector3 chainPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                // 지면 높이 맞추기
                Ray groundRay = new Ray(chainPos + Vector3.up * 10f, Vector3.down);
                if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
                {
                    chainPos = groundHit.point;
                }
                else
                {
                    chainPos.y = 0f;
                }

                // 연쇄 지뢰 생성 (chain_count=0으로 설정하여 무한 연쇄 방지)
                GameObject chainMineObj = new GameObject($"ChainMine_{skillData.skill_name}_{i}");
                MineObject chainMine = chainMineObj.AddComponent<MineObject>();

                // 연쇄 지뢰는 chain_count를 0으로 설정 (무한 연쇄 방지)
                SupportSkillData chainSupportData = null; // 연쇄 지뢰는 서포트 효과 없음

                chainMine.Initialize(skillData, skillPrefabs, chainSupportData, chainDamage, chainPos, attackerGenre);

                Debug.Log($"[MineObject] Chain mine {i + 1} spawned at {chainPos}");
            }
        }

        private void OnDestroy()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
        }
    }
}
