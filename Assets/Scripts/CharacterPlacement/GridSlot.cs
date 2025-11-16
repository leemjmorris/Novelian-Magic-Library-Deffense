using UnityEngine;

/// <summary>
/// World coordinate-based 2D Sprite grid slot
/// Represents a position where characters can be placed
/// </summary>
public class GridSlot : MonoBehaviour
{
    [Header("Slot Info")]
    [SerializeField] private int slotIndex;             // Slot number (0-9)
    [SerializeField] private SpriteRenderer spriteRenderer; // Grid visualization

    [Header("Slot State")]
    private GameObject currentCharacter;  // Currently placed character GameObject
    private bool isOccupied = false;      // Whether slot is occupied

    private void Awake()
    {
        // Auto-setup SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Initialize slot (called from CharacterPlacementManager)
    /// </summary>
    public void Initialize(int index)
    {
        slotIndex = index;
        HideGrid();
    }

    /// <summary>
    /// Show grid
    /// </summary>
    public void ShowGrid()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    /// <summary>
    /// Hide grid
    /// </summary>
    public void HideGrid()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Place character
    /// </summary>
    public void PlaceCharacter(GameObject character)
    {
        if (character == null) return;

        currentCharacter = character;
        isOccupied = true;

        // Move character to slot position
        character.transform.position = transform.position;

        Debug.Log($"[GridSlot {slotIndex}] Character placed");
    }

    /// <summary>
    /// Remove character
    /// </summary>
    public void RemoveCharacter()
    {
        currentCharacter = null;
        isOccupied = false;

        Debug.Log($"[GridSlot {slotIndex}] Character removed");
    }

    /// <summary>
    /// Check if slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return !isOccupied;
    }

    /// <summary>
    /// Get currently placed character
    /// </summary>
    public GameObject GetCurrentCharacter()
    {
        return currentCharacter;
    }

    /// <summary>
    /// Get slot index
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }

    /// <summary>
    /// Get slot's world position
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }
}
