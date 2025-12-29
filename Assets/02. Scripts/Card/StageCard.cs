using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class StageCard : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private int cardId;

    public int CardId => cardId;

    public async UniTask Initialize(int cardId)
    {
        this.cardId = cardId;

        var cardData = CSVLoader.Instance.GetData<CardData>(cardId);
        if (cardData == null)
        {
            Debug.LogError($"[StageCard] CardData not found: {cardId}");
            return;
        }

        // Issue #564: 아이콘 먼저 투명하게 (로딩 전 기본 아이콘이 보이지 않도록)
        if (iconImage != null)
        {
            var color = iconImage.color;
            color.a = 0f;
            iconImage.color = color;
        }

        // 카드 이름
        var nameData = CSVLoader.Instance.GetData<StringTable>(cardData.Card_Name_ID);
        if (cardNameText != null)
        {
            cardNameText.text = nameData?.Text ?? $"Card_{cardId}";
        }

        // 카드 설명
        var descData = CSVLoader.Instance.GetData<StringTable>(cardData.Card_Description_ID);
        if (descriptionText != null)
        {
            descriptionText.text = descData?.Text ?? "";
        }

        // 아이콘 로드 (TimeScale=0에서도 동작하도록 EarlyUpdate 사용)
        if (iconImage != null && cardData.Path_ID > 0)
        {
            var pathData = CSVLoader.Instance.GetData<PathData>(cardData.Path_ID);
            if (pathData != null)
            {
                var sprite = await Addressables.LoadAssetAsync<Sprite>(pathData.Addressable_Key)
                    .ToUniTask(timing: PlayerLoopTiming.EarlyUpdate);
                iconImage.sprite = sprite;

                // Issue #564: 아이콘 로딩 완료 후 불투명하게
                var color = iconImage.color;
                color.a = 1f;
                iconImage.color = color;
            }
        }
    }
}
