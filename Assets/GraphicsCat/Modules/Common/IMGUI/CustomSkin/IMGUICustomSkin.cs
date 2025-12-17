using UnityEngine;

namespace GraphicsCat
{
    public partial class IMGUICustomSkin
    {
        static GUISkin s_PreviousSkin;
        static GUISkin s_CustomSkin;
        static bool s_IsCustomSkinLoaded;

        public static void BeginGUI()
        {
            if (s_IsCustomSkinLoaded == false)
            {
                s_CustomSkin = Resources.Load<GUISkin>("CustomSkin");
                s_IsCustomSkinLoaded = true;
            }

            s_PreviousSkin = GUI.skin;
            if (s_CustomSkin != null)
                GUI.skin = s_CustomSkin;
        }

        public static void EndGUI()
        {
            if (GUI.skin != s_PreviousSkin)
                GUI.skin = s_PreviousSkin;
        }
    }
}

