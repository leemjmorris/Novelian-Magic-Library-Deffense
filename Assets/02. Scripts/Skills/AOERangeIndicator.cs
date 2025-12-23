using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Novelian.Combat
{
    /// <summary>
    /// AOE 범위 시각화 컴포넌트
    /// LineRenderer로 원형 범위를 표시하고 페이드아웃
    /// </summary>
    public class AOERangeIndicator : MonoBehaviour
    {
        #region Settings
        [Header("표시 설정")]
        [SerializeField] private int segments = 64;
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float heightOffset = 0.1f;

        [Header("색상")]
        [SerializeField] private Color damageColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color warningColor = new Color(1f, 1f, 0f, 0.8f);
        [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.3f, 0.8f);
        #endregion

        #region Components
        private LineRenderer lineRenderer;
        private Material lineMaterial;
        #endregion

        #region Static Pool
        private static GameObject indicatorPrefab;
        private static Transform poolParent;

        /// <summary>
        /// AOE 범위 표시 (정적 메서드)
        /// </summary>
        public static void Show(Vector3 position, float radius, IndicatorType type = IndicatorType.Damage)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
            if (radius <= 0) return;

            var indicator = GetOrCreateIndicator();
            indicator.transform.position = position;
            indicator.Display(radius, type);
#endif
        }

        /// <summary>
        /// 경고 표시 후 데미지 범위 표시 (낙하 스킬용)
        /// </summary>
        public static void ShowWithWarning(Vector3 position, float radius, float warningDuration)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
            if (radius <= 0) return;

            var indicator = GetOrCreateIndicator();
            indicator.transform.position = position;
            indicator.DisplayWithWarning(radius, warningDuration).Forget();
#endif
        }

        private static AOERangeIndicator GetOrCreateIndicator()
        {
            // 풀 부모 생성
            if (poolParent == null)
            {
                var poolObj = new GameObject("[AOE Range Indicators]");
                poolParent = poolObj.transform;
                Object.DontDestroyOnLoad(poolObj);
            }

            // 새 인디케이터 생성
            var indicatorObj = new GameObject("AOE_Indicator");
            indicatorObj.transform.SetParent(poolParent);
            return indicatorObj.AddComponent<AOERangeIndicator>();
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = segments;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            // 기본 머티리얼 생성
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.renderQueue = 3100; // 투명 오브젝트 위에 렌더링
            lineRenderer.material = lineMaterial;
            lineRenderer.sortingOrder = 100;
        }
        #endregion

        #region Display Methods
        public void Display(float radius, IndicatorType type)
        {
            Color color = type switch
            {
                IndicatorType.Damage => damageColor,
                IndicatorType.Warning => warningColor,
                IndicatorType.Heal => healColor,
                _ => damageColor
            };

            DrawCircle(radius);
            SetColor(color);
            FadeOutAndDestroy().Forget();
        }

        private async UniTaskVoid DisplayWithWarning(float radius, float warningDuration)
        {
            DrawCircle(radius);
            SetColor(warningColor);

            // 경고 표시 (깜빡임)
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                float alpha = Mathf.PingPong(elapsed * 4f, 1f) * 0.5f + 0.3f;
                SetAlpha(alpha);
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            // 데미지 범위로 전환
            SetColor(damageColor);
            await FadeOutAndDestroy();
        }

        private void DrawCircle(float radius)
        {
            Vector3[] positions = new Vector3[segments];
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                positions[i] = new Vector3(x, heightOffset, z);
            }

            lineRenderer.SetPositions(positions);
        }

        private void SetColor(Color color)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            if (lineMaterial != null)
            {
                lineMaterial.color = color;
            }
        }

        private void SetAlpha(float alpha)
        {
            Color startColor = lineRenderer.startColor;
            startColor.a = alpha;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = startColor;
        }

        private async UniTask FadeOutAndDestroy()
        {
            // 표시 유지
            await UniTask.Delay((int)(displayDuration * 1000));

            // 페이드아웃
            float elapsed = 0f;
            Color startColor = lineRenderer.startColor;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeOutDuration);
                SetAlpha(alpha);
                await UniTask.Yield();
            }

            // 파괴
            if (lineMaterial != null)
            {
                Destroy(lineMaterial);
            }
            Destroy(gameObject);
        }
        #endregion

        #region Enums
        public enum IndicatorType
        {
            Damage,
            Warning,
            Heal
        }
        #endregion
    }
}
