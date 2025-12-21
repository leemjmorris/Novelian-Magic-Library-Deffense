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

    [Header("Spawn Settings")]
    [SerializeField] private Transform monsterSpawnArea;
    [SerializeField] private Transform characterSpawnPoint;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private GameObject characterPrefab;

    [Header("Test Character")]
    [SerializeField] private Character testCharacter;

    // 데이터
    private List<MainSkillData> mainSkillList = new List<MainSkillData>();
    private List<SupportSkillData> supportSkillList = new List<SupportSkillData>();
    private List<GameObject> spawnedMonsters = new List<GameObject>();

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

        // 서포트 스킬 드롭다운
        if (supportSkillDropdown != null)
        {
            supportSkillDropdown.ClearOptions();
            var supportOptions = new List<string> { "-- 서포트 스킬 없음 --" };
            foreach (var skill in supportSkillList)
            {
                supportOptions.Add($"[{skill.support_id}] {skill.support_name} ({skill.support_type})");
            }
            supportSkillDropdown.AddOptions(supportOptions);
            supportSkillDropdown.onValueChanged.AddListener(OnSupportSkillChanged);
        }
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
    }

    private void OnMainSkillChanged(int index)
    {
        if (index <= 0 || index > mainSkillList.Count)
        {
            selectedMainSkillId = 0;
            return;
        }

        selectedMainSkillId = mainSkillList[index - 1].skill_id;
        var skill = mainSkillList[index - 1];
        Log($"메인 스킬 선택: {skill.skill_name} (타입: {skill.behavior_type}, 데미지: {skill.base_damage})");
    }

    private void OnSupportSkillChanged(int index)
    {
        if (index <= 0 || index > supportSkillList.Count)
        {
            selectedSupportSkillId = 0;
            return;
        }

        selectedSupportSkillId = supportSkillList[index - 1].support_id;
        var skill = supportSkillList[index - 1];
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
}
