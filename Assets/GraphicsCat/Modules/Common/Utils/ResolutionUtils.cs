using UnityEngine;

namespace GraphicsCat
{
    public class ResolutionUtils
    {
        readonly public static Resolution s_DefaultResolution = Screen.currentResolution;
        readonly public static ScreenOrientation s_DefaultOrientation = Screen.orientation;

        public static float defaultLandscapeHeight
        {
            get
            {
                return (float)Mathf.Min(s_DefaultResolution.width, s_DefaultResolution.height);
            }
        }

        public static float resolutionScale
        {
            get
            {
                var currentResolution = Screen.currentResolution;
                var defaultMaxWidthHeight = (float)Mathf.Max(s_DefaultResolution.width, s_DefaultResolution.height);
                var currentMaxWidthHeight = (float)Mathf.Max(currentResolution.width, currentResolution.height);
                return currentMaxWidthHeight / defaultMaxWidthHeight;
            }
            set
            {
                var isDefaultOrientation = (s_DefaultOrientation == Screen.orientation);
                var newWidth = isDefaultOrientation ? s_DefaultResolution.width : s_DefaultResolution.height;
                var newHeight = isDefaultOrientation ? s_DefaultResolution.height : s_DefaultResolution.width;
                Screen.SetResolution((int)(newWidth * value), (int)(newHeight * value), true);
            }
        }
    }
}
