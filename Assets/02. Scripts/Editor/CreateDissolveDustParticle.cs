using UnityEngine;
using UnityEditor;

/// <summary>
/// Dissolve 효과용 Dust Particle 프리팹을 생성하는 에디터 유틸리티
/// 타노스 스냅 스타일의 입자 분해 효과
/// </summary>
public static class CreateDissolveDustParticle
{
    [MenuItem("Tools/Create Dissolve Dust Particle")]
    public static void CreateParticle()
    {
        // 저장 경로 설정
        string folderPath = "Assets/03. Prefabs/Effects";
        string prefabPath = folderPath + "/DissolveDustParticle.prefab";

        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/03. Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "03. Prefabs");
            }
            AssetDatabase.CreateFolder("Assets/03. Prefabs", "Effects");
        }

        // 루트 게임오브젝트 생성
        GameObject particleRoot = new GameObject("DissolveDustParticle");

        // 메인 파티클 시스템 추가
        ParticleSystem ps = particleRoot.AddComponent<ParticleSystem>();

        // Main 모듈 설정
        var main = ps.main;
        main.duration = 2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.8f, 0.6f, 0.4f, 1f),  // 베이지/갈색 톤
            new Color(0.5f, 0.4f, 0.3f, 1f)
        );
        main.gravityModifier = -0.1f; // 살짝 위로 떠오름
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.maxParticles = 500;

        // Emission 모듈 설정 - 버스트로 한번에 많은 입자
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 200, 300, 1, 0.01f)
        });

        // Shape 모듈 설정 - 구형으로 몬스터 크기만큼
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;
        shape.radiusThickness = 1f; // 전체 볼륨에서 방출

        // Velocity over Lifetime - 바깥쪽으로 흩어지는 효과
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

        // Color over Lifetime - 페이드 아웃
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.7f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Size over Lifetime - 점점 작아짐
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 0.8f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Noise 모듈 - 자연스러운 움직임
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 1f;
        noise.scrollSpeed = 0.5f;
        noise.damping = true;

        // Renderer 설정
        ParticleSystemRenderer renderer = particleRoot.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // 기본 파티클 머티리얼 찾기 또는 생성
        Material particleMat = FindOrCreateParticleMaterial();
        if (particleMat != null)
        {
            renderer.material = particleMat;
        }

        // 프리팹으로 저장
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(particleRoot, prefabPath);

        // 임시 오브젝트 삭제
        Object.DestroyImmediate(particleRoot);

        // 생성된 프리팹 선택
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;

        Debug.Log($"[CreateDissolveDustParticle] Dust Particle 프리팹이 생성되었습니다: {prefabPath}");
        EditorUtility.DisplayDialog("완료",
            $"Dissolve Dust Particle 프리팹이 생성되었습니다.\n\n" +
            $"경로: {prefabPath}\n\n" +
            $"DissolveSettings의 Dust Particle Prefab 필드에 할당해주세요.",
            "확인");
    }

    private static Material FindOrCreateParticleMaterial()
    {
        // URP Default Particle 머티리얼 찾기
        string[] guids = AssetDatabase.FindAssets("t:Material Default-Particle");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        // URP Particles/Unlit 셰이더로 새 머티리얼 생성
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Particles/Standard Unlit");
        }

        if (particleShader != null)
        {
            string matPath = "Assets/03. Prefabs/Effects/DissolveDustMaterial.mat";
            Material mat = new Material(particleShader);
            mat.SetColor("_BaseColor", new Color(0.9f, 0.8f, 0.7f, 1f));

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        return null;
    }
}
