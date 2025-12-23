using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Tutorial
{
    /// <summary>
    /// 변형 구조 대화 UI (중앙/상단, 썸네일 + 대사, 이름 없음)
    /// </summary>
    public class CompactDialogView : MonoBehaviour, ITutorialView, IPointerClickHandler
    {
        [Header("Events")]
        [SerializeField] private TutorialEvents tutorialEvents;

        [Header("UI References")]
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private TextMeshProUGUI dialogText;

        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private string fullText = "";

        public void Show(TutorialStep step, string text, float typingSpeed)
        {
            if (viewRoot != null)
                viewRoot.SetActive(true);

            // 썸네일 설정 (첫 번째 캐릭터의 썸네일 사용)
            if (step.Characters.Count > 0 && thumbnailImage != null)
            {
                thumbnailImage.gameObject.SetActive(true);
                // TODO: Addressables로 썸네일 로드
                // LoadThumbnail(thumbnailImage, step.Characters[0].IllustrationKey);
            }
            else if (thumbnailImage != null)
            {
                thumbnailImage.gameObject.SetActive(false);
            }

            // 텍스트 설정
            fullText = text;
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeTextCoroutine(text, typingSpeed));
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
                CompleteTyping();
            }
            else
            {
                tutorialEvents?.RaiseDialogTouched();
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
        }
    }
}
