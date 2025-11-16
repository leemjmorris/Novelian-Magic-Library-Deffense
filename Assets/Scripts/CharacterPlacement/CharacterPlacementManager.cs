using System.Collections.Generic;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

/// <summary>
/// World coordinate-based character placement system manager
/// - Creates and manages 5x2 grid
/// - Places characters in random slots
/// - Moves characters via drag and drop
/// </summary>
public class CharacterPlacementManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private GameObject gridSlotPrefab;  // GridSlot prefab
    [SerializeField] private Transform gridParent;       // Grid parent object

    [Header("Grid Layout")]
    [SerializeField] private int gridColumns = 5;        // 5 columns
    [SerializeField] private int gridRows = 2;           // 2 rows
    [SerializeField] private float gridSpacingX = 1.5f;  // X spacing
    [SerializeField] private float gridSpacingY = 1.5f;  // Y spacing
    [SerializeField] private Vector3 gridStartPosition = new Vector3(-3f, 0.75f, 0f); // Start position

    [Header("Character Settings")]
    [SerializeField] private GameObject characterPrefab; // Character prefab (SpriteRenderer only)

    // Grid slot list
    private List<GridSlot> gridSlots = new List<GridSlot>();

    // Drag state management
    private GameObject draggingCharacter;
    private GridSlot originalSlot;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        // Create grid
        CreateGrid();
    }

    private void OnEnable()
    {
        Debug.Log("[CharacterPlacementManager] OnEnable called! Starting InputManager event subscription");
        // Subscribe to InputManager events
        InputManager.OnLongPressStart += HandleLongPressStart;
        InputManager.OnDragUpdate += HandleDragUpdate;
        InputManager.OnDrop += HandleDrop;
        Debug.Log("[CharacterPlacementManager] InputManager event subscription completed");
    }

    private void OnDisable()
    {
        // Unsubscribe from InputManager events
        InputManager.OnLongPressStart -= HandleLongPressStart;
        InputManager.OnDragUpdate -= HandleDragUpdate;
        InputManager.OnDrop -= HandleDrop;
    }

    /// <summary>
    /// Create 5x2 grid
    /// </summary>
    private void CreateGrid()
    {
        if (gridSlotPrefab == null)
        {
            Debug.LogError("[CharacterPlacementManager] GridSlot Prefab is not assigned!");
            return;
        }

        Debug.Log($"[CharacterPlacementManager] CreateGrid started - gridRows={gridRows}, gridColumns={gridColumns}, gridParent={gridParent?.name ?? "null"}");

        int slotIndex = 0;

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridColumns; col++)
            {
                // Calculate grid position
                Vector3 position = gridStartPosition + new Vector3(
                    col * gridSpacingX,
                    -row * gridSpacingY,
                    0f
                );

                // Create GridSlot
                GameObject slotObj = Instantiate(gridSlotPrefab, position, Quaternion.identity, gridParent);
                slotObj.name = $"GridSlot_{slotIndex}";

                GridSlot gridSlot = slotObj.GetComponent<GridSlot>();
                if (gridSlot != null)
                {
                    gridSlot.Initialize(slotIndex);
                    gridSlots.Add(gridSlot);
                    Debug.Log($"[CharacterPlacementManager] GridSlot {slotIndex} created at {position}");
                }
                else
                {
                    Debug.LogError($"[CharacterPlacementManager] GridSlot component not found: {slotObj.name}");
                }

                slotIndex++;
            }
        }

        Debug.Log($"[CharacterPlacementManager] {gridSlots.Count} grids created");
    }

    /// <summary>
    /// Place character in random empty slot (called from CardSelectionManager)
    /// </summary>
    public void SpawnCharacterAtRandomSlot(Sprite characterSprite)
    {
        if (characterSprite == null)
        {
            Debug.LogError("[CharacterPlacementManager] characterSprite is null!");
            return;
        }

        // Find empty slots
        List<GridSlot> emptySlots = GetEmptySlots();
        if (emptySlots.Count == 0)
        {
            Debug.LogWarning("[CharacterPlacementManager] No empty slots available!");
            return;
        }

        // Random selection
        int randomIndex = Random.Range(0, emptySlots.Count);
        GridSlot targetSlot = emptySlots[randomIndex];

        // Create character
        GameObject character = Instantiate(characterPrefab, targetSlot.GetWorldPosition(), Quaternion.identity, gridParent);
        character.name = $"Character_{targetSlot.GetSlotIndex()}";

        // Set sprite
        SpriteRenderer spriteRenderer = character.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = characterSprite;
        }

        // Place in slot
        targetSlot.PlaceCharacter(character);

        Debug.Log($"[CharacterPlacementManager] Character placed in slot {targetSlot.GetSlotIndex()} (random)");
    }

    /// <summary>
    /// Get list of empty slots
    /// </summary>
    private List<GridSlot> GetEmptySlots()
    {
        List<GridSlot> emptySlots = new List<GridSlot>();
        foreach (GridSlot slot in gridSlots)
        {
            if (slot.IsEmpty())
            {
                emptySlots.Add(slot);
            }
        }
        return emptySlots;
    }

    /// <summary>
    /// Long press started (2 second hold)
    /// </summary>
    private void HandleLongPressStart(Vector2 screenPosition)
    {
        Debug.Log($"[CharacterPlacementManager] HandleLongPressStart called! screenPosition={screenPosition}");

        // Convert screen coordinates to world coordinates
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        worldPosition.z = 0f;

        Debug.Log($"[CharacterPlacementManager] worldPosition={worldPosition}");

        // Detect character with Raycast2D
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
        Debug.Log($"[CharacterPlacementManager] Raycast hit={hit.collider?.gameObject.name ?? "null"}");

        if (hit.collider != null)
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"[CharacterPlacementManager] Hit object: {hitObject.name}");

            // Check if it's a character
            GridSlot ownerSlot = FindSlotByCharacter(hitObject);
            Debug.Log($"[CharacterPlacementManager] ownerSlot={ownerSlot?.GetSlotIndex().ToString() ?? "null"}");

            if (ownerSlot != null)
            {
                draggingCharacter = hitObject;
                originalSlot = ownerSlot;

                // Show all grids
                ShowAllGrids();

                Debug.Log($"[CharacterPlacementManager] Character drag started: slot {ownerSlot.GetSlotIndex()}");
            }
        }
    }

    /// <summary>
    /// Dragging (position update)
    /// </summary>
    private void HandleDragUpdate(Vector2 screenPosition)
    {
        if (draggingCharacter == null) return;

        // Convert screen coordinates to world coordinates
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        worldPosition.z = 0f;

        // Update character position
        draggingCharacter.transform.position = worldPosition;
    }

    /// <summary>
    /// Drop (finger/mouse released)
    /// </summary>
    private void HandleDrop(Vector2 screenPosition)
    {
        if (draggingCharacter == null) return;

        // Convert screen coordinates to world coordinates
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        worldPosition.z = 0f;

        // Check if there's a GridSlot at drop position
        GridSlot targetSlot = FindSlotAtPosition(worldPosition);

        if (targetSlot != null && targetSlot.IsEmpty())
        {
            // Drop on empty slot: move character
            originalSlot.RemoveCharacter();
            targetSlot.PlaceCharacter(draggingCharacter);

            Debug.Log($"[CharacterPlacementManager] Character moved: slot {originalSlot.GetSlotIndex()} → {targetSlot.GetSlotIndex()}");
        }
        else
        {
            // Not an empty slot: return to original position
            draggingCharacter.transform.position = originalSlot.GetWorldPosition();
            Debug.Log($"[CharacterPlacementManager] Character returned to original position: slot {originalSlot.GetSlotIndex()}");
        }

        // Reset state
        draggingCharacter = null;
        originalSlot = null;

        // Hide all grids
        HideAllGrids();
    }

    /// <summary>
    /// Find GridSlot at specific position
    /// </summary>
    private GridSlot FindSlotAtPosition(Vector3 worldPosition)
    {
        foreach (GridSlot slot in gridSlots)
        {
            float distance = Vector3.Distance(slot.GetWorldPosition(), worldPosition);
            if (distance < gridSpacingX / 2f) // Within half of grid spacing
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// Find slot that contains the character
    /// </summary>
    private GridSlot FindSlotByCharacter(GameObject character)
    {
        foreach (GridSlot slot in gridSlots)
        {
            if (slot.GetCurrentCharacter() == character)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// Show all grids
    /// </summary>
    private void ShowAllGrids()
    {
        foreach (GridSlot slot in gridSlots)
        {
            slot.ShowGrid();
        }
    }

    /// <summary>
    /// Hide all grids
    /// </summary>
    private void HideAllGrids()
    {
        foreach (GridSlot slot in gridSlots)
        {
            slot.HideGrid();
        }
    }

    /// <summary>
    /// Clear all slots
    /// </summary>
    public void ClearAllSlots()
    {
        foreach (GridSlot slot in gridSlots)
        {
            GameObject character = slot.GetCurrentCharacter();
            if (character != null)
            {
                Destroy(character);
            }
            slot.RemoveCharacter();
        }

        Debug.Log("[CharacterPlacementManager] All slots cleared");
    }
}
