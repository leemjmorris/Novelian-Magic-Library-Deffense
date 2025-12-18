using UnityEngine;
using TMPro;
using NovelianMagicLibraryDefense.Core;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// 스테이지 선택 팝업 패널
    /// 선택된 스테이지 번호에 맞게 텍스트를 업데이트
    /// </summary>
    public class StagePopUpPanel : MonoBehaviour
    {
        [Header("Stage Name Text")]
        [SerializeField] private TextMeshProUGUI stageNameText;

        private void OnEnable()
        {
            UpdateStageText();
        }

        /// <summary>
        /// SelectedStage 데이터를 기반으로 스테이지 텍스트 업데이트
        /// </summary>
        public void UpdateStageText()
        {
            if (stageNameText == null) return;

            if (SelectedStage.HasSelection)
            {
                int chapterNumber = SelectedStage.Data.Chapter_Number;
                stageNameText.text = $"스테이지 {chapterNumber}";
            }
            else
            {
                stageNameText.text = "스테이지 -";
            }
        }

        /// <summary>
        /// 스테이지 번호를 직접 설정 (StageButton에서 호출 가능)
        /// </summary>
        public void SetStageNumber(int stageNumber)
        {
            if (stageNameText != null)
            {
                stageNameText.text = $"스테이지 {stageNumber}";
            }
        }
    }
}
