using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// 스테이지 선택 팝업 패널
    /// 선택된 스테이지 번호에 맞게 텍스트를 업데이트
    /// 획득 가능 보상 아이콘 표시 (RewardIconHelper 사용)
    /// 클리어 등급 표시 (Issue #645)
    /// </summary>
    public class StagePopUpPanel : MonoBehaviour
    {
        [Header("Stage Name Text")]
        [SerializeField] private TextMeshProUGUI stageNameText;

        [Header("Clear Rank Display (Issue #645)")]
        [SerializeField] private Image clearRankImage;          // 랭크 이미지 (S/A/B/F)
        [SerializeField] private TextMeshProUGUI clearRankText; // 랭크 텍스트 ("-" 표시용)
        [SerializeField] private Sprite[] rankSprites;          // 순서: S(0), A(1), B(2), F(3)

        [Header("Reward Icons")]
        [SerializeField] private Transform rewardIconContainer; // Content 오브젝트
        [SerializeField] private GameObject rewardIconPrefab;   // 아이콘 프리팹 (자식에 HideRewardInfo 패널 포함)
        [SerializeField] private GridLayoutGroup gridLayoutGroup; // Grid Layout Group

        // 동적 생성된 아이콘 오브젝트 리스트
        private List<GameObject> spawnedIcons = new List<GameObject>();

        private void OnEnable()
        {
            UpdateStageText();
            UpdateClearRank(); // Issue #645: 클리어 랭크 표시
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
        /// Issue #645: 클리어 랭크 표시 업데이트
        /// 클리어 안 했으면 "-", 클리어 했으면 랭크 이미지 표시
        /// </summary>
        public void UpdateClearRank()
        {
            if (!SelectedStage.HasSelection)
            {
                ShowNotCleared();
                return;
            }

            int stageNumber = SelectedStage.Data.Chapter_Number;
            int rankIndex = StageProgressManager.Instance != null
                ? StageProgressManager.Instance.GetStageRank(stageNumber)
                : -1;

            if (rankIndex < 0)
            {
                // 클리어 안 함 - "-" 표시
                ShowNotCleared();
            }
            else
            {
                // 클리어 함 - 랭크 이미지 표시
                ShowRank(rankIndex);
            }
        }

        /// <summary>
        /// 클리어 안 한 상태 표시 ("-")
        /// </summary>
        private void ShowNotCleared()
        {
            // 이미지 숨기고 텍스트 표시
            if (clearRankImage != null)
            {
                clearRankImage.gameObject.SetActive(false);
            }

            if (clearRankText != null)
            {
                clearRankText.gameObject.SetActive(true);
                clearRankText.text = "-";
            }
        }

        /// <summary>
        /// 랭크 이미지 표시
        /// </summary>
        private void ShowRank(int rankIndex)
        {
            // 텍스트 숨기고 이미지 표시
            if (clearRankText != null)
            {
                clearRankText.gameObject.SetActive(false);
            }

            if (clearRankImage != null && rankSprites != null && rankIndex >= 0 && rankIndex < rankSprites.Length)
            {
                clearRankImage.gameObject.SetActive(true);
                clearRankImage.sprite = rankSprites[rankIndex];
            }
            else if (clearRankText != null)
            {
                // 스프라이트 없으면 텍스트로 표시
                clearRankText.gameObject.SetActive(true);
                clearRankText.text = rankIndex switch
                {
                    0 => "S",
                    1 => "A",
                    2 => "B",
                    3 => "F",
                    _ => "-"
                };
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

            int rewardGroup1Id = SelectedStage.Data.Reward_Group_1_ID;
            int rewardGroup2Id = SelectedStage.Data.Reward_Group_2_ID;

            // Reward_Group_1 아이콘 생성
            await RewardIconHelper.CreateRewardIcons(
                rewardGroup1Id,
                rewardIconContainer,
                rewardIconPrefab,
                spawnedIcons,
                enableTooltip: true
            );

            // Reward_Group_2 아이콘 생성
            await RewardIconHelper.CreateRewardIcons(
                rewardGroup2Id,
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
