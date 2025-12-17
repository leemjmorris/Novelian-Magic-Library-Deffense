using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 파티 시너지 리스트를 표시하는 패널
/// 토글 버튼 및 활성 시너지 표시는 TeamSetupPanel에서 처리
/// </summary>
public class PartyPanel : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject partySynergyInfoPrefab;

    [Header("Containers")]
    [SerializeField] private Transform contentParent;

    [Header("Enhancement Panel")]
    [SerializeField] private PartySynergyEnhancementPanel enhancementPanel;

    private List<PartySynergyInfoPanel> synergyPanels = new List<PartySynergyInfoPanel>();
    private List<PartySynergyData> allSynergies = new List<PartySynergyData>();
    private bool isInitialized = false;

    private async UniTaskVoid Start()
    {
        Debug.Log("[PartyPanel] Start() called - Waiting for CSVLoader...");

        // CSV 로딩 완료될 때까지 대기
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);

        Debug.Log("[PartyPanel] CSVLoader ready - Getting PartySynergyTable...");

        await InitializeSynergyList();
    }

    private async UniTask InitializeSynergyList()
    {
        // 기존 패널들 제거
        ClearSynergyPanels();

        var synergyTable = CSVLoader.Instance.GetTable<PartySynergyData>();

        if (synergyTable == null)
        {
            Debug.LogError("[PartyPanel] PartySynergyTable is null!");
            return;
        }

        Debug.Log($"[PartyPanel] PartySynergyTable loaded with {synergyTable.Count} entries");

        // 유효한 시너지만 필터링 (Party_Name_ID != 0)
        allSynergies = synergyTable.GetAll()
            .Where(s => s.Party_Name_ID != 0)
            .OrderBy(s => s.Party_ID)
            .ToList();

        Debug.Log($"[PartyPanel] Valid synergies count: {allSynergies.Count}");

        foreach (var synergyData in allSynergies)
        {
            var panelObj = Instantiate(partySynergyInfoPrefab, contentParent);
            var panel = panelObj.GetComponent<PartySynergyInfoPanel>();

            if (panel != null)
            {
                panel.Init(synergyData);
                panel.SetEnhancementPanel(enhancementPanel);
                synergyPanels.Add(panel);
            }
            else
            {
                Debug.LogError("[PartyPanel] PartySynergyInfoPanel component not found on prefab!");
            }
        }

        isInitialized = true;
        Debug.Log($"[PartyPanel] Created {synergyPanels.Count} synergy panels");
    }

    private void OnEnable()
    {
        if (!isInitialized) return;

        // 패널 활성화 시 정보 갱신
        RefreshAllPanels();
    }

    public void RefreshAllPanels()
    {
        Debug.Log("[PartyPanel] Refreshing all panels...");
        // 추후 캐릭터 레벨 등 갱신 필요시 구현
    }

    private void ClearSynergyPanels()
    {
        foreach (var panel in synergyPanels)
        {
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }
        synergyPanels.Clear();
    }

    private void OnDisable()
    {
        Debug.Log("[PartyPanel] OnDisable called");
    }
}
