using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

public class PartyPanel : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject partySynergyInfoPrefab;

    [Header("Containers")]
    [SerializeField] private Transform contentParent;

    [Header("Toggle Buttons")]
    [SerializeField] private Button partySynergyButton;
    [SerializeField] private Button characterSelectionButton;

    [Header("Panels")]
    [SerializeField] private GameObject synergyListPanel;
    [SerializeField] private GameObject activeSynergyPanel;

    [Header("Active Synergy Display")]
    [SerializeField] private TextMeshProUGUI activeSynergyTitleText;
    [SerializeField] private TextMeshProUGUI activeSynergyNameText;
    [SerializeField] private TextMeshProUGUI activeSynergyEffectText;

    private List<PartySynergyInfoPanel> synergyPanels = new List<PartySynergyInfoPanel>();
    private List<PartySynergyData> allSynergies = new List<PartySynergyData>();
    private bool isInitialized = false;

    private async UniTaskVoid Start()
    {
        Debug.Log("[PartyPanel] Start() called - Waiting for CSVLoader...");

        // 버튼 리스너 등록
        if (partySynergyButton != null)
            partySynergyButton.onClick.AddListener(OnPartySynergyButtonClicked);
        if (characterSelectionButton != null)
            characterSelectionButton.onClick.AddListener(OnCharacterSelectionButtonClicked);

        // ActiveSynergyPanel이 없으면 동적 생성
        if (activeSynergyPanel == null)
        {
            CreateActiveSynergyPanel();
        }

        // CSV 로딩 완료될 때까지 대기
        await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);

        Debug.Log("[PartyPanel] CSVLoader ready - Getting PartySynergyTable...");

        await InitializeSynergyList();

        // 초기 상태: 시너지 리스트 패널 활성화
        ShowSynergyListPanel();
    }

    /// <summary>
    /// ActiveSynergyPanel UI 동적 생성
    /// </summary>
    private void CreateActiveSynergyPanel()
    {
        // 부모 Transform 결정 (synergyListPanel의 부모 또는 this.transform)
        Transform parentTransform = synergyListPanel != null ? synergyListPanel.transform.parent : transform;

        // ActiveSynergyPanel 생성
        GameObject panelObj = new GameObject("ActiveSynergyPanel");
        panelObj.transform.SetParent(parentTransform, false);
        activeSynergyPanel = panelObj;

        // RectTransform 설정
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // VerticalLayoutGroup 추가
        VerticalLayoutGroup layoutGroup = panelObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 20f;
        layoutGroup.padding = new RectOffset(20, 20, 40, 20);

        // Title Text 생성
        activeSynergyTitleText = CreateTextElement(panelObj.transform, "TitleText", "파티 시너지", 36, FontStyles.Bold);

        // Name Text 생성
        activeSynergyNameText = CreateTextElement(panelObj.transform, "NameText", "", 28, FontStyles.Normal);

        // Effect Text 생성
        activeSynergyEffectText = CreateTextElement(panelObj.transform, "EffectText", "", 24, FontStyles.Normal);

        // 초기에는 비활성화
        panelObj.SetActive(false);

        Debug.Log("[PartyPanel] ActiveSynergyPanel 동적 생성 완료");
    }

    /// <summary>
    /// TextMeshProUGUI 요소 생성 헬퍼
    /// </summary>
    private TextMeshProUGUI CreateTextElement(Transform parent, string name, string initialText, int fontSize, FontStyles fontStyle)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, fontSize + 20);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = initialText;
        tmpText.fontSize = fontSize;
        tmpText.fontStyle = fontStyle;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        return tmpText;
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

    private void OnDestroy()
    {
        // 버튼 리스너 해제
        if (partySynergyButton != null)
            partySynergyButton.onClick.RemoveListener(OnPartySynergyButtonClicked);
        if (characterSelectionButton != null)
            characterSelectionButton.onClick.RemoveListener(OnCharacterSelectionButtonClicked);
    }

    #region Toggle Buttons

    /// <summary>
    /// 파티 시너지 버튼 클릭 - 활성 시너지 표시
    /// </summary>
    public void OnPartySynergyButtonClicked()
    {
        Debug.Log("[PartyPanel] Party Synergy Button Clicked");
        ShowActiveSynergyPanel();
    }

    /// <summary>
    /// 캐릭터 셀렉션 버튼 클릭 - 시너지 리스트 표시
    /// </summary>
    public void OnCharacterSelectionButtonClicked()
    {
        Debug.Log("[PartyPanel] Character Selection Button Clicked");
        ShowSynergyListPanel();
    }

    private void ShowSynergyListPanel()
    {
        if (synergyListPanel != null)
            synergyListPanel.SetActive(true);
        if (activeSynergyPanel != null)
            activeSynergyPanel.SetActive(false);

        // 버튼 상태 업데이트
        if (partySynergyButton != null)
            partySynergyButton.interactable = true;
        if (characterSelectionButton != null)
            characterSelectionButton.interactable = false;
    }

    private void ShowActiveSynergyPanel()
    {
        if (synergyListPanel != null)
            synergyListPanel.SetActive(false);
        if (activeSynergyPanel != null)
            activeSynergyPanel.SetActive(true);

        // 버튼 상태 업데이트
        if (partySynergyButton != null)
            partySynergyButton.interactable = false;
        if (characterSelectionButton != null)
            characterSelectionButton.interactable = true;

        // 활성 시너지 새로고침
        RefreshActiveSynergies();
    }

    #endregion

    #region Active Synergy Display

    /// <summary>
    /// 현재 덱 기준 활성 시너지 새로고침
    /// </summary>
    private void RefreshActiveSynergies()
    {
        var activeSynergies = GetActiveSynergies();

        if (activeSynergies.Count == 0)
        {
            DisplayNoActiveSynergy();
        }
        else
        {
            // 첫 번째 활성 시너지 표시 (현재 데이터상 동시에 1개만 활성화 가능)
            DisplayActiveSynergy(activeSynergies[0]);
        }
    }

    /// <summary>
    /// 현재 덱 기준 활성화된 시너지 목록 반환
    /// </summary>
    private List<PartySynergyData> GetActiveSynergies()
    {
        var activeSynergies = new List<PartySynergyData>();

        if (DeckManager.Instance == null)
        {
            Debug.LogWarning("[PartyPanel] DeckManager.Instance is null");
            return activeSynergies;
        }

        var validCharacters = DeckManager.Instance.GetValidCharacters();
        if (validCharacters.Count < 4)
        {
            Debug.Log($"[PartyPanel] 덱에 캐릭터가 {validCharacters.Count}명뿐입니다. 시너지 활성화에는 4명 필요.");
            return activeSynergies;
        }

        var deckSet = new HashSet<int>(validCharacters);

        foreach (var synergy in allSynergies)
        {
            bool isActive = deckSet.Contains(synergy.Req_Char_1_ID)
                         && deckSet.Contains(synergy.Req_Char_2_ID)
                         && deckSet.Contains(synergy.Req_Char_3_ID)
                         && deckSet.Contains(synergy.Req_Char_4_ID);

            if (isActive)
            {
                activeSynergies.Add(synergy);
                Debug.Log($"[PartyPanel] 시너지 활성화됨! Party_ID: {synergy.Party_ID}");
            }
        }

        return activeSynergies;
    }

    /// <summary>
    /// 활성 시너지 없음 표시
    /// </summary>
    private void DisplayNoActiveSynergy()
    {
        if (activeSynergyTitleText != null)
            activeSynergyTitleText.text = "파티 시너지";
        if (activeSynergyNameText != null)
            activeSynergyNameText.text = "활성화된 시너지 없음";
        if (activeSynergyEffectText != null)
            activeSynergyEffectText.text = "4명의 캐릭터를 같은 파티로 편성하세요";

        Debug.Log("[PartyPanel] 활성화된 시너지 없음");
    }

    /// <summary>
    /// 활성 시너지 표시
    /// </summary>
    private void DisplayActiveSynergy(PartySynergyData synergy)
    {
        // 제목 (고정)
        if (activeSynergyTitleText != null)
            activeSynergyTitleText.text = "파티 시너지";

        // 시너지 이름
        if (activeSynergyNameText != null)
        {
            var nameString = CSVLoader.Instance.GetData<StringTable>(synergy.Party_Name_ID);
            activeSynergyNameText.text = nameString?.Text ?? $"시너지_{synergy.Party_ID}";
        }

        // 효과 설명 (Lv1 기준, 확장성 고려)
        if (activeSynergyEffectText != null)
        {
            var (effectId, effectValue) = GetEffectByLevel(synergy, 1);
            string effectDescription = GetEffectDescription(effectId, effectValue);
            activeSynergyEffectText.text = effectDescription;
        }

        Debug.Log($"[PartyPanel] 시너지 표시: {activeSynergyNameText?.text}");
    }

    /// <summary>
    /// 레벨에 따른 효과 ID와 값 반환 (확장성 고려)
    /// </summary>
    private (int effectId, float effectValue) GetEffectByLevel(PartySynergyData data, int level = 1)
    {
        return level switch
        {
            1 => (data.Effect_1_ID, data.Effect_1_Value),
            2 => (data.Effect_2_ID, data.Effect_2_Value),
            3 => (data.Effect_3_ID, data.Effect_3_Value),
            4 => (data.Effect_4_ID, data.Effect_4_Value),
            5 => (data.Effect_5_ID, data.Effect_5_Value),
            _ => (data.Effect_1_ID, data.Effect_1_Value)
        };
    }

    /// <summary>
    /// 효과 설명 텍스트 생성
    /// </summary>
    private string GetEffectDescription(int effectId, float effectValue)
    {
        var effectData = CSVLoader.Instance.GetData<PartySynergyEffectData>(effectId);
        if (effectData == null)
        {
            Debug.LogWarning($"[PartyPanel] PartySynergyEffectData not found for ID: {effectId}");
            return $"효과 {effectId}: {effectValue}";
        }

        var descriptionString = CSVLoader.Instance.GetData<StringTable>(effectData.Party_Effect_Description_ID);
        if (descriptionString == null)
        {
            Debug.LogWarning($"[PartyPanel] StringTable not found for ID: {effectData.Party_Effect_Description_ID}");
            return $"효과: {effectValue}";
        }

        // 효과값 포맷팅 (정수면 소수점 제거)
        string valueText = effectValue % 1 == 0 ? ((int)effectValue).ToString() : effectValue.ToString("F1");

        // 플레이스홀더 {0}이 있으면 대체, 없으면 그대로 반환
        string description = descriptionString.Text;
        if (description.Contains("{0}"))
        {
            return string.Format(description, valueText);
        }
        else
        {
            // 플레이스홀더 없으면 값을 뒤에 추가
            return $"{description} {valueText}";
        }
    }

    #endregion
}
