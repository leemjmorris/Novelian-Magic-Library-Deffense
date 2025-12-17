using UnityEngine;

namespace GraphicsCat
{
    [ExecuteInEditMode]
    public class GridConfig : MonoBehaviour
    {
        [SerializeField] 
        private Color m_Color = Color.white;

        [SerializeField]
        [Range(0.1f, 3f)]
        private float m_UVScale = 1.0f;

        [SerializeField]
        [Range(0.1f, 10f)]
        private float m_TransformScale = 1.0f;
        [SerializeField]
        [Range(0.1f, 1f)]
        private float m_TransformScaleX = 1.0f;
        [SerializeField]
        [Range(0.1f, 1f)]
        private float m_TransformScaleZ = 1.0f;

        private Vector3 localScale = Vector3.one;

        private void OnEnable()
        {
            UpdateProperties();
        }

        private void OnValidate()
        {
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            localScale.x = m_TransformScale * m_TransformScaleX;
            localScale.z = m_TransformScale * m_TransformScaleZ;
            transform.localScale = localScale;

            var mat = GetMaterial();
            if (mat != null)
            {
                mat.SetColor("_Color", m_Color);
                mat.SetVector("_UVTiling", Vector2.one * m_UVScale);
            }
        }

        private Material GetMaterial()
        {
            if (TryGetComponent<Renderer>(out var renderer))
                return renderer.sharedMaterial;
            return null;
        }
    }
}
