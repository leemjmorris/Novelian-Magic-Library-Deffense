using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;

//JML: World coordinate-based character placement system manager
//     - Creates and manages 4x1 grid (4 slots in a single row)
//     - Places characters in random slots
//     - Moves characters via drag and drop
public class CharacterPlacementManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private GameObject gridSlotPrefab;  // GridSlot prefab
    [SerializeField] private Transform gridParent;       // Grid parent object

    [Header("Grid Layout")]
    [SerializeField] private int gridColumns = 4;        // 4 columns (changed from 5)
    [SerializeField] private int gridRows = 1;           // 1 row (changed from 2)
    [SerializeField] private float gridSpacingX = 1.5f;  // X spacing
    [SerializeField] private float gridSpacingZ = 1.5f;  // Z spacing
    [SerializeField, Tooltip("그리드 중앙 위치 오프셋 (그리드 전체를 이동시킬 때 사용)")]
    private Vector3 gridCenterOffset = Vector3.zero;    // 그리드 중앙 위치 조절용

    // Grid slot list
    private List<GridSlot> gridSlots = new List<GridSlot>();

    // Loaded character prefabs cache
    private Dictionary<string, GameObject> loadedCharacterPrefabs = new Dictionary<string, GameObject>();
    private bool isPreloadComplete = false;

    // Drag state management
    private GameObject draggingCharacter;
    private GridSlot originalSlot;
    private Camera mainCamera;

    private async void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[CharacterPlacementManager] Main camera not found!");
        }

        // Create grid
        CreateGrid();

        // Preload all character prefabs IMMEDIATELY in Awake
        await PreloadCharacterPrefabs();
    }

    //JML: 범용 프리팹 1개 로드 (Issue #320)
    private async UniTask PreloadCharacterPrefabs()
    {
        Debug.Log("[CharacterPlacementManager] Preloading character prefab...");

        string characterKey = "Character";  // 단일 프리팹 키

        try
        {
            GameObject prefab = await Addressables.LoadAssetAsync<GameObject>(characterKey).Task;
            loadedCharacterPrefabs[characterKey] = prefab;
            Debug.Log($"[CharacterPlacementManager] Loaded character prefab: {characterKey}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterPlacementManager] Failed to load '{characterKey}': {e.Message}");
        }

        Debug.Log("[CharacterPlacementManager] Character prefab preload complete");
        isPreloadComplete = true;
    }

    //JML: Check if character prefabs are preloaded
    public bool IsPreloadComplete()
    {
        return isPreloadComplete;
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

    //JML: Create 4x1 grid (그리드 중앙이 원점이 되도록 자동 계산)
    private void CreateGrid()
    {
        if (gridSlotPrefab == null)
        {
            Debug.LogError("[CharacterPlacementManager] GridSlot Prefab is not assigned!");
            return;
        }

        // 그리드 중앙이 gridCenterOffset 위치가 되도록 시작 위치 자동 계산
        // 전체 너비 = (columns - 1) * spacingX
        // 시작 X = -전체너비 / 2 + offset.x
        float totalWidth = (gridColumns - 1) * gridSpacingX;
        float totalDepth = (gridRows - 1) * gridSpacingZ;
        Vector3 calculatedStartPosition = new Vector3(
            -totalWidth / 2f + gridCenterOffset.x,
            gridCenterOffset.y,
            totalDepth / 2f + gridCenterOffset.z  // Z는 음수 방향으로 증가하므로 양수로 시작
        );

        Debug.Log($"[CharacterPlacementManager] CreateGrid started - gridRows={gridRows}, gridColumns={gridColumns}, centerOffset={gridCenterOffset}, calculatedStart={calculatedStartPosition}");

        int slotIndex = 0;

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridColumns; col++)
            {
                // Calculate grid position (XZ plane with Y height offset)
                Vector3 position = calculatedStartPosition + new Vector3(
                    col * gridSpacingX,
                    0f,
                    -row * gridSpacingZ
                );

                // Create GridSlot - preserve prefab's rotation (X=90 for ground-aligned quad)
                GameObject slotObj = Instantiate(gridSlotPrefab, position, gridSlotPrefab.transform.rotation, gridParent);
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

    //JML: Place character in random empty slot by CharacterID (called from CardSelectionManager)
    //     Returns true if spawn successful, false if no empty slots
    public bool SpawnCharacterById(int characterId)
    {
        string characterKey = AddressableKey.GetCharacterKey(characterId);

        // Check if prefab is loaded
        if (!loadedCharacterPrefabs.ContainsKey(characterKey))
        {
            Debug.LogError($"[CharacterPlacementManager] Character prefab '{characterKey}' (ID: {characterId}) is not loaded!");
            return false;
        }

        // Find empty slot
        GridSlot targetSlot = GetRandomEmptySlot();
        if (targetSlot == null)
        {
            Debug.LogWarning("[CharacterPlacementManager] No empty slots available!");
            return false;
        }

        // Instantiate character prefab
        Vector3 spawnPosition = targetSlot.GetWorldPosition();
        Debug.Log($"[CharacterPlacementManager] Spawning character at slot {targetSlot.GetSlotIndex()}, position: {spawnPosition}");

        GameObject characterObj = Instantiate(
            loadedCharacterPrefabs[characterKey],
            spawnPosition,
            Quaternion.identity,
            gridParent
        );
        characterObj.name = $"Character_{characterId}_Slot{targetSlot.GetSlotIndex()}";

        // JML: CSV 데이터 기반 초기화 (Issue #320)
        var character = characterObj.GetComponent<Novelian.Combat.Character>();
        if (character != null)
        {
            character.Initialize(characterId);  // CSV에서 스킬/스텟 로드 + 책갈피 적용
        }

        // Place in slot
        targetSlot.PlaceCharacter(characterObj);

        Debug.Log($"[CharacterPlacementManager] Character ID {characterId} spawned at slot {targetSlot.GetSlotIndex()}");
        return true;
    }

    //JML: Check if there are any empty slots available
    public bool HasEmptySlot()
    {
        foreach (GridSlot slot in gridSlots)
        {
            if (slot.IsEmpty())
            {
                return true;
            }
        }
        return false;
    }

    //JML: Get random empty slot
    private GridSlot GetRandomEmptySlot()
    {
        List<GridSlot> emptySlots = GetEmptySlots();
        if (emptySlots.Count == 0) return null;

        int randomIndex = Random.Range(0, emptySlots.Count);
        return emptySlots[randomIndex];
    }

    //JML: Get list of empty slots
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

    //JML: Long press started (2 second hold)
    private void HandleLongPressStart(Vector2 screenPosition)
    {
        Debug.Log($"[CharacterPlacementManager] HandleLongPressStart called! screenPosition={screenPosition}");

        // Convert screen coordinates to world coordinates using raycast to grid plane
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));

        // Raycast to detect character
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"[CharacterPlacementManager] Raycast hit={hitObject.name}");

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
        else
        {
            Debug.Log("[CharacterPlacementManager] Raycast hit nothing");
        }
    }

    //JML: Dragging (position update)
    private void HandleDragUpdate(Vector2 screenPosition)
    {
        if (draggingCharacter == null) return;

        // Convert screen coordinates to world position on the grid plane (Y = gridCenterOffset.y)
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0f, gridCenterOffset.y, 0f));

        if (gridPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);
            // Keep the character at grid height
            worldPosition.y = gridCenterOffset.y;
            draggingCharacter.transform.position = worldPosition;
        }
    }

    //JML: Drop (finger/mouse released)
    private void HandleDrop(Vector2 screenPosition)
    {
        if (draggingCharacter == null) return;

        // Convert screen coordinates to world position on the grid plane
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0f, gridCenterOffset.y, 0f));
        Vector3 worldPosition = Vector3.zero;

        if (gridPlane.Raycast(ray, out float distance))
        {
            worldPosition = ray.GetPoint(distance);
        }

        // Check if there's a GridSlot at drop position
        GridSlot targetSlot = FindSlotAtPosition(worldPosition);

        if (targetSlot != null && targetSlot != originalSlot)
        {
            if (targetSlot.IsEmpty())
            {
                // Drop on empty slot: move character
                originalSlot.RemoveCharacter();
                targetSlot.PlaceCharacter(draggingCharacter);

                Debug.Log($"[CharacterPlacementManager] Character moved: slot {originalSlot.GetSlotIndex()} → {targetSlot.GetSlotIndex()}");
            }
            else
            {
                // Drop on occupied slot: swap characters
                GameObject targetCharacter = targetSlot.GetCurrentCharacter();

                // Remove both characters from slots
                originalSlot.RemoveCharacter();
                targetSlot.RemoveCharacter();

                // Swap positions
                targetSlot.PlaceCharacter(draggingCharacter);
                originalSlot.PlaceCharacter(targetCharacter);

                Debug.Log($"[CharacterPlacementManager] Characters swapped: slot {originalSlot.GetSlotIndex()} ↔ {targetSlot.GetSlotIndex()}");
            }
        }
        else
        {
            // Not a valid slot or same slot: return to original position
            draggingCharacter.transform.position = originalSlot.GetWorldPosition();
            Debug.Log($"[CharacterPlacementManager] Character returned to original position: slot {originalSlot.GetSlotIndex()}");
        }

        // Reset state
        draggingCharacter = null;
        originalSlot = null;

        // Hide all grids
        HideAllGrids();
    }

    //JML: Find GridSlot at specific position
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

    //JML: Find slot that contains the character
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

    //JML: Show all grids
    private void ShowAllGrids()
    {
        foreach (GridSlot slot in gridSlots)
        {
            slot.ShowGrid();
        }
    }

    //JML: Hide all grids
    private void HideAllGrids()
    {
        foreach (GridSlot slot in gridSlots)
        {
            slot.HideGrid();
        }
    }

    //JML: Clear all slots
    public void ClearAllSlots()
    {
        foreach (GridSlot slot in gridSlots)
        {
            GameObject characterObj = slot.GetCurrentCharacter();
            if (characterObj != null)
            {
                // Destroy instantiated character
                Destroy(characterObj);
            }
            slot.RemoveCharacter();
        }

        Debug.Log("[CharacterPlacementManager] All slots cleared (characters destroyed)");
    }

    //JML: Draw grid in SceneView using Gizmos
    private void OnDrawGizmos()
    {
        // Draw grid even when not playing (for visualization during setup)
        Color emptyColor = new Color(0f, 1f, 0f, 0.3f);  // Green for empty slots
        Color occupiedColor = new Color(1f, 0f, 0f, 0.5f); // Red for occupied slots
        Color wireColor = new Color(1f, 1f, 0f, 0.8f);   // Yellow wireframe

        // 그리드 중앙이 gridCenterOffset 위치가 되도록 시작 위치 계산 (CreateGrid와 동일한 로직)
        float totalWidth = (gridColumns - 1) * gridSpacingX;
        float totalDepth = (gridRows - 1) * gridSpacingZ;
        Vector3 calculatedStartPosition = new Vector3(
            -totalWidth / 2f + gridCenterOffset.x,
            gridCenterOffset.y,
            totalDepth / 2f + gridCenterOffset.z
        );

        // Draw each grid slot
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridColumns; col++)
            {
                // Calculate grid position (same as CreateGrid)
                Vector3 position = calculatedStartPosition + new Vector3(
                    col * gridSpacingX,
                    0f,
                    -row * gridSpacingZ
                );

                // Check if slot is occupied (only in play mode)
                bool isOccupied = false;
                if (Application.isPlaying && gridSlots.Count > 0)
                {
                    int slotIndex = row * gridColumns + col;
                    if (slotIndex < gridSlots.Count)
                    {
                        isOccupied = !gridSlots[slotIndex].IsEmpty();
                    }
                }

                // Set color based on occupancy
                Gizmos.color = isOccupied ? occupiedColor : emptyColor;

                // Draw cube for slot
                Vector3 cubeSize = new Vector3(gridSpacingX * 0.8f, 0.1f, gridSpacingZ * 0.8f);
                Gizmos.DrawCube(position, cubeSize);

                // Draw wireframe
                Gizmos.color = wireColor;
                Gizmos.DrawWireCube(position, cubeSize);

                // Draw slot index label in scene view
                #if UNITY_EDITOR
                int index = row * gridColumns + col;
                UnityEditor.Handles.Label(position + Vector3.up * 0.2f, $"Slot {index}");
                #endif
            }
        }

        // Draw grid boundary (중앙이 gridCenterOffset)
        Gizmos.color = Color.cyan;
        Vector3 gridCenter = gridCenterOffset;  // 오프셋 위치가 중앙
        Vector3 gridBoundarySize = new Vector3(
            gridColumns * gridSpacingX,
            0.05f,
            gridRows * gridSpacingZ
        );
        Gizmos.DrawWireCube(gridCenter, gridBoundarySize);
    }
}
