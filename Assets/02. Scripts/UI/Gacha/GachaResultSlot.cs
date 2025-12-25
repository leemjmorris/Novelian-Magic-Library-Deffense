using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// 가챠 결과 슬롯 UI
/// - 캐릭터 아이콘 표시
/// - 캐릭터 이름 표시
/// - 신규 캐릭터: "NEW!" 뱃지
/// - 중복 캐릭터: "정수 x1" 표시
/// </summary>
public class GachaResultSlot : MonoBehaviour
{
    [Header("Character Display")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Grade Frame (Optional)")]
    [SerializeField] private Image gradeFrame;
    [SerializeField] private Color horrorColor = new Color(0.6f, 0.2f, 0.6f);      // 공포 - 보라
    [SerializeField] private Color romanceColor = new Color(1f, 0.5f, 0.7f);       // 로맨스 - 핑크
    [SerializeField] private Color adventureColor = new Color(0.2f, 0.8f, 0.2f);   // 모험 - 녹색
    [SerializeField] private Color comedyColor = new Color(1f, 0.8f, 0.2f);        // 코미디 - 노랑
    [SerializeField] private Color mysteryColor = new Color(0.2f, 0.4f, 0.8f);     // 미스터리 - 파랑

    [Header("New Badge")]
    [SerializeField] private GameObject newBadge;
    [SerializeField] private TextMeshProUGUI newBadgeText;

    [Header("Duplicate Info")]
    [SerializeField] private GameObject duplicateInfo;
    [SerializeField] private TextMeshProUGUI essenceText;

    [Header("Animation")]
    [SerializeField] private Animator slotAnimator;
    [SerializeField] private string revealAnimationTrigger = "Reveal";

    private GachaResult currentResult;

    /// <summary>
    /// 슬롯 초기화 및 결과 표시
    /// </summary>
    public async UniTask Initialize(GachaResult result)
    {
        currentResult = result;

        // 1. 캐릭터 이름 설정
        if (characterNameText != null)
        {
            characterNameText.text = result.GetCharacterName();
        }

        // 2. 아이콘 로드 (신규: 캐릭터 / 중복: 정수)
        if (result.IsNew)
        {
            await LoadCharacterIcon(result.CharacterId);
        }
        else
        {
            await LoadEssenceIcon(result.EssenceId);
        }

        // 3. 등급 프레임 색상 설정
        SetGradeFrameColor(result.CharacterId);

        // 4. 신규/중복 표시
        if (result.IsNew)
        {
            // 신규 캐릭터
            if (newBadge != null)
                newBadge.SetActive(true);

            if (newBadgeText != null)
                newBadgeText.text = "NEW!";

            if (duplicateInfo != null)
                duplicateInfo.SetActive(false);
        }
        else
        {
            // 중복 캐릭터
            if (newBadge != null)
                newBadge.SetActive(false);

            if (duplicateInfo != null)
                duplicateInfo.SetActive(true);

            if (essenceText != null)
            {
                string essenceName = result.GetEssenceName();
                if (string.IsNullOrEmpty(essenceName))
                    essenceName = $"{result.GetCharacterName()}의 정수";
                essenceText.text = $"{essenceName} x1";
            }
        }

        // 5. 등장 애니메이션 재생
        PlayRevealAnimation();
    }

    /// <summary>
    /// 캐릭터 아이콘 로드 (Addressables)
    /// </summary>
    private async UniTask LoadCharacterIcon(int characterId)
    {
        if (characterIcon == null) return;

        // 기본 아이콘 키
        string iconKey = AddressableKey.Icon_Character;

        // CharacterData에서 Path_ID로 개별 아이콘 키 조회
        if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
        {
            var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
            if (characterData != null && characterData.Path_ID > 0)
            {
                var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
                if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
                {
                    iconKey = pathData.Addressable_Key;
                }
            }
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(iconKey);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                characterIcon.sprite = handle.Result;
                characterIcon.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[GachaResultSlot] 아이콘 로드 실패: {iconKey}");
                characterIcon.color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GachaResultSlot] 아이콘 로드 예외: {e.Message}");
            characterIcon.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    /// <summary>
    /// 정수 아이콘 로드 (Addressables) - 중복 캐릭터용
    /// </summary>
    private async UniTask LoadEssenceIcon(int essenceId)
    {
        if (characterIcon == null || essenceId == 0) return;

        // AddressableKey 유틸리티로 아이콘 키 조회
        string iconKey = AddressableKey.GetItemIconKey(essenceId);

        try
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(iconKey);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                characterIcon.sprite = handle.Result;
                characterIcon.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[GachaResultSlot] 정수 아이콘 로드 실패: {iconKey}");
                characterIcon.color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GachaResultSlot] 정수 아이콘 로드 예외: {e.Message}");
            characterIcon.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    /// <summary>
    /// 장르에 따른 프레임 색상 설정
    /// </summary>
    private void SetGradeFrameColor(int characterId)
    {
        if (gradeFrame == null) return;

        Genre genre = GetCharacterGenre(characterId);

        gradeFrame.color = genre switch
        {
            Genre.Horror => horrorColor,
            Genre.Romance => romanceColor,
            Genre.Adventure => adventureColor,
            Genre.Comedy => comedyColor,
            Genre.Mystery => mysteryColor,
            _ => Color.white
        };
    }

    /// <summary>
    /// 캐릭터 장르 조회
    /// </summary>
    private Genre GetCharacterGenre(int characterId)
    {
        if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            // ID에서 장르 추측 (021xxx=Horror, 022xxx=Romance, ...)
            int genreDigit = (characterId / 1000) % 10;
            return (Genre)genreDigit;
        }

        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        return characterData?.Genre ?? Genre.Horror;
    }

    /// <summary>
    /// 등장 애니메이션 재생
    /// </summary>
    private void PlayRevealAnimation()
    {
        if (slotAnimator != null && !string.IsNullOrEmpty(revealAnimationTrigger))
        {
            slotAnimator.SetTrigger(revealAnimationTrigger);
        }
    }

    /// <summary>
    /// 슬롯 초기화 (빈 상태로)
    /// </summary>
    public void Clear()
    {
        currentResult = null;

        if (characterIcon != null)
        {
            characterIcon.sprite = null;
            characterIcon.color = new Color(1f, 1f, 1f, 0.3f);
        }

        if (characterNameText != null)
            characterNameText.text = "";

        if (newBadge != null)
            newBadge.SetActive(false);

        if (duplicateInfo != null)
            duplicateInfo.SetActive(false);
    }

    /// <summary>
    /// 현재 결과 반환
    /// </summary>
    public GachaResult GetCurrentResult()
    {
        return currentResult;
    }
}
