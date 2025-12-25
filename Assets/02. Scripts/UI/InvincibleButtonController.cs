using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wall 무적 모드 토글 버튼 컨트롤러 (테스트용)
/// </summary>
public class InvincibleButtonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Wall wall;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(0.5f, 1f, 0.5f, 1f); // 연한 초록색

    private bool isInvincible = false;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(ToggleInvincible);
        }

        UpdateButtonVisual();
    }

    private void ToggleInvincible()
    {
        // 버튼 클릭 시점에 Wall 찾기 (맵 로드 후 동적 생성되므로)
        if (wall == null)
        {
            GameObject wallObj = GameObject.FindWithTag("Wall");
            if (wallObj != null)
            {
                wall = wallObj.GetComponent<Wall>();
            }
        }

        if (wall == null)
        {
            Debug.LogWarning("[InvincibleButtonController] Wall을 찾을 수 없습니다! 맵이 로드되었는지 확인하세요.");
            return;
        }

        isInvincible = !isInvincible;
        wall.SetInvincible(isInvincible);
        UpdateButtonVisual();
    }

    private void UpdateButtonVisual()
    {
        if (buttonImage != null)
        {
            buttonImage.color = isInvincible ? activeColor : normalColor;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ToggleInvincible);
        }
    }
}
