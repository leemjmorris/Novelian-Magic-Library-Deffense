using DG.Tweening;
using UnityEngine;

/// <summary>
/// DOTween을 사용한 부드러운 확대/축소 애니메이션 컴포넌트
/// 파견 맵 선택 등에서 재사용 가능
/// </summary>
public class ScaleAnimator : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease selectEase = Ease.OutBack;
    [SerializeField] private Ease deselectEase = Ease.OutQuad;

    private Vector3 originalScale;
    private Tween currentTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// 선택 시 확대 애니메이션
    /// </summary>
    public void PlaySelect()
    {
        KillCurrentTween();
        currentTween = transform.DOScale(originalScale * selectedScale, duration)
            .SetEase(selectEase);
    }

    /// <summary>
    /// 선택 해제 시 원래 크기로 복귀
    /// </summary>
    public void PlayDeselect()
    {
        KillCurrentTween();
        currentTween = transform.DOScale(originalScale, duration)
            .SetEase(deselectEase);
    }

    /// <summary>
    /// 즉시 선택 상태로 변경 (애니메이션 없이)
    /// </summary>
    public void SetSelectedImmediate()
    {
        KillCurrentTween();
        transform.localScale = originalScale * selectedScale;
    }

    /// <summary>
    /// 즉시 선택 해제 상태로 변경 (애니메이션 없이)
    /// </summary>
    public void SetDeselectedImmediate()
    {
        KillCurrentTween();
        transform.localScale = originalScale;
    }

    private void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }
}
