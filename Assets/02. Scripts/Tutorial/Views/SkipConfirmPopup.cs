using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tutorial
{
    /// <summary>
    /// 스킵 확인 팝업
    /// "해당 튜토리얼을 건너 뛰시겠습니까?"
    /// </summary>
    public class SkipConfirmPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Texts")]
        [SerializeField] private string defaultTitle = "해당 튜토리얼을 건너 뛰시겠습니까?";
        [SerializeField] private string defaultMessage = "건너 뛴 내용은 튜토리얼\n다시보기에서 확인하실 수 있습니다.";

        private Action onConfirmCallback;
        private Action onCancelCallback;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            Hide();
        }

        public void Show(Action onConfirm, Action onCancel)
        {
            onConfirmCallback = onConfirm;
            onCancelCallback = onCancel;

            if (titleText != null)
                titleText.text = defaultTitle;

            if (messageText != null)
                messageText.text = defaultMessage;

            if (popupRoot != null)
                popupRoot.SetActive(true);
        }

        public void Show(string title, string message, Action onConfirm, Action onCancel)
        {
            onConfirmCallback = onConfirm;
            onCancelCallback = onCancel;

            if (titleText != null)
                titleText.text = title;

            if (messageText != null)
                messageText.text = message;

            if (popupRoot != null)
                popupRoot.SetActive(true);
        }

        public void Hide()
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);

            onConfirmCallback = null;
            onCancelCallback = null;
        }

        private void OnConfirmClicked()
        {
            var callback = onConfirmCallback;
            Hide();
            callback?.Invoke();
        }

        private void OnCancelClicked()
        {
            var callback = onCancelCallback;
            Hide();
            callback?.Invoke();
        }
    }
}
