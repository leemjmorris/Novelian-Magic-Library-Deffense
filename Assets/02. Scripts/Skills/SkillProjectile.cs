using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Novelian.Combat
{
    /// <summary>
    /// 스킬 투사체 - 이동, 충돌, 데미지 처리
    /// </summary>
    public class SkillProjectile : MonoBehaviour
    {
        [Header("Runtime Data")]
        private MainSkillData mainSkill;
        private SupportSkillData supportSkill;
        private ITargetable target;
        private bool isExplosive;
        private GameObject hitPrefab; // 피격/폭발 이펙트 프리팹

        [Header("State")]
        private bool isInitialized;
        private int pierceCount;
        private int chainCount;
        private float damage;
        private float speed;
        private float maxRange;
        private Vector3 startPosition;

        [Header("Homing")]
        private bool isHoming;
        private float homingStrength;

        public void Initialize(MainSkillData main, SupportSkillData support, ITargetable targetable, bool explosive, GameObject hitEffectPrefab = null)
        {
            mainSkill = main;
            supportSkill = support;
            target = targetable;
            isExplosive = explosive;
            hitPrefab = hitEffectPrefab;

            // 기본 값 설정
            damage = SkillExecutor.CalculateDamage(main, support);
            speed = main.projectile_speed > 0 ? main.projectile_speed : 15f;
            maxRange = main.range > 0 ? main.range : 20f;
            startPosition = transform.position;

            // 서포트 스킬 효과 적용
            if (support != null)
            {
                // 관통
                if (support.IsPierceSupport)
                {
                    pierceCount = support.count;
                }

                // 연쇄
                if (support.IsChainSupport)
                {
                    chainCount = support.count;
                }

                // 유도
                if (support.IsHomingSupport)
                {
                    isHoming = true;
                    homingStrength = support.homing_strength;
                }
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            // 유도 처리
            if (isHoming && target != null && target.IsAlive())
            {
                Vector3 targetDir = (target.GetTransform().position - transform.position).normalized;
                Vector3 newDir = Vector3.Lerp(transform.forward, targetDir, homingStrength * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(newDir);
            }

            // Raycast로 이동 경로상 충돌 체크 (터널링 방지)
            float moveDistance = speed * Time.deltaTime;
            int monsterLayerMask = LayerMask.GetMask("Monster");

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, moveDistance + 0.5f, monsterLayerMask))
            {
                // 몬스터와 충돌 감지됨 - 충돌 지점으로 이동 후 처리
                transform.position = hit.point;
                HandleHit(hit.collider);
                return;
            }

            // 이동
            transform.position += transform.forward * moveDistance;

            // 최대 사거리 체크
            if (Vector3.Distance(startPosition, transform.position) > maxRange)
            {
                DestroyProjectile();
            }
        }

        /// <summary>
        /// Raycast 충돌 처리
        /// </summary>
        private void HandleHit(Collider other)
        {
            if (!isInitialized) return;

            // ITargetable 가져오기
            ITargetable hitTarget = other.GetComponent<Monster>();
            if (hitTarget == null)
            {
                hitTarget = other.GetComponent<BossMonster>();
            }

            if (hitTarget == null || !hitTarget.IsAlive())
            {
                return;
            }

            // 데미지 적용
            hitTarget.TakeDamage(damage);

            // 폭발형 처리
            if (isExplosive && mainSkill.aoe_radius > 0)
            {
                ExplodeAsync().Forget();
                return;
            }

            // 관통 처리
            if (pierceCount > 0)
            {
                pierceCount--;
                if (supportSkill != null && supportSkill.chain_decay > 0)
                {
                    damage *= supportSkill.chain_decay;
                }
                return;
            }

            // 연쇄 처리
            if (chainCount > 0)
            {
                ChainToNextTarget(hitTarget).Forget();
                return;
            }

            // 일반 충돌 - Hit Effect 재생 후 파괴
            SpawnHitEffect(transform.position);
            DestroyProjectile();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized) return;

            // 몬스터 또는 보스 태그 체크
            if (!other.CompareTag(Tag.Monster) && !other.CompareTag(Tag.BossMonster))
            {
                return;
            }

            // ITargetable 가져오기
            ITargetable hitTarget = null;
            if (other.CompareTag(Tag.Monster))
            {
                hitTarget = other.GetComponent<Monster>();
            }
            else if (other.CompareTag(Tag.BossMonster))
            {
                hitTarget = other.GetComponent<BossMonster>();
            }

            if (hitTarget == null || !hitTarget.IsAlive())
            {
                return;
            }

            // 데미지 적용
            hitTarget.TakeDamage(damage);

            // 폭발형 처리
            if (isExplosive && mainSkill.aoe_radius > 0)
            {
                ExplodeAsync().Forget();
                return;
            }

            // 관통 처리
            if (pierceCount > 0)
            {
                pierceCount--;
                // 관통 시 데미지 감소 (서포트 스킬의 chain_decay 사용)
                if (supportSkill != null && supportSkill.chain_decay > 0)
                {
                    damage *= supportSkill.chain_decay;
                }
                return; // 계속 진행
            }

            // 연쇄 처리
            if (chainCount > 0)
            {
                ChainToNextTarget(hitTarget).Forget();
                return;
            }

            // 일반 충돌 - Hit Effect 재생 후 파괴
            SpawnHitEffect(transform.position);
            DestroyProjectile();
        }

        private async UniTaskVoid ExplodeAsync()
        {
            float radius = mainSkill.aoe_radius;

            // 폭발 이펙트 스폰
            SpawnHitEffect(transform.position);

            // 범위 내 적 탐색
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

            foreach (var col in colliders)
            {
                if (!col.CompareTag(Tag.Monster) && !col.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable explosionTarget = null;
                if (col.CompareTag(Tag.Monster))
                {
                    explosionTarget = col.GetComponent<Monster>();
                }
                else if (col.CompareTag(Tag.BossMonster))
                {
                    explosionTarget = col.GetComponent<BossMonster>();
                }

                if (explosionTarget != null && explosionTarget.IsAlive())
                {
                    // 폭발 데미지 (서포트 스킬 explosion_ratio 적용)
                    float explosionDamage = damage;
                    if (supportSkill != null && supportSkill.explosion_ratio > 0)
                    {
                        explosionDamage *= supportSkill.explosion_ratio;
                    }

                    explosionTarget.TakeDamage(explosionDamage);
                }
            }

            await UniTask.Yield();
            DestroyProjectile();
        }

        private async UniTaskVoid ChainToNextTarget(ITargetable hitTarget)
        {
            chainCount--;

            // 연쇄 범위 내 다음 타겟 찾기
            float chainRange = supportSkill?.chain_range ?? 5f;
            Collider[] colliders = Physics.OverlapSphere(transform.position, chainRange);

            ITargetable nextTarget = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (!col.CompareTag(Tag.Monster) && !col.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable candidate = null;
                if (col.CompareTag(Tag.Monster))
                {
                    candidate = col.GetComponent<Monster>();
                }
                else if (col.CompareTag(Tag.BossMonster))
                {
                    candidate = col.GetComponent<BossMonster>();
                }

                // 이전 타겟 제외
                if (candidate == null || !candidate.IsAlive() || candidate == hitTarget)
                    continue;

                float dist = Vector3.Distance(transform.position, candidate.GetTransform().position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nextTarget = candidate;
                }
            }

            if (nextTarget != null)
            {
                // 다음 타겟으로 방향 전환
                target = nextTarget;
                Vector3 newDir = (nextTarget.GetTransform().position - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(newDir);

                // 연쇄 시 데미지 감소
                if (supportSkill != null && supportSkill.chain_decay > 0)
                {
                    damage *= supportSkill.chain_decay;
                }

            }
            else
            {
                // 다음 타겟 없음 - 파괴
                DestroyProjectile();
            }

            await UniTask.Yield();
        }

        private void DestroyProjectile()
        {
            isInitialized = false;
            Destroy(gameObject);
        }

        /// <summary>
        /// 피격/폭발 이펙트 스폰
        /// </summary>
        private void SpawnHitEffect(Vector3 position)
        {
            if (hitPrefab == null) return;

            GameObject hitEffect = Instantiate(hitPrefab, position, Quaternion.identity);

            // 이펙트 자동 삭제 (2초 후)
            Destroy(hitEffect, 2f);
        }
    }
}
