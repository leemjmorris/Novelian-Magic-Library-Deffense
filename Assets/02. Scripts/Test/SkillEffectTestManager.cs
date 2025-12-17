using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스킬 이펙트 테스트 매니저
/// DevScene에서 SpecialSkillsEffectsPack 이펙트를 테스트할 수 있는 도구
///
/// 사용법:
/// 1. 씬에 빈 GameObject 생성 후 이 컴포넌트 추가
/// 2. SkillEffectManager가 있는 씬에서 실행
/// 3. Inspector에서 skillIdToTest 설정 후 스페이스바로 테스트
/// </summary>
public class SkillEffectTestManager : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField, Tooltip("테스트할 스킬 ID (MainSkillTable)")]
    private int skillIdToTest = 39001;

    [SerializeField, Tooltip("스폰 위치 오프셋")]
    private Vector3 spawnOffset = Vector3.zero;

    [SerializeField, Tooltip("데미지 배율")]
    private float damageMultiplier = 1f;

    [Header("타겟 설정")]
    [SerializeField, Tooltip("타겟 트랜스폼 (선택사항)")]
    private Transform targetTransform;

    [SerializeField, Tooltip("자동 타겟 탐색 (TargetRegistry 사용)")]
    private bool useAutoTargeting = true;

    [SerializeField, Tooltip("타겟 탐색 범위")]
    private float targetSearchRange = 1000f;

    [Header("연속 테스트 설정")]
    [SerializeField, Tooltip("연속 테스트 모드")]
    private bool continuousTestMode = false;

    [SerializeField, Tooltip("연속 테스트 간격 (초)")]
    private float continuousTestInterval = 2f;

    [Header("이펙트 브라우저")]
    [SerializeField, Tooltip("데이터베이스에서 테스트할 스킬 ID 목록")]
    private List<int> skillIdsToPrewarm = new List<int>();

    [Header("디버그")]
    [SerializeField, Tooltip("디버그 로그 출력")]
    private bool showDebugLogs = true;

    // 내부 상태
    private float lastTestTime;
    private bool isInitialized = false;
    private SkillEffectWrapper lastSpawnedWrapper;

    private void Start()
    {
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        // SkillEffectManager가 초기화될 때까지 대기
        while (SkillEffectManager.Instance == null)
        {
            await UniTask.Yield();
        }

        // 프리웜 스킬들 초기화
        if (skillIdsToPrewarm.Count > 0)
        {
            await SkillEffectBridge.PrewarmSkillEffects(skillIdsToPrewarm.ToArray());
        }

        isInitialized = true;
        Log("SkillEffectTestManager 초기화 완료");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 스페이스바로 테스트
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestSpawnEffect();
        }

        // F1: 히트 이펙트 테스트
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestSpawnHitEffect();
        }

        // F2: 시전 이펙트 테스트
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestSpawnCastEffect();
        }

        // F3: 모든 이펙트 회수
        if (Input.GetKeyDown(KeyCode.F3))
        {
            DespawnAllEffects();
        }

        // F4: 디버그 정보 출력
        if (Input.GetKeyDown(KeyCode.F4))
        {
            PrintDebugInfo();
        }

        // 연속 테스트 모드
        if (continuousTestMode && Time.time - lastTestTime > continuousTestInterval)
        {
            TestSpawnEffect();
            lastTestTime = Time.time;
        }
    }

    /// <summary>
    /// 메인 이펙트 스폰 테스트
    /// </summary>
    public void TestSpawnEffect()
    {
        TestSpawnEffectAsync().Forget();
    }

    private async UniTaskVoid TestSpawnEffectAsync()
    {
        Vector3 spawnPosition = transform.position + spawnOffset;
        Transform target = GetTarget();

        Log($"이펙트 스폰 테스트: Skill {skillIdToTest} at {spawnPosition}");

        // 새 시스템으로 스폰 시도
        bool success = await SkillEffectBridge.TrySpawnSkillEffectToward(
            skillIdToTest,
            spawnPosition,
            target,
            damageMultiplier
        );

        if (success)
        {
            Log($"[성공] 스킬 {skillIdToTest} 이펙트 스폰됨 (새 시스템)");
        }
        else
        {
            // 매핑되지 않은 경우 직접 스폰
            var manager = SkillEffectManager.Instance;
            if (manager != null)
            {
                lastSpawnedWrapper = await manager.SpawnSkillEffect(
                    skillIdToTest,
                    spawnPosition,
                    Quaternion.identity,
                    target,
                    damageMultiplier
                );

                if (lastSpawnedWrapper != null)
                {
                    Log($"[성공] 스킬 {skillIdToTest} 이펙트 스폰됨 (직접 스폰)");
                }
                else
                {
                    LogWarning($"[실패] 스킬 {skillIdToTest}에 매핑된 이펙트가 없습니다");
                }
            }
        }
    }

    /// <summary>
    /// 히트 이펙트 스폰 테스트
    /// </summary>
    public void TestSpawnHitEffect()
    {
        Vector3 hitPosition = transform.position + spawnOffset;
        Transform target = GetTarget();

        if (target != null)
        {
            hitPosition = target.position;
        }

        Log($"히트 이펙트 테스트: Skill {skillIdToTest} at {hitPosition}");

        var effect = SkillEffectBridge.TrySpawnHitEffect(skillIdToTest, hitPosition);
        if (effect != null)
        {
            Log("[성공] 히트 이펙트 스폰됨");
        }
        else
        {
            LogWarning("[실패] 히트 이펙트가 매핑되지 않았습니다");
        }
    }

    /// <summary>
    /// 시전 이펙트 스폰 테스트
    /// </summary>
    public void TestSpawnCastEffect()
    {
        Vector3 castPosition = transform.position + spawnOffset;

        Log($"시전 이펙트 테스트: Skill {skillIdToTest} at {castPosition}");

        var effect = SkillEffectBridge.TrySpawnCastEffect(skillIdToTest, castPosition, transform);
        if (effect != null)
        {
            Log("[성공] 시전 이펙트 스폰됨");
        }
        else
        {
            LogWarning("[실패] 시전 이펙트가 매핑되지 않았습니다");
        }
    }

    /// <summary>
    /// 모든 활성 이펙트 회수
    /// </summary>
    public void DespawnAllEffects()
    {
        SkillEffectBridge.DespawnAllEffects();
        Log("모든 이펙트 회수됨");
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public void PrintDebugInfo()
    {
        SkillEffectBridge.LogDebugInfo();

        var database = SkillEffectDatabase.Instance;
        if (database != null)
        {
            bool isMapped = SkillEffectBridge.IsSkillMapped(skillIdToTest);
            var entry = database.GetEntry(skillIdToTest);

            Debug.Log($"[SkillEffectTest] 스킬 {skillIdToTest} 정보:\n" +
                      $"  매핑 여부: {isMapped}\n" +
                      $"  엔트리 존재: {entry != null}\n" +
                      $"  메인 이펙트: {(entry?.mainEffectPrefab != null ? entry.mainEffectPrefab.name : "없음")}\n" +
                      $"  히트 이펙트: {(entry?.hitEffectPrefab != null ? entry.hitEffectPrefab.name : "없음")}\n" +
                      $"  시전 이펙트: {(entry?.castEffectPrefab != null ? entry.castEffectPrefab.name : "없음")}");
        }
    }

    /// <summary>
    /// 타겟 가져오기
    /// </summary>
    private Transform GetTarget()
    {
        // 명시적 타겟이 있으면 사용
        if (targetTransform != null)
        {
            return targetTransform;
        }

        // 자동 타겟팅
        if (useAutoTargeting && TargetRegistry.Instance != null)
        {
            var target = TargetRegistry.Instance.FindTarget(transform.position, targetSearchRange);
            return target?.GetTransform();
        }

        return null;
    }

    /// <summary>
    /// 스킬 ID 변경 (런타임)
    /// </summary>
    public void SetSkillId(int newSkillId)
    {
        skillIdToTest = newSkillId;
        Log($"테스트 스킬 ID 변경: {newSkillId}");
    }

    /// <summary>
    /// 연속 테스트 모드 토글
    /// </summary>
    public void ToggleContinuousMode()
    {
        continuousTestMode = !continuousTestMode;
        lastTestTime = Time.time;
        Log($"연속 테스트 모드: {(continuousTestMode ? "ON" : "OFF")}");
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SkillEffectTest] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"[SkillEffectTest] {message}");
        }
    }

    private void OnGUI()
    {
        if (!isInitialized) return;

        // 간단한 UI 표시
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== 스킬 이펙트 테스트 ===");
        GUILayout.Label($"현재 스킬 ID: {skillIdToTest}");
        GUILayout.Label($"연속 테스트: {(continuousTestMode ? "ON" : "OFF")}");
        GUILayout.Space(10);
        GUILayout.Label("조작법:");
        GUILayout.Label("  Space: 메인 이펙트 스폰");
        GUILayout.Label("  F1: 히트 이펙트 스폰");
        GUILayout.Label("  F2: 시전 이펙트 스폰");
        GUILayout.Label("  F3: 모든 이펙트 회수");
        GUILayout.Label("  F4: 디버그 정보");
        GUILayout.EndArea();
    }
}
