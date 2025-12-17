using UnityEngine;
using UnityEngine.UI;

namespace GraphicsCat
{
    [ExecuteAlways]
    public class DemoDescription : MonoBehaviour
    {
        public Text text;
        public RectTransform background;

        private void Awake()
        {
            var localEulerAngles = transform.localEulerAngles;
            if (localEulerAngles.z == 0)
            {
                localEulerAngles.z = Random.Range(-2f, 2f);
                transform.localEulerAngles = localEulerAngles;
            }
        }

        private void Update()
        {
            if (text == null || background == null)
                return;

            // Calculate preferred text size
            float preferredWidth = text.preferredWidth;
            float preferredHeight = text.preferredHeight;

            // Padding around the text
            float paddingX = 100f;
            float paddingY = 60f;

            // Resize background
            background.sizeDelta = new Vector2(preferredWidth + paddingX, preferredHeight + paddingY);

            // Align background center with text center
            background.pivot = text.rectTransform.pivot;
            background.anchorMin = text.rectTransform.anchorMin;
            background.anchorMax = text.rectTransform.anchorMax;
            background.position = text.rectTransform.position;
        }
    }
}
