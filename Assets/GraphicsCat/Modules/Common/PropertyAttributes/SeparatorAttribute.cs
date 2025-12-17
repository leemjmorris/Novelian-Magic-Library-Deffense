using UnityEngine;

namespace GraphicsCat
{
    public sealed class SeparatorAttribute : PropertyAttribute
    {
        public float Height { get; }

        public SeparatorAttribute(float height = 3f)
        {
            Height = height;
        }
    }
}
