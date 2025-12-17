using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// LMJ: Floating damage text that rises and fades out
/// Uses object pooling for performance
/// </summary>
public class FloatingDamageText : MonoBehaviour, IPoolable
{
    [Header("References")]
    [SerializeField] private TextMeshPro damageText;

    [Header("Animation Settings")]
    [SerializeField] private float floatHeight = 1f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0.5f, 0.2f, 1f);

    [Header("Color Settings")]
    [SerializeField] private Color normalDamageColor = Color.white;
    [SerializeField] private Color criticalDamageColor = Color.yellow;
    [SerializeField] private Color healColor = Color.green;

    private CancellationTokenSource animationCts;
    private Vector3 startPosition;
    private float initialScale;
    private Camera mainCamera;

    private void Awake()
    {
        if (damageText == null)
        {
            damageText = GetComponent<TextMeshPro>();
        }
        initialScale = transform.localScale.x;
        mainCamera = Camera.main;
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
        startPosition = transform.position;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    public void OnDespawn()
    {
        animationCts?.Cancel();
        animationCts?.Dispose();
        animationCts = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Initialize and play the floating damage text animation
    /// </summary>
    /// <param name="damage">Damage value to display</param>
    /// <param name="isCritical">Is this a critical hit?</param>
    /// <param name="isHeal">Is this healing instead of damage?</param>
    public void Initialize(float damage, bool isCritical = false, bool isHeal = false)
    {
        // Set text
        string prefix = isHeal ? "+" : "-";
        damageText.text = $"{prefix}{Mathf.RoundToInt(damage)}";

        // Set color
        if (isHeal)
        {
            damageText.color = healColor;
        }
        else if (isCritical)
        {
            damageText.color = criticalDamageColor;
            damageText.text = $"{Mathf.RoundToInt(damage)}!";
        }
        else
        {
            damageText.color = normalDamageColor;
        }

        // Start animation
        animationCts?.Cancel();
        animationCts?.Dispose();
        animationCts = new CancellationTokenSource();
        AnimateAsync(animationCts.Token).Forget();
    }

    /// <summary>
    /// Initialize with custom text (for status effects like "STUN", "IMMUNE", etc.)
    /// </summary>
    public void InitializeStatus(string statusText, Color color)
    {
        damageText.text = statusText;
        damageText.color = color;

        animationCts?.Cancel();
        animationCts?.Dispose();
        animationCts = new CancellationTokenSource();
        AnimateAsync(animationCts.Token).Forget();
    }

    private async UniTaskVoid AnimateAsync(CancellationToken ct)
    {
        float elapsed = 0f;
        Vector3 targetPosition = startPosition + Vector3.up * floatHeight;
        Color startColor = damageText.color;

        while (elapsed < duration && !ct.IsCancellationRequested)
        {
            float t = elapsed / duration;

            // Move upward
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // Fade out
            float alpha = fadeCurve.Evaluate(t);
            damageText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            // Scale animation (pop effect)
            float scale = scaleCurve.Evaluate(t) * initialScale;
            transform.localScale = new Vector3(scale, scale, scale);

            // Billboard - always face camera
            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (!ct.IsCancellationRequested)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (NovelianMagicLibraryDefense.Managers.GameManager.Instance != null &&
            NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool != null)
        {
            NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        animationCts?.Cancel();
        animationCts?.Dispose();
    }
}
