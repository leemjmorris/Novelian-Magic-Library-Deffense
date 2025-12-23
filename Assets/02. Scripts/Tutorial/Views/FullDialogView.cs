using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private string fullText = "";

        [System.Serializable]
        public class CharacterSlot
        {
            public Image illustrationImage;
            public int position; // 0: 왼쪽, 1: 중앙, 2: 오른쪽
        }

        public void Show(TutorialStep step, string text, float typingSpeed)
        {
            if (viewRoot != null)
                viewRoot.SetActive(true);

            // 캐릭터 설정
            SetupCharacters(step);

            // 텍스트 설정
            fullText = text;
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeTextCoroutine(text, typingSpeed));

            // 화살표 숨기기
            if (dialogArrow != null)
                dialogArrow.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (viewRoot != null)
                viewRoot.SetActive(false);

            isTyping = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
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

                    // TODO: Addressables로 일러스트 로드
                    // LoadIllustration(slot.illustrationImage, charInfo.IllustrationKey);
                }

                // 화자 이름 설정
                if (i == step.SpeakerIndex && characterNameText != null)
                {
                    characterNameText.text = charInfo.CharacterName;
                }
            }
        }

        private IEnumerator TypeTextCoroutine(string text, float speed)
        {
            isTyping = true;
            dialogText.text = "";

            foreach (char c in text)
            {
                dialogText.text += c;
                yield return new WaitForSecondsRealtime(speed);
            }

            isTyping = false;
            typingCoroutine = null;

            // 타이핑 완료 후 화살표 표시
            if (dialogArrow != null)
                dialogArrow.gameObject.SetActive(true);
        }

        private void CompleteTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogText.text = fullText;
            isTyping = false;

            if (dialogArrow != null)
                dialogArrow.gameObject.SetActive(true);
        }
    }
}
