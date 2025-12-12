using UnityEngine;
using System.Collections.Generic;

namespace GraphicsCat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class MaterialPropertySetter : MonoBehaviour
    {
        private Renderer targetRenderer;
        private MaterialPropertyBlock materialPropertyBlock;
        private static readonly Dictionary<string, int> propertyIdCache = new Dictionary<string, int>();

        void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            materialPropertyBlock = new MaterialPropertyBlock();
            // Cache initial values from the material
            targetRenderer.GetPropertyBlock(materialPropertyBlock);
        }

        public void SetProperty<T>(string propName, T value)
        {
            SetProperty(propName, value);
        }

        /// <summary>
        /// Sets a float material property on the object's renderer.
        /// The property name's ID is automatically cached.
        /// </summary>
        public void SetProperty(string propName, float value)
        {
            int propertyId = GetPropertyId(propName);
            materialPropertyBlock.SetFloat(propertyId, value);
            targetRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// Sets an integer material property on the object's renderer.
        /// The property name's ID is automatically cached.
        /// </summary>
        public void SetProperty(string propName, int value)
        {
            int propertyId = GetPropertyId(propName);
            materialPropertyBlock.SetInt(propertyId, value);
            targetRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// Sets a Vector4 material property on the object's renderer.
        /// The property name's ID is automatically cached.
        /// </summary>
        public void SetProperty(string propName, Vector4 value)
        {
            int propertyId = GetPropertyId(propName);
            materialPropertyBlock.SetVector(propertyId, value);
            targetRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// Sets a Color material property on the object's renderer.
        /// The property name's ID is automatically cached.
        /// </summary>
        public void SetProperty(string propName, Color value)
        {
            int propertyId = GetPropertyId(propName);
            materialPropertyBlock.SetColor(propertyId, value);
            targetRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// Sets a Matrix4x4 material property on the object's renderer.
        /// The property name's ID is automatically cached.
        /// </summary>
        public void SetProperty(string propName, Matrix4x4 value)
        {
            int propertyId = GetPropertyId(propName);
            materialPropertyBlock.SetMatrix(propertyId, value);
            targetRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// Sets a Texture material property on the object's renderer.
        /// The property name's ID is automatically cached.
        /// </summary>
        public void SetProperty(string propName, Texture value)
        {
            int propertyId = GetPropertyId(propName);
            materialPropertyBlock.SetTexture(propertyId, value);
            targetRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// Gets the property ID from the cache or generates it and adds to the cache.
        /// </summary>
        private static int GetPropertyId(string propName)
        {
            if (!propertyIdCache.TryGetValue(propName, out int propertyId))
            {
                propertyId = Shader.PropertyToID(propName);
                propertyIdCache.Add(propName, propertyId);
            }
            return propertyId;
        }
    }
}