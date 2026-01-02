using UnityEngine;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Account UI 패널 관리
    /// 닫기 버튼 등 Account UI 내부 기능 담당
    /// </summary>
    public class AccountUI : MonoBehaviour
    {
        [SerializeField] private GameObject accountUIPanel; // 본인 패널 참조

        /// <summary>
        /// 닫기 버튼 클릭 시 패널 비활성화
        /// Inspector에서 X 버튼의 OnClick에 연결
        /// </summary>
        public void OnCloseButton()
        {
            if (accountUIPanel != null)
            {
                accountUIPanel.SetActive(false);
            }
        }
    }
}