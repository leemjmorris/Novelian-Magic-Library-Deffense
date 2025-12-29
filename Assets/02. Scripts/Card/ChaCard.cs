using Cysharp.Threading.Tasks;
using Novelian.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading;
using AssetKits.ParticleImage;
using AssetKits.ParticleImage.Enumerations;

/// <summary>
/// JML: CharacterCardGrid의 각 슬롯에 표시되는 캐릭터 카드 (Issue #424)
/// 소환된 캐릭터 정보 표시 (아이콘, 이름, 스탯)
/// 스탯: 장착 스킬, 공격력, 공격속도, 치명타 확률, 치명타 배율
/// Issue #437: 서포트 스킬 선택 시 클릭 가능 + 애니메이션
/// </summary>
public class ChaCard : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterInfoText;
    [SerializeField] private Image backgroundImage;

    [Header("Star Icons (성급 표시)")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    [Header("3-Star Effect (Issue #559)")]
    [Tooltip("3성 달성 시 재생되는 파티클 효과")]
    [SerializeField] private ParticleImage threeStarEffect;

    [Header("Empty State")]
    [SerializeField] private Color emptyBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color activeBackgroundColor = Color.white;

    [Header("Animation Settings (Issue #437)")]
    [SerializeField] private Color selectableHighlightColor = new Color(0.5f, 1f, 0.5f, 1f);
    [SerializeField] private Color dimColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    [SerializeField] private float selectableYOffset = 20f;
    [SerializeField] private float animationDuration = 0.3f;

    private int characterId = -1;
    private int starTier = 1;
    private bool isEmpty = true;
    private Character linkedCharacter; // 연결된 캐릭터 인스턴스

    // Animation state (Issue #437)
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private bool hasOriginalPosition = false;  // 원본 위치가 저장되었는지 여부
    private bool isSelectable = false;
    private bool isAnimating = false;
    private CharacterCardGridManager gridManager;
    private CancellationTokenSource animationCts;

    // Popup mode (상세 팝업으로 표시 중인지)
    private bool isPopupMode = false;
    private System.Action onPopupClicked; // 팝업 클릭 시 콜백

    public int CharacterId => characterId;
    public int StarTier => starTier;
    public bool IsEmpty => isEmpty;
    public bool IsSelectable => isSelectable;

    private void Awake()
    {
        // GridManager 찾기
        gridManager = GetComponentInParent<CharacterCardGridManager>();
        rectTransform = GetComponent<RectTransform>();

        // Issue #559: 3성 파티클 초기화 (TimeScale=0에서도 재생되도록)
        InitializeThreeStarEffect();

        // Issue #476: 이미 Initialize()가 호출된 경우 SetEmpty() 스킵
        // (스크립트 실행 순서 문제로 BossDungeonManager.Awake()가 먼저 실행되어
        //  Initialize()가 Awake()보다 먼저 호출될 수 있음)
        if (isEmpty)
        {
            SetEmpty();
        }
        else
        {
            Debug.Log($"[ChaCard] Awake: 이미 초기화됨, SetEmpty 스킵 (charId={characterId})");
        }
    }

    private void Start()
    {
        // 레이아웃 완료 후 원본 위치 저장 (다음 프레임에서)
        SaveOriginalPositionDelayed().Forget();
    }

    private void OnDestroy()
    {
        // 애니메이션 취소
        animationCts?.Cancel();
        animationCts?.Dispose();
    }

    /// <summary>
    /// 레이아웃 시스템이 완료된 후 위치 저장 (EndOfFrame 대기)
    /// </summary>
    private async UniTaskVoid SaveOriginalPositionDelayed()
    {
        // 레이아웃 시스템이 완료될 때까지 대기 (EndOfFrame)
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        SaveOriginalPosition();
    }

    /// <summary>
    /// 현재 위치를 원본 위치로 저장 (애니메이션 기준점)
    /// </summary>
    private void SaveOriginalPosition()
    {
        if (!hasOriginalPosition && rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
            hasOriginalPosition = true;
            Debug.Log($"[ChaCard] Original position saved: {originalAnchoredPosition}");
        }
    }

    /// <summary>
    /// JML: 캐릭터 정보로 카드 초기화
    /// CharacterCardGridManager에서 캐릭터 소환 시 호출
    /// </summary>
    /// <param name="charId">캐릭터 ID (CharacterTable)</param>
    /// <param name="tier">성급 (1~3)</param>
    /// <param name="character">연결할 Character 인스턴스 (스탯 표시용)</param>
    public async UniTask Initialize(int charId, int tier = 1, Character character = null)
    {
        Debug.Log($"[ChaCard] Initialize 시작: charId={charId}, gameObject={gameObject.name}, " +
                  $"characterNameText={characterNameText != null}, iconImage={iconImage != null}");

        characterId = charId;
        starTier = Mathf.Clamp(tier, 1, 3);
        isEmpty = false;
        linkedCharacter = character;
        Debug.Log($"[ChaCard] isEmpty=false 설정됨: gameObject={gameObject.name}, charId={charId}");

        // 1. CSV에서 캐릭터 데이터 로드
        bool csvReady = CSVLoader.Instance != null && CSVLoader.Instance.IsInit;
        Debug.Log($"[ChaCard] CSV 상태: Instance={CSVLoader.Instance != null}, IsInit={CSVLoader.Instance?.IsInit}");

        if (csvReady)
        {
            var characterData = CSVLoader.Instance.GetData<CharacterData>(charId);
            Debug.Log($"[ChaCard] CharacterData: {(characterData != null ? $"Name_ID={characterData.Character_Name_ID}" : "NULL")}");

            if (characterData != null)
            {
                // 캐릭터 이름 설정
                var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
                if (characterNameText != null)
                {
                    characterNameText.text = stringData?.Text ?? $"Character_{charId}";
                    Debug.Log($"[ChaCard] 이름 설정됨: {characterNameText.text}");
                }
                else
                {
                    Debug.LogError($"[ChaCard] characterNameText가 NULL입니다! charId={charId}");
                }

                // 아이콘 로드
                if (iconImage != null)
                {
                    string addressableKey = AddressableKey.Icon_Character;

                    // Path_ID가 있으면 PathTable에서 조회
                    if (characterData.Path_ID > 0)
                    {
                        var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
                        if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
                        {
                            addressableKey = pathData.Addressable_Key;
                        }
                    }

                    Debug.Log($"[ChaCard] 아이콘 로드 시작: {addressableKey}");

                    try
                    {
                        var sprite = await Addressables.LoadAssetAsync<Sprite>(addressableKey).ToUniTask();
                        iconImage.sprite = sprite;
                        iconImage.color = Color.white;
                        Debug.Log($"[ChaCard] 아이콘 로드 완료: {addressableKey}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[ChaCard] Failed to load icon for character {charId}: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[ChaCard] iconImage가 NULL입니다! charId={charId}");
                }
            }
            else
            {
                Debug.LogWarning($"[ChaCard] CharacterData not found for ID: {charId}");
                SetFallbackData(charId);
            }
        }
        else
        {
            Debug.LogWarning($"[ChaCard] CSV 로드 안됨! Fallback 사용. charId={charId}");
            SetFallbackData(charId);
        }

        // 2. 성급 표시 업데이트
        UpdateStarDisplay();

        // 3. 스탯 정보 업데이트
        UpdateStatDisplay();

        // 4. 배경 색상 활성화
        if (backgroundImage != null)
        {
            backgroundImage.color = activeBackgroundColor;
        }

        Debug.Log($"[ChaCard] Initialized: Character {charId}, {starTier}성");
    }

    /// <summary>
    /// JML: 성급 업데이트 (중복 카드 선택 시)
    /// </summary>
    public void UpdateStarTier(int newTier)
    {
        starTier = Mathf.Clamp(newTier, 1, 3);

        UpdateStarDisplay();
        Debug.Log($"[ChaCard] Star tier updated: Character {characterId} → {starTier}성");
    }

    /// <summary>
    /// JML: 빈 슬롯 상태로 설정
    /// </summary>
    public void SetEmpty()
    {
        // Issue #476: 디버그 - SetEmpty 호출 추적
        Debug.Log($"[ChaCard] SetEmpty 호출됨: gameObject={gameObject.name}, instanceId={GetInstanceID()}, 이전 charId={characterId}", this);
        characterId = -1;
        starTier = 1;
        isEmpty = true;

        // 텍스트 초기화
        if (characterNameText != null)
        {
            characterNameText.text = "";
        }

        if (characterInfoText != null)
        {
            characterInfoText.text = "";
        }

        // 아이콘 숨기기/투명하게
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = new Color(1f, 1f, 1f, 0.2f);
        }

        // 성급 아이콘 숨기기
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);

        // Issue #559: 3성 파티클 효과 정지
        if (threeStarEffect != null)
        {
            threeStarEffect.Stop(true);
        }

        // 배경 딤 처리
        if (backgroundImage != null)
        {
            backgroundImage.color = emptyBackgroundColor;
        }
    }

    /// <summary>
    /// JML: CSV 로드 실패 시 기본 데이터 설정
    /// </summary>
    private void SetFallbackData(int charId)
    {
        if (characterNameText != null)
        {
            characterNameText.text = $"Character_{charId}";
        }
    }

    /// <summary>
    /// JML: 3성 파티클 효과 초기화 (Issue #559)
    /// TimeScale=0에서도 재생되도록 설정
    /// </summary>
    private void InitializeThreeStarEffect()
    {
        if (threeStarEffect == null) return;

        threeStarEffect.timeScale = TimeScale.Unscaled;
        threeStarEffect.Stop(true);
    }

    /// <summary>
    /// JML: 성급 아이콘 표시 업데이트
    /// </summary>
    private void UpdateStarDisplay()
    {
        if (star1 != null) star1.SetActive(starTier >= 1);
        if (star2 != null) star2.SetActive(starTier >= 2);
        if (star3 != null) star3.SetActive(starTier >= 3);

        // Issue #559: 3성 달성 시 파티클 효과 재생
        UpdateThreeStarEffect();
    }

    /// <summary>
    /// JML: 3성 파티클 효과 재생/정지 (Issue #559)
    /// </summary>
    private void UpdateThreeStarEffect()
    {
        if (threeStarEffect == null) return;

        if (starTier >= 3)
        {
            // 이미 재생 중이 아닐 때만 재생
            if (!threeStarEffect.isPlaying)
            {
                threeStarEffect.Stop(true);
                threeStarEffect.Clear();
                threeStarEffect.Play();
                Debug.Log($"[ChaCard] 3-Star effect started for character {characterId}");
            }
        }
        else
        {
            // 3성 미만이면 정지
            if (threeStarEffect.isPlaying)
            {
                threeStarEffect.Stop(true);
                Debug.Log($"[ChaCard] 3-Star effect stopped for character {characterId}");
            }
        }
    }

    /// <summary>
    /// JML: 스탯 정보 표시 업데이트 (Issue #424)
    /// characterInfoText에 스탯 정보 표시
    /// 형식: 장착 스킬: {스킬명}\n공격력: {값}\n공격속도: {값}\n치명타 확률: {값}%\n치명타 배율: {값}%
    /// </summary>
    private void UpdateStatDisplay()
    {
        if (characterInfoText == null) return;

        if (linkedCharacter != null)
        {
            // 캐릭터 인스턴스에서 실제 스탯 가져오기
            string skillName = linkedCharacter.GetDisplaySkillName();
            float damage = linkedCharacter.GetDisplayDamage();
            float attackSpeed = linkedCharacter.GetDisplayAttackSpeed();
            float critChance = linkedCharacter.GetDisplayCritChance();
            float critMultiplier = linkedCharacter.GetDisplayCritMultiplier();

            characterInfoText.text = $"장착 스킬: {skillName}\n" +
                                     $"공격력: {damage:F1}\n" +
                                     $"공격속도: {attackSpeed:F2}\n" +
                                     $"치명타 확률: {critChance:F1}%\n" +
                                     $"치명타 배율: {critMultiplier:F1}%";
        }
        else
        {
            // 캐릭터 인스턴스가 없으면 기본값 표시
            characterInfoText.text = "장착 스킬: 없음\n" +
                                     "공격력: -\n" +
                                     "공격속도: -\n" +
                                     "치명타 확률: -\n" +
                                     "치명타 배율: -";
        }
    }

    /// <summary>
    /// JML: 캐릭터 인스턴스 연결 및 스탯 갱신 (Issue #424)
    /// 캐릭터 소환 완료 후 호출
    /// </summary>
    public void LinkCharacter(Character character)
    {
        linkedCharacter = character;
        UpdateStatDisplay();
    }

    /// <summary>
    /// JML: 스탯 갱신 (외부에서 호출 가능)
    /// 캐릭터 스탯이 변경될 때 호출
    /// </summary>
    public void RefreshStats()
    {
        UpdateStatDisplay();
    }

    #region Animation Methods (Issue #437)

    /// <summary>
    /// JML: 선택 가능 애니메이션 (서포트 스킬 호환 시)
    /// Y축 위로 이동 + 하이라이트 색상
    /// </summary>
    public void PlaySelectableAnimation()
    {
        if (isEmpty || isAnimating) return;

        // 애니메이션 시작 전 현재 위치를 원본으로 저장 (아직 저장 안됐으면)
        if (!hasOriginalPosition && rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
            hasOriginalPosition = true;
            Debug.Log($"[ChaCard] Original position saved on animation start: {originalAnchoredPosition}");
        }

        isSelectable = true;

        // 기존 애니메이션 취소
        animationCts?.Cancel();
        animationCts = new CancellationTokenSource();

        AnimateToSelectableState(animationCts.Token).Forget();
    }

    /// <summary>
    /// JML: 딤 애니메이션 (서포트 스킬 비호환 시)
    /// 어두운 색상으로 변경
    /// </summary>
    public void PlayDimAnimation()
    {
        if (isEmpty || isAnimating) return;

        isSelectable = false;

        // 기존 애니메이션 취소
        animationCts?.Cancel();
        animationCts = new CancellationTokenSource();

        AnimateToDimState(animationCts.Token).Forget();
    }

    /// <summary>
    /// JML: 애니메이션 초기화 (원래 상태로 복원)
    /// </summary>
    public void ResetAnimation()
    {
        if (isEmpty) return;

        isSelectable = false;

        // 기존 애니메이션 취소
        animationCts?.Cancel();
        animationCts = new CancellationTokenSource();

        AnimateToNormalState(animationCts.Token).Forget();
    }

    private async UniTaskVoid AnimateToSelectableState(CancellationToken ct)
    {
        if (rectTransform == null) return;

        isAnimating = true;

        Vector2 targetPos = originalAnchoredPosition + new Vector2(0, selectableYOffset);
        float elapsed = 0f;

        Vector2 startPos = rectTransform.anchoredPosition;
        Color startColor = backgroundImage != null ? backgroundImage.color : activeBackgroundColor;

        try
        {
            while (elapsed < animationDuration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / animationDuration);

                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(startColor, selectableHighlightColor, t);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            rectTransform.anchoredPosition = targetPos;
            if (backgroundImage != null)
            {
                backgroundImage.color = selectableHighlightColor;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 애니메이션 취소됨 - 정상적인 상황
        }
        finally
        {
            isAnimating = false;
        }
    }

    private async UniTaskVoid AnimateToDimState(CancellationToken ct)
    {
        isAnimating = true;

        float elapsed = 0f;
        Color startColor = backgroundImage != null ? backgroundImage.color : activeBackgroundColor;

        try
        {
            while (elapsed < animationDuration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / animationDuration);

                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(startColor, dimColor, t);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = dimColor;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 애니메이션 취소됨
        }
        finally
        {
            isAnimating = false;
        }
    }

    private async UniTaskVoid AnimateToNormalState(CancellationToken ct)
    {
        if (rectTransform == null) return;

        isAnimating = true;

        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Color startColor = backgroundImage != null ? backgroundImage.color : activeBackgroundColor;

        try
        {
            while (elapsed < animationDuration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / animationDuration);

                rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalAnchoredPosition, t);

                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(startColor, activeBackgroundColor, t);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            rectTransform.anchoredPosition = originalAnchoredPosition;
            if (backgroundImage != null)
            {
                backgroundImage.color = activeBackgroundColor;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 애니메이션 취소됨
        }
        finally
        {
            isAnimating = false;
        }
    }

    #endregion

    #region Click Handler (Issue #437)

    /// <summary>
    /// JML: 카드 클릭 시 처리 (IPointerClickHandler)
    /// - 팝업 모드: 클릭 시 팝업 닫기
    /// - 선택 가능 상태: GridManager에 스킬 장착 요청
    /// - 일반 상태: 상세 팝업 열기
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 팝업 모드인 경우 - 클릭하면 팝업 닫기
        if (isPopupMode)
        {
            Debug.Log($"[ChaCard] Popup clicked, closing...");
            onPopupClicked?.Invoke();
            return;
        }

        // 빈 슬롯은 무시
        if (isEmpty)
        {
            Debug.Log($"[ChaCard] Click ignored: slot is empty");
            return;
        }

        // GridManager 찾기
        if (gridManager == null)
        {
            gridManager = GetComponentInParent<CharacterCardGridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogWarning("[ChaCard] GridManager not found!");
            return;
        }

        // 2. 선택 가능 상태 (서포트 스킬 장착 모드) - 스킬 장착 처리
        if (isSelectable)
        {
            Debug.Log($"[ChaCard] Clicked for skill equip! CharacterId={characterId}");
            gridManager.OnCharacterCardClicked(characterId);
            return;
        }

        // 3. 일반 상태 - 상세 팝업 열기
        Debug.Log($"[ChaCard] Clicked for detail popup! CharacterId={characterId}");
        gridManager.ShowCardDetailPopup(this);
    }

    #endregion

    #region Popup Mode

    /// <summary>
    /// JML: 팝업 모드 설정 (복제된 카드용)
    /// </summary>
    /// <param name="isPopup">팝업 모드 여부</param>
    /// <param name="onClicked">팝업 클릭 시 콜백</param>
    public void SetPopupMode(bool isPopup, System.Action onClicked = null)
    {
        isPopupMode = isPopup;
        onPopupClicked = onClicked;

        // 팝업 모드에서는 GridManager 필요 없음
        if (isPopup)
        {
            gridManager = null;
        }

        Debug.Log($"[ChaCard] Popup mode set: {isPopup}");
    }

    /// <summary>
    /// JML: 원본 카드의 데이터를 복사 (팝업용)
    /// </summary>
    public void CopyDataFrom(ChaCard source)
    {
        if (source == null) return;

        characterId = source.characterId;
        starTier = source.starTier;
        isEmpty = source.isEmpty;
        linkedCharacter = source.linkedCharacter;

        // UI 업데이트
        if (characterNameText != null && source.characterNameText != null)
        {
            characterNameText.text = source.characterNameText.text;
        }

        if (iconImage != null && source.iconImage != null)
        {
            iconImage.sprite = source.iconImage.sprite;
            iconImage.color = source.iconImage.color;
        }

        // 성급 표시
        UpdateStarDisplay();

        // 스탯 정보
        UpdateStatDisplay();

        // 배경색
        if (backgroundImage != null)
        {
            backgroundImage.color = activeBackgroundColor;
        }

        Debug.Log($"[ChaCard] Data copied from source: CharacterId={characterId}");
    }

    #endregion
}
