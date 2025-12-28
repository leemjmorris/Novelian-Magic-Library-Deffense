using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// 스테이지 선택 팝업 패널
    /// 선택된 스테이지 번호에 맞게 텍스트를 업데이트
    /// 획득 가능 보상 아이콘 표시 (RewardIconHelper 사용)
    /// </summary>
    public class StagePopUpPanel : MonoBehaviour
    {
        [Header("Stage Name Text")]
        [SerializeField] private TextMeshProUGUI stageNameText;

        [Header("Reward Icons")]
        [SerializeField] private Transform rewardIconContainer; // Content 오브젝트
        [SerializeField] private GameObject rewardIconPrefab;   // 아이콘 프리팹 (자식에 HideRewardInfo 패널 포함)
        [SerializeField] private GridLayoutGroup gridLayoutGroup; // Grid Layout Group

        // 동적 생성된 아이콘 오브젝트 리스트
        private List<GameObject> spawnedIcons = new List<GameObject>();

        private void OnEnable()
        {
            UpdateStageText();
            RewardIconHelper.HideAllTooltips(spawnedIcons);
            // 한 프레임 대기 후 아이콘 업데이트 (SerializedField 초기화 보장)
            DelayedUpdateRewardIcons().Forget();
        }

        private async UniTaskVoid DelayedUpdateRewardIcons()
        {
            // 여러 프레임 대기하면서 참조 확인
            int maxWaitFrames = 5;
            for (int i = 0; i < maxWaitFrames; i++)
            {
                await UniTask.Yield();
                if (rewardIconContainer != null && rewardIconPrefab != null)
                {
                    break;
                }
            }

            // Grid Layout을 1줄로 고정
            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                gridLayoutGroup.constraintCount = 1;
            }

            await UpdateRewardIcons();
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

        /// <summary>
        /// 획득 가능 보상 아이콘 업데이트 (RewardIconHelper 사용)
        /// </summary>
        private async UniTask UpdateRewardIcons()
        {
            // 기존 아이콘 모두 삭제
            RewardIconHelper.ClearIcons(spawnedIcons);

            if (!SelectedStage.HasSelection)
            {
                return;
            }

            if (rewardIconContainer == null || rewardIconPrefab == null)
            {
                // Inspector에서 설정 안 된 경우 무시 (정상 동작)
                return;
            }

            int rewardGroupId = SelectedStage.Data.Reward_Group_ID;
            await RewardIconHelper.CreateRewardIcons(
                rewardGroupId,
                rewardIconContainer,
                rewardIconPrefab,
                spawnedIcons,
                enableTooltip: true
            );
        }

        private void OnDisable()
        {
            RewardIconHelper.ClearIcons(spawnedIcons);
        }

        /// <summary>
        /// 모든 보상 정보 툴팁 숨기기
        /// </summary>
        public void HideRewardInfo()
        {
            RewardIconHelper.HideAllTooltips(spawnedIcons);
        }
    }
}
