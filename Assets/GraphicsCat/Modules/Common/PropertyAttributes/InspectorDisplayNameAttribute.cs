using UnityEngine;

namespace GraphicsCat
{
    public sealed class InspectorDisplayNameAttribute : PropertyAttribute
    {
        public string displayName { get; }
        public string tooltip { get; }

        public InspectorDisplayNameAttribute(string displayName)
        {
            this.displayName = displayName;
            tooltip = null;
        }

        public InspectorDisplayNameAttribute(string displayName, string tooltip)
        {
            this.displayName = displayName;
            this.tooltip = tooltip;
        }
    }
}