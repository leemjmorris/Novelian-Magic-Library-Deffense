using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using TMPro;

namespace Tutorial
{
    /// <summary>
    /// 기본 구조 대화 UI (하단 고정, 캐릭터 일러스트 + 이름 + 대사)
    /// </summary>
    public class FullDialogView : MonoBehaviour, ITutorialView, IPointerClickHandler
    {
        [Header("Events")]
        [SerializeField] private TutorialEvents tutorialEvents;

        [Header("UI References")]
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Image dialogArrow;

        [Header("Character Slots")]
        [SerializeField] private List<CharacterSlot> characterSlots;

        [Header("Settings")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.33f);

        private CancellationTokenSource typingCts;
        private bool isTyping = false;
        private string fullText = "";

        /// <summary>
        /// 외부에서 TutorialEvents 인스턴스를 주입받는 메서드
        /// </summary>
        public void SetTutorialEvents(TutorialEvents events)
        {
            tutorialEvents = events;
            Debug.Log($"[FullDialogView] TutorialEvents injected: {events != null}");
        }

        [System.Serializable]
        public class CharacterSlot
        {
            public Image illustrationImage;
            public int position; // 0: 왼쪽, 1: 중앙, 2: 오른쪽
        }

        public void Show(TutorialStep step, string text, float typingSpeed)
        {
            // 먼저 GameObject 활성화
            gameObject.SetActive(true);

            if (viewRoot != null)
                viewRoot.SetActive(true);

            // 캐릭터 설정
            SetupCharacters(step);

            // 텍스트 설정
            fullText = text;

            // 이전 타이핑 취소
            typingCts?.Cancel();
            typingCts?.Dispose();
            typingCts = new CancellationTokenSource();

            TypeTextAsync(text, typingSpeed, typingCts.Token).Forget();

            // 화살표 숨기기
            if (dialogArrow != null)
                dialogArrow.gameObject.SetActive(false);
        }

        public void Hide()
        {
            typingCts?.Cancel();
            typingCts?.Dispose();
            typingCts = null;

            if (viewRoot != null)
                viewRoot.SetActive(false);

            gameObject.SetActive(false);
            isTyping = false;
        }

        private void OnDestroy()
        {
            typingCts?.Cancel();
            typingCts?.Dispose();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[FullDialogView] OnPointerClick called! isTyping={isTyping}, tutorialEvents={tutorialEvents != null}");

            if (isTyping)
            {
                // 타이핑 중이면 즉시 완료
                CompleteTyping();
            }
            else
            {
                // 타이핑 완료 상태면 다음으로 진행
                tutorialEvents?.RaiseDialogTouched();
            }
        }

        private void SetupCharacters(TutorialStep step)
        {
            // 모든 슬롯 초기화
            foreach (var slot in characterSlots)
            {
                if (slot.illustrationImage != null)
                {
                    slot.illustrationImage.gameObject.SetActive(false);
                }
            }

            // 캐릭터 표시
            for (int i = 0; i < step.Characters.Count; i++)
            {
                var charInfo = step.Characters[i];
                var slot = characterSlots.Find(s => s.position == charInfo.Position);

                if (slot != null && slot.illustrationImage != null)
                {
                    slot.illustrationImage.gameObject.SetActive(charInfo.DisplayState != CharacterDisplayState.Hidden);

                    // 채도 설정
                    if (charInfo.DisplayState == CharacterDisplayState.Active || i == step.SpeakerIndex)
                    {
                        slot.illustrationImage.color = activeColor;
                    }
                    else
                    {
                        slot.illustrationImage.color = inactiveColor;
                    }

                    // Addressables로 일러스트 로드
                    if (!string.IsNullOrEmpty(charInfo.IllustrationKey))
                    {
                        LoadIllustrationAsync(slot.illustrationImage, charInfo.IllustrationKey).Forget();
                    }
                }

                // 화자 이름 설정
                if (i == step.SpeakerIndex && characterNameText != null)
                {
                    characterNameText.text = charInfo.CharacterName;
                }
            }
        }

        private async UniTaskVoid TypeTextAsync(string text, float speed, CancellationToken token)
        {
            isTyping = true;
            dialogText.text = "";

            try
            {
                foreach (char c in text)
                {
                    token.ThrowIfCancellationRequested();
                    dialogText.text += c;
                    await UniTask.Delay(TimeSpan.FromSeconds(speed), ignoreTimeScale: true, cancellationToken: token);
                }

                isTyping = false;

                // 타이핑 완료 후 화살표 표시
                if (dialogArrow != null)
                    dialogArrow.gameObject.SetActive(true);
            }
            catch (OperationCanceledException)
            {
                // 취소됨 - 정상 동작
            }
        }

        private void CompleteTyping()
        {
            typingCts?.Cancel();

            dialogText.text = fullText;
            isTyping = false;

            if (dialogArrow != null)
                dialogArrow.gameObject.SetActive(true);
        }

        private async UniTaskVoid LoadIllustrationAsync(Image targetImage, string pathIdString)
        {
            try
            {
                // IllustrationKey는 실제로 Path_ID이므로 PathData를 조회하여 실제 Addressable_Key를 얻어야 함
                if (int.TryParse(pathIdString, out int pathId))
                {
                    var pathData = CSVLoader.Instance.GetData<PathData>(pathId);
                    if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
                    {
                        var sprite = await Addressables.LoadAssetAsync<Sprite>(pathData.Addressable_Key).ToUniTask();
                        if (sprite != null && targetImage != null)
                        {
                            targetImage.sprite = sprite;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[FullDialogView] PathData not found for Path_ID: {pathId}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[FullDialogView] Invalid Path_ID format: {pathIdString}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FullDialogView] Failed to load illustration: {pathIdString}, Error: {e.Message}");
            }
        }
    }
}
