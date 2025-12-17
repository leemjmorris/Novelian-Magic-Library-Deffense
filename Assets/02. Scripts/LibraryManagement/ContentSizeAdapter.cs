using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.LibraryManagement
{
    /// <summary>
    /// LCB: Content 크기에 맞춰 Grid Layout의 셀 크기와 열 개수를 자동 조절
    /// LCB: 250x400 비율(5:8)을 유지하며 화면에 맞게 행/열 자동 정렬, 좌우 대칭 간격
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ContentSizeAdapter : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private float targetCellWidth = 250f;   // 목표 셀 너비
        [SerializeField] private float cellAspectRatio = 0.625f; // 가로:세로 비율 (250/400 = 0.625)
        [SerializeField] private float minSpacing = 20f;         // 최소 셀 간 간격
        [SerializeField] private float maxSpacing = 60f;         // 최대 셀 간 간격

        [Header("Column Limits")]
        [SerializeField] private int minColumns = 2;             // 최소 열 개수
        [SerializeField] private int maxColumns = 5;             // 최대 열 개수

        private GridLayoutGroup gridLayout;
        private RectTransform rectTransform;
        private float lastWidth = -1f;
        private float lastHeight = -1f;

        private void Awake()
        {
            gridLayout = GetComponent<GridLayoutGroup>();
            rectTransform = GetComponent<RectTransform>();

            // Grid Layout 기본 설정
            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            }
        }

        private void Start()
        {
            AdjustCellSize();
        }

        private void Update()
        {
            // Content 크기가 변경되었을 때만 재계산
            float currentWidth = rectTransform.rect.width;
            float currentHeight = rectTransform.rect.height;

            if (Mathf.Abs(currentWidth - lastWidth) > 0.1f ||
                Mathf.Abs(currentHeight - lastHeight) > 0.1f)
            {
                lastWidth = currentWidth;
                lastHeight = currentHeight;
                AdjustCellSize();
            }
        }

        /// <summary>
        /// LCB: Content 크기에 맞춰 셀 크기와 열 개수 자동 계산 (좌우 대칭)
        /// </summary>
        private void AdjustCellSize()
        {
            if (gridLayout == null || rectTransform == null) return;

            // Content 전체 너비
            float contentWidth = rectTransform.rect.width;

            // 최적의 열 개수 계산 (목표 셀 너비 기준)
            int optimalColumns = Mathf.RoundToInt(contentWidth / (targetCellWidth + minSpacing));
            optimalColumns = Mathf.Clamp(optimalColumns, minColumns, maxColumns);

            // 실제 셀 너비 계산 (좌우 대칭을 위해)
            // 공식: contentWidth = leftPadding + (cellWidth * columns) + (spacing * (columns - 1)) + rightPadding
            // 단순화: contentWidth = (cellWidth + spacing) * columns + spacing
            float cellWidth = (contentWidth - minSpacing) / optimalColumns - minSpacing;

            // 셀 너비가 너무 작으면 열 개수 줄이기
            if (cellWidth < targetCellWidth * 0.7f && optimalColumns > minColumns)
            {
                optimalColumns--;
                cellWidth = (contentWidth - minSpacing) / optimalColumns - minSpacing;
            }

            // 좌우 대칭 간격 계산
            float totalCellWidth = cellWidth * optimalColumns;
            float totalSpacingWidth = contentWidth - totalCellWidth;
            float spacing = totalSpacingWidth / (optimalColumns + 1);

            // 간격 제한 (너무 크거나 작지 않도록)
            spacing = Mathf.Clamp(spacing, minSpacing, maxSpacing);

            // 간격 적용 후 셀 너비 재계산 (정확한 좌우 대칭)
            float actualSpacing = spacing;
            float actualCellWidth = (contentWidth - actualSpacing * (optimalColumns + 1)) / optimalColumns;
            float actualCellHeight = actualCellWidth / cellAspectRatio;

            // Grid Layout에 적용
            gridLayout.constraintCount = optimalColumns;
            gridLayout.cellSize = new Vector2(actualCellWidth, actualCellHeight);
            gridLayout.spacing = new Vector2(actualSpacing, actualSpacing);
            gridLayout.padding = new RectOffset(
                (int)actualSpacing,  // left
                (int)actualSpacing,  // right
                (int)actualSpacing,  // top
                (int)actualSpacing   // bottom
            );

            Debug.Log($"[ContentSizeAdapter] Content: {contentWidth:F0}px, " +
                      $"Columns: {optimalColumns}, Cell: {actualCellWidth:F0}x{actualCellHeight:F0}, " +
                      $"Spacing: {actualSpacing:F0}");
        }

        /// <summary>
        /// LCB: 외부에서 강제로 크기 재조정 호출 가능
        /// </summary>
        public void ForceAdjust()
        {
            AdjustCellSize();
        }

        /// <summary>
        /// LCB: Inspector에서 값 변경 시 즉시 반영 (에디터 전용)
        /// </summary>
        private void OnValidate()
        {
            if (gridLayout == null)
            {
                gridLayout = GetComponent<GridLayoutGroup>();
            }

            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

#if UNITY_EDITOR
            // 에디터에서 값 변경 시 즉시 적용
            if (Application.isPlaying && gridLayout != null)
            {
                AdjustCellSize();
            }
#endif
        }
    }
}
