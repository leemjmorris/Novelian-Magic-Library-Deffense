//LMJ : Static utility class for Character visual effects (Beam, Layer, Collider management)
namespace Novelian.Combat
{
    using UnityEngine;

    /// <summary>
    /// 캐릭터 이펙트 유틸리티 (정적 클래스)
    /// - 레이어 설정 (Wall 통과용)
    /// - Collider 비활성화
    /// - 빔 이펙트 관리
    /// - 렌더링 순서 조정
    /// </summary>
    public static class CharacterEffectUtils
    {
        #region Layer Management

        /// <summary>
        /// 게임오브젝트와 모든 자식의 레이어 설정 (재귀)
        /// Wall 통과를 위해 Projectile 레이어로 설정할 때 사용
        /// </summary>
        public static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 레이어 이름으로 설정
        /// </summary>
        public static void SetLayerRecursively(GameObject obj, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1)
            {
                SetLayerRecursively(obj, layer);
            }
            else
            {
                Debug.LogWarning($"[CharacterEffectUtils] Layer not found: {layerName}");
            }
        }

        #endregion

        #region Collider Management

        /// <summary>
        /// 게임오브젝트와 모든 자식의 Collider 비활성화 (재귀)
        /// 빔 이펙트 등 시각 효과만 필요한 경우 사용
        /// </summary>
        public static void DisableCollidersRecursively(GameObject obj)
        {
            if (obj == null) return;

            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }

        /// <summary>
        /// 게임오브젝트와 모든 자식의 Collider 활성화 (재귀)
        /// </summary>
        public static void EnableCollidersRecursively(GameObject obj)
        {
            if (obj == null) return;

            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
        }

        #endregion

        #region Beam Effect Management

        /// <summary>
        /// 빔 이펙트의 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
        /// Renderer의 sortingOrder 또는 material renderQueue 조정
        /// </summary>
        public static void SetBeamRenderingOrder(GameObject obj, int orderOffset)
        {
            if (obj == null) return;

            // LineRenderer 처리
            LineRenderer[] lineRenderers = obj.GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lr in lineRenderers)
            {
                lr.sortingOrder = orderOffset;

                // Material의 renderQueue도 조정
                if (lr.material != null)
                {
                    lr.material.renderQueue = 3000 + orderOffset;
                }
            }

            // 일반 Renderer 처리
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is LineRenderer) continue; // 이미 처리됨

                renderer.sortingOrder = orderOffset;

                // Material renderQueue 조정 (투명 오브젝트 기준)
                foreach (Material mat in renderer.materials)
                {
                    if (mat != null)
                    {
                        mat.renderQueue = 3000 + orderOffset;
                    }
                }
            }
        }

        /// <summary>
        /// RetroBeamStatic 등 빔 에셋의 beamCollides 비활성화
        /// Wall Raycast 충돌을 방지
        /// </summary>
        public static void DisableBeamCollision(GameObject obj)
        {
            if (obj == null) return;

            // RetroBeamStatic 컴포넌트 찾기 (리플렉션 사용)
            var beamComponents = obj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in beamComponents)
            {
                if (component == null) continue;

                System.Type type = component.GetType();

                // beamCollides 필드 비활성화 (RetroBeamStatic 등)
                var beamCollidesField = type.GetField("beamCollides",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (beamCollidesField != null && beamCollidesField.FieldType == typeof(bool))
                {
                    beamCollidesField.SetValue(component, false);
                    Debug.Log($"[CharacterEffectUtils] Disabled beamCollides on {component.GetType().Name}");
                }

                // collisionEnabled 필드 비활성화 (다른 빔 에셋)
                var collisionEnabledField = type.GetField("collisionEnabled",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (collisionEnabledField != null && collisionEnabledField.FieldType == typeof(bool))
                {
                    collisionEnabledField.SetValue(component, false);
                }
            }
        }

        /// <summary>
        /// 빔 이펙트 위치/방향 업데이트 (시작점 → 끝점)
        /// LineRenderer 또는 Transform 기반 빔 처리
        /// </summary>
        public static void UpdateBeamEffect(GameObject beamEffect, Vector3 startPos, Vector3 endPos)
        {
            if (beamEffect == null) return;

            // LineRenderer 기반 빔
            LineRenderer lineRenderer = beamEffect.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
                return;
            }

            // Transform 기반 빔 (스케일로 길이 조절)
            beamEffect.transform.position = startPos;

            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;

            if (distance > 0.01f)
            {
                // 빔 방향 설정
                beamEffect.transform.rotation = Quaternion.LookRotation(direction.normalized);

                // 빔 길이 설정 (Z축 스케일)
                Vector3 scale = beamEffect.transform.localScale;
                scale.z = distance;
                beamEffect.transform.localScale = scale;
            }
        }

        /// <summary>
        /// 빔 이펙트 위치/방향 업데이트 (LineRenderer 직접)
        /// </summary>
        public static void UpdateLineRenderer(LineRenderer lineRenderer, Vector3 startPos, Vector3 endPos)
        {
            if (lineRenderer == null) return;

            if (lineRenderer.positionCount < 2)
            {
                lineRenderer.positionCount = 2;
            }

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }

        #endregion

        #region Particle Effect Management

        /// <summary>
        /// 파티클 시스템 재생
        /// </summary>
        public static void PlayParticleSystem(GameObject obj)
        {
            if (obj == null) return;

            ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particles)
            {
                ps.Play();
            }
        }

        /// <summary>
        /// 파티클 시스템 정지
        /// </summary>
        public static void StopParticleSystem(GameObject obj, bool clear = false)
        {
            if (obj == null) return;

            ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particles)
            {
                ps.Stop();
                if (clear)
                {
                    ps.Clear();
                }
            }
        }

        #endregion
    }
}
