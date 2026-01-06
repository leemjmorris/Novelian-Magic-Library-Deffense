using UnityEngine;

/// <summary>
/// Dissolve 효과에 필요한 셰이더, 텍스처, 설정을 중앙 관리하는 ScriptableObject
/// Monster 사망 시 런타임에서 Material을 교체할 때 사용
/// </summary>
[CreateAssetMenu(fileName = "DissolveSettings", menuName = "Game/Dissolve Settings")]
public class DissolveSettings : ScriptableObject
{
    [Header("Dissolve Shader")]
    [Tooltip("Dissolve 효과에 사용할 셰이더 (Custom/URPDissolve)")]
    [SerializeField] private Shader dissolveShader;

    [Header("Noise Texture")]
    [Tooltip("Dissolve 패턴에 사용할 Noise 텍스처")]
    [SerializeField] private Texture2D noiseTexture;

    [Header("Visual Settings")]
    [Tooltip("Dissolve 엣지 색상")]
    [SerializeField] private Color edgeColor = new Color(1f, 0.5f, 0f, 1f); // 주황색

    [Tooltip("Dissolve 엣지 두께")]
    [Range(0f, 0.2f)]
    [SerializeField] private float edgeWidth = 0.05f;

    [Tooltip("Emission 효과 사용 여부")]
    [SerializeField] private bool useEmission = true;

    [Header("Dust Particle")]
    [Tooltip("Dissolve 시작 시 재생되는 먼지 파티클 프리팹")]
    [SerializeField] private GameObject dustParticlePrefab;

    // 셰이더 프로퍼티 ID (성능 최적화)
    private static readonly int BASE_MAP = Shader.PropertyToID("_BaseMap");
    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int NOISE_MAP = Shader.PropertyToID("_NoiseMap");
    private static readonly int DISSOLVE_AMOUNT = Shader.PropertyToID("_DissolveAmount");
    private static readonly int EDGE_WIDTH = Shader.PropertyToID("_EdgeWidth");
    private static readonly int EDGE_COLOR = Shader.PropertyToID("_EdgeColor");
    private static readonly int USE_EMISSION = Shader.PropertyToID("_UseEmission");

    // 기존 URP Lit 셰이더 프로퍼티
    private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");

    /// <summary>
    /// 원본 Material을 Dissolve Material로 교체하여 반환
    /// 원본 텍스처와 색상을 유지하면서 Dissolve 셰이더 적용
    /// </summary>
    public Material CreateDissolveMaterial(Material originalMaterial)
    {
        if (dissolveShader == null)
        {
            GameLog.LogWarning("[DissolveSettings] Dissolve Shader가 설정되지 않았습니다!");
            return null;
        }

        // 새 Material 생성 (Dissolve 셰이더 사용)
        Material dissolveMat = new Material(dissolveShader);

        // 원본 Material에서 텍스처 복사
        Texture mainTex = null;
        Color baseColor = Color.white;

        // _BaseMap (URP) 또는 _MainTex (Standard) 에서 텍스처 가져오기
        if (originalMaterial.HasProperty(BASE_MAP))
        {
            mainTex = originalMaterial.GetTexture(BASE_MAP);
        }
        else if (originalMaterial.HasProperty(MAIN_TEX))
        {
            mainTex = originalMaterial.GetTexture(MAIN_TEX);
        }

        // 색상 가져오기
        if (originalMaterial.HasProperty(BASE_COLOR))
        {
            baseColor = originalMaterial.GetColor(BASE_COLOR);
        }
        else if (originalMaterial.HasProperty("_Color"))
        {
            baseColor = originalMaterial.GetColor("_Color");
        }

        // Dissolve Material에 값 설정
        if (mainTex != null)
        {
            dissolveMat.SetTexture(BASE_MAP, mainTex);
        }
        dissolveMat.SetColor(BASE_COLOR, baseColor);

        // Noise 텍스처 설정
        if (noiseTexture != null)
        {
            dissolveMat.SetTexture(NOISE_MAP, noiseTexture);
        }

        // Dissolve 설정
        dissolveMat.SetFloat(DISSOLVE_AMOUNT, 0f); // 시작값 0
        dissolveMat.SetFloat(EDGE_WIDTH, edgeWidth);
        dissolveMat.SetColor(EDGE_COLOR, edgeColor);
        dissolveMat.SetFloat(USE_EMISSION, useEmission ? 1f : 0f);

        return dissolveMat;
    }

    /// <summary>
    /// Dissolve Shader 참조 반환
    /// </summary>
    public Shader GetDissolveShader() => dissolveShader;

    /// <summary>
    /// Noise Texture 참조 반환
    /// </summary>
    public Texture2D GetNoiseTexture() => noiseTexture;

    /// <summary>
    /// Dust Particle 프리팹 반환
    /// </summary>
    public GameObject GetDustParticlePrefab() => dustParticlePrefab;
}
