using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Novelian.Combat;

/// <summary>
/// 스킬 시스템 테스트 매니저
/// DevScene에서 스킬과 서포트 스킬을 테스트하기 위한 UI 컨트롤러
/// </summary>
public class SkillTestManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown mainSkillDropdown;
    [SerializeField] private TMP_Dropdown supportSkillDropdown;
    [SerializeField] private Button fireButton;
    [SerializeField] private Button addMonsterButton;
    [SerializeField] private Button removeMonsterButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_Text monsterCountText;

    [Header("Formation Buttons")]
    [SerializeField] private Button linePatternButton;
    [SerializeField] private Button circlePatternButton;
    [SerializeField] private Button randomPatternButton;

    [Header("Formation Settings")]
    [SerializeField] private int formationMonsterCount = 5;
    [SerializeField] private float formationSpacing = 3f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform monsterSpawnArea;
    [SerializeField] private Transform characterSpawnPoint;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private GameObject characterPrefab;

    [Header("Test Character")]
    [SerializeField] private Character testCharacter;

    [Header("조합 규칙 데이터")]
    [SerializeField] private SkillCombinationRuleData combinationRuleData;

    // 데이터
    private List<MainSkillData> mainSkillList = new List<MainSkillData>();
    private List<SupportSkillData> supportSkillList = new List<SupportSkillData>();
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    // 현재 필터링된 서포트 스킬 목록 (드롭다운 인덱스 매핑용)
    private List<SupportSkillData> filteredSupportSkillList = new List<SupportSkillData>();

    private int selectedMainSkillId = 0;
    private int selectedSupportSkillId = 0;

    private async void Start()
    {
        // CSVLoader 초기화 대기
        await WaitForCSVLoaderAsync();

        // 데이터 로드
        LoadSkillData();

        // UI 초기화
        InitializeDropdowns();
        InitializeButtons();

        // 테스트 캐릭터 스폰
        SpawnTestCharacter();

        Log("스킬 테스트 매니저 초기화 완료");
    }

    private async UniTask WaitForCSVLoaderAsync()
    {
        while (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            await UniTask.Delay(100);
        }
        Log("CSVLoader 초기화 완료");
    }

    private void LoadSkillData()
    {
        // MainSkillData 로드
        var mainTable = CSVLoader.Instance.GetTable<MainSkillData>();
        if (mainTable != null)
        {
            mainSkillList = mainTable.GetAll();
            Log($"메인 스킬 {mainSkillList.Count}개 로드됨");
        }

        // SupportSkillData 로드
        var supportTable = CSVLoader.Instance.GetTable<SupportSkillData>();
        if (supportTable != null)
        {
            supportSkillList = supportTable.GetAll();
            Log($"서포트 스킬 {supportSkillList.Count}개 로드됨");
        }

        // 조합 규칙 데이터 확인
        if (combinationRuleData != null)
        {
            Log("조합 규칙 데이터 로드됨");
        }
        else
        {
            Debug.LogWarning("[SkillTestManager] CombinationRuleData가 할당되지 않았습니다. Inspector에서 할당하세요.");
        }
    }

    private void InitializeDropdowns()
    {
        // 메인 스킬 드롭다운
        if (mainSkillDropdown != null)
        {
            mainSkillDropdown.ClearOptions();
            var mainOptions = new List<string> { "-- 메인 스킬 선택 --" };
            foreach (var skill in mainSkillList)
            {
                mainOptions.Add($"[{skill.skill_id}] {skill.skill_name} ({skill.behavior_type})");
            }
            mainSkillDropdown.AddOptions(mainOptions);
            mainSkillDropdown.onValueChanged.AddListener(OnMainSkillChanged);
        }

        // 서포트 스킬 드롭다운 - 초기에는 "메인 스킬을 선택하세요"만 표시
        if (supportSkillDropdown != null)
        {
            supportSkillDropdown.ClearOptions();
            supportSkillDropdown.AddOptions(new List<string> { "-- 메인 스킬을 먼저 선택 --" });
            supportSkillDropdown.onValueChanged.AddListener(OnSupportSkillChanged);
        }
    }

    /// <summary>
    /// 메인 스킬의 behavior_type에 따라 호환되는 서포트 스킬만 드롭다운에 표시
    /// </summary>
    private void UpdateSupportSkillDropdown(string behaviorType)
    {
        if (supportSkillDropdown == null) return;

        supportSkillDropdown.ClearOptions();
        filteredSupportSkillList.Clear();

        var options = new List<string> { "-- 서포트 스킬 없음 --" };

        foreach (var support in supportSkillList)
        {
            // SkillCombinationRuleData를 사용하여 조합 가능 여부 확인
            bool isCompatible = false;

            if (combinationRuleData != null)
            {
                isCompatible = combinationRuleData.IsValidCombination(behaviorType, support.support_type);
            }
            else
            {
                // 조합 규칙 데이터가 없으면 모든 조합 허용
                isCompatible = true;
            }

            if (isCompatible)
            {
                filteredSupportSkillList.Add(support);
                options.Add($"[{support.support_id}] {support.support_name} ({support.support_type})");
            }
        }

        supportSkillDropdown.AddOptions(options);
        supportSkillDropdown.value = 0;
        selectedSupportSkillId = 0;

        Log($"호환 서포트 스킬: {filteredSupportSkillList.Count}개 ({behaviorType})");
    }

    private void InitializeButtons()
    {
        if (fireButton != null)
            fireButton.onClick.AddListener(OnFireButtonClicked);

        if (addMonsterButton != null)
            addMonsterButton.onClick.AddListener(OnAddMonsterClicked);

        if (removeMonsterButton != null)
            removeMonsterButton.onClick.AddListener(OnRemoveMonsterClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        // 포메이션 버튼
        if (linePatternButton != null)
            linePatternButton.onClick.AddListener(OnLinePatternClicked);

        if (circlePatternButton != null)
            circlePatternButton.onClick.AddListener(OnCirclePatternClicked);

        if (randomPatternButton != null)
            randomPatternButton.onClick.AddListener(OnRandomPatternClicked);
    }

    private void OnMainSkillChanged(int index)
    {
        if (index <= 0 || index > mainSkillList.Count)
        {
            selectedMainSkillId = 0;
            // 메인 스킬 미선택 시 서포트 드롭다운 초기화
            if (supportSkillDropdown != null)
            {
                supportSkillDropdown.ClearOptions();
                supportSkillDropdown.AddOptions(new List<string> { "-- 메인 스킬을 먼저 선택 --" });
                filteredSupportSkillList.Clear();
                selectedSupportSkillId = 0;
            }
            return;
        }

        selectedMainSkillId = mainSkillList[index - 1].skill_id;
        var skill = mainSkillList[index - 1];
        Log($"메인 스킬 선택: {skill.skill_name} (타입: {skill.behavior_type}, 데미지: {skill.base_damage})");

        // 메인 스킬의 behavior_type에 따라 서포트 스킬 드롭다운 업데이트
        UpdateSupportSkillDropdown(skill.behavior_type);
    }

    private void OnSupportSkillChanged(int index)
    {
        // 필터링된 목록 사용
        if (index <= 0 || index > filteredSupportSkillList.Count)
        {
            selectedSupportSkillId = 0;
            return;
        }

        selectedSupportSkillId = filteredSupportSkillList[index - 1].support_id;
        var skill = filteredSupportSkillList[index - 1];
        Log($"서포트 스킬 선택: {skill.support_name} (타입: {skill.support_type})");
    }

    private void OnFireButtonClicked()
    {
        if (selectedMainSkillId == 0)
        {
            Log("메인 스킬을 선택하세요!");
            return;
        }

        FireSkillAsync().Forget();
    }

    private async UniTaskVoid FireSkillAsync()
    {
        var mainSkill = CSVLoader.Instance.GetData<MainSkillData>(selectedMainSkillId);
        if (mainSkill == null)
        {
            Log($"메인 스킬 데이터를 찾을 수 없음: {selectedMainSkillId}");
            return;
        }

        SupportSkillData supportSkill = null;
        if (selectedSupportSkillId > 0)
        {
            supportSkill = CSVLoader.Instance.GetData<SupportSkillData>(selectedSupportSkillId);
        }

        // 타겟 찾기
        ITargetable target = null;
        if (spawnedMonsters.Count > 0)
        {
            var monsterObj = spawnedMonsters[0];
            if (monsterObj != null)
            {
                target = monsterObj.GetComponent<Monster>();
            }
        }

        if (target == null)
        {
            Log("타겟이 없습니다. 몬스터를 추가하세요!");
            return;
        }

        // 캐스터 위치
        Transform caster = testCharacter != null ? testCharacter.transform : characterSpawnPoint;

        Log($"스킬 발사: {mainSkill.skill_name}" + (supportSkill != null ? $" + {supportSkill.support_name}" : ""));

        // 스킬 실행
        await SkillExecutor.Instance.ExecuteSkillAsync(caster, target, mainSkill, supportSkill);
    }

    private void OnAddMonsterClicked()
    {
        SpawnMonster();
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Log("몬스터 프리팹이 없습니다. Inspector에서 설정하세요.");
            return;
        }

        Vector3 pos = monsterSpawnArea != null
            ? monsterSpawnArea.position + Random.insideUnitSphere * 3f
            : Vector3.zero + Vector3.forward * 5f;
        pos.y = 0;

        var newMonster = Instantiate(monsterPrefab, pos, Quaternion.identity);

        // Monster 초기화 (체력 설정)
        var monster = newMonster.GetComponent<Monster>();
        if (monster != null)
        {
            monster.OnSpawn(); // 체력 초기화
        }

        spawnedMonsters.Add(newMonster);
        UpdateMonsterCount();
        Log($"몬스터 스폰됨 (총 {spawnedMonsters.Count}마리)");
    }

    private void OnRemoveMonsterClicked()
    {
        if (spawnedMonsters.Count == 0)
        {
            Log("제거할 몬스터가 없습니다.");
            return;
        }

        var monster = spawnedMonsters[spawnedMonsters.Count - 1];
        spawnedMonsters.RemoveAt(spawnedMonsters.Count - 1);

        if (monster != null)
        {
            Destroy(monster);
        }

        UpdateMonsterCount();
        Log($"몬스터 제거됨 (남은 {spawnedMonsters.Count}마리)");
    }

    private void OnResetClicked()
    {
        // 모든 몬스터 제거
        foreach (var monster in spawnedMonsters)
        {
            if (monster != null)
            {
                Destroy(monster);
            }
        }
        spawnedMonsters.Clear();

        // 드롭다운 초기화
        if (mainSkillDropdown != null) mainSkillDropdown.value = 0;
        if (supportSkillDropdown != null) supportSkillDropdown.value = 0;

        selectedMainSkillId = 0;
        selectedSupportSkillId = 0;

        UpdateMonsterCount();
        Log("리셋 완료");
    }

    private void SpawnTestCharacter()
    {
        if (testCharacter != null) return;

        if (characterPrefab != null && characterSpawnPoint != null)
        {
            var charObj = Instantiate(characterPrefab, characterSpawnPoint.position, Quaternion.identity);
            testCharacter = charObj.GetComponent<Character>();

            // 자동 공격 비활성화 (스킬 테스트용)
            if (testCharacter != null)
            {
                testCharacter.SetAutoAttackEnabled(false);
            }

            Log("테스트 캐릭터 스폰됨 (자동 공격 비활성화)");
        }
    }

    private void UpdateMonsterCount()
    {
        // 죽은 몬스터 정리
        spawnedMonsters.RemoveAll(m => m == null);

        if (monsterCountText != null)
        {
            monsterCountText.text = $"몬스터: {spawnedMonsters.Count}";
        }
    }

    private void Log(string message)
    {
        Debug.Log($"[SkillTestManager] {message}");

        if (logText != null)
        {
            logText.text = message;
        }
    }

    #region Formation Spawning

    private void OnLinePatternClicked()
    {
        ClearAllMonsters();
        SpawnMonstersInLine(formationMonsterCount);
        Log($"일렬 배치로 {formationMonsterCount}마리 스폰 (간격: {formationSpacing}m)");
    }

    private void OnCirclePatternClicked()
    {
        ClearAllMonsters();
        SpawnMonstersInCircle(formationMonsterCount);
        Log($"원형 배치로 {formationMonsterCount}마리 스폰");
    }

    private void OnRandomPatternClicked()
    {
        ClearAllMonsters();
        SpawnMonstersRandom(formationMonsterCount);
        Log($"랜덤 배치로 {formationMonsterCount}마리 스폰 (최소 간격: {formationSpacing}m)");
    }

    private void SpawnMonstersInLine(int count)
    {
        if (monsterPrefab == null || monsterSpawnArea == null) return;

        Vector3 center = monsterSpawnArea.position;
        float totalWidth = (count - 1) * formationSpacing;
        float startX = center.x - totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * formationSpacing;
            Vector3 pos = new Vector3(x, 0f, center.z);
            SpawnMonsterAtPosition(pos);
        }

        UpdateMonsterCount();
    }

    private void SpawnMonstersInCircle(int count)
    {
        if (monsterPrefab == null || monsterSpawnArea == null) return;

        Vector3 center = monsterSpawnArea.position;
        float radius = Mathf.Max(2f, count * 0.5f);

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            Vector3 pos = new Vector3(x, 0f, z);
            SpawnMonsterAtPosition(pos);
        }

        UpdateMonsterCount();
    }

    private void SpawnMonstersRandom(int count)
    {
        if (monsterPrefab == null || monsterSpawnArea == null) return;

        Vector3 center = monsterSpawnArea.position;
        var positions = new List<Vector3>();
        int maxAttempts = 50;
        float spawnRadius = 5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 newPos = Vector3.zero;
            bool validPosition = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 랜덤 위치 생성
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                newPos = new Vector3(center.x + randomCircle.x, 0f, center.z + randomCircle.y);

                // 기존 위치들과 최소 간격 체크
                validPosition = true;
                foreach (var existingPos in positions)
                {
                    if (Vector3.Distance(newPos, existingPos) < formationSpacing)
                    {
                        validPosition = false;
                        break;
                    }
                }

                if (validPosition) break;
            }

            positions.Add(newPos);
            SpawnMonsterAtPosition(newPos);
        }

        UpdateMonsterCount();
    }

    private void SpawnMonsterAtPosition(Vector3 position)
    {
        if (monsterPrefab == null) return;

        var newMonster = Instantiate(monsterPrefab, position, Quaternion.identity);

        var monster = newMonster.GetComponent<Monster>();
        if (monster != null)
        {
            monster.OnSpawn();
        }

        spawnedMonsters.Add(newMonster);
    }

    private void ClearAllMonsters()
    {
        foreach (var monster in spawnedMonsters)
        {
            if (monster != null)
            {
                Destroy(monster);
            }
        }
        spawnedMonsters.Clear();
    }

    #endregion
}
