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
    [SerializeField, Tooltip("2행 레이아웃에서 행 사이 간격 (ProtectionObj 공간)")]
    private float rowGap = 0f;

    [Header("Layout Settings (Issue #420)")]
    [SerializeField, Tooltip("true: StageManager에서 레이아웃 적용 후 그리드 생성, false: Awake에서 즉시 생성")]
    private bool waitForLayoutPreset = true;

    [Header("Dual Defense Layout (양방향 방어)")]
    [SerializeField, Tooltip("true: 상단/하단 그리드 분리 모드 활성화")]
    private bool splitGridMode = false;
    [SerializeField, Tooltip("분리 모드에서 2번째 그리드 중심 위치")]
    private Vector3 grid2CenterOffset = Vector3.zero;

    // Grid slot list
    private List<GridSlot> gridSlots = new List<GridSlot>();
    private List<GridSlot> gridSlots2 = new List<GridSlot>();  // 2번째 그리드 (양방향 방어)

    // Loaded character prefabs cache
    private Dictionary<string, GameObject> loadedCharacterPrefabs = new Dictionary<string, GameObject>();
    private bool isPreloadComplete = false;

    // Note: 자가 완결형 맵 시스템에서는 GridMarker에서 직접 설정을 읽어옴

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

        // Issue #420: 레이아웃 프리셋 대기 모드가 아닐 때만 Awake에서 그리드 생성
        if (!waitForLayoutPreset)
        {
            CreateGrid();
        }
        else
        {
            Debug.Log("[CharacterPlacementManager] Waiting for layout preset from StageManager...");
        }

        // Preload all character prefabs IMMEDIATELY in Awake
        await PreloadCharacterPrefabs();
    }

    //JML: 모든 캐릭터 프리팹 로드 (Issue #447 - 개별 캐릭터 프리팹 시스템)
    //     CharacterTable → PathTable → "Prefab_" + Addressable_Key 로 프리팹 로드
    private async UniTask PreloadCharacterPrefabs()
    {
        Debug.Log("[CharacterPlacementManager] Preloading all character prefabs...");

        // CSVLoader 초기화 대기
        while (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            Debug.Log("[CharacterPlacementManager] Waiting for CSVLoader to initialize...");
            await UniTask.Delay(100);
        }

        // CharacterTable에서 모든 캐릭터 데이터 조회
        var characterTable = CSVLoader.Instance.GetTable<CharacterData>();
        if (characterTable == null || characterTable.Count == 0)
        {
            Debug.LogError("[CharacterPlacementManager] No character data found in CharacterTable!");
            isPreloadComplete = true;
            return;
        }

        var allCharacterData = characterTable.GetAll();
        int loadedCount = 0;
        int failedCount = 0;

        foreach (var characterData in allCharacterData)
        {
            // PathTable에서 Addressable_Key 조회
            var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
            if (pathData == null)
            {
                Debug.LogWarning($"[CharacterPlacementManager] PathData not found for Path_ID: {characterData.Path_ID}");
                failedCount++;
                continue;
            }

            // "Prefab_" + Addressable_Key 로 프리팹 키 생성
            string prefabKey = $"Prefab_{pathData.Addressable_Key}";

            // 이미 로드된 프리팹은 스킵
            if (loadedCharacterPrefabs.ContainsKey(prefabKey))
            {
                continue;
            }

            try
            {
                GameObject prefab = await Addressables.LoadAssetAsync<GameObject>(prefabKey).Task;
                loadedCharacterPrefabs[prefabKey] = prefab;
                loadedCount++;
                Debug.Log($"[CharacterPlacementManager] Loaded: {prefabKey} (Character_ID: {characterData.Character_ID})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterPlacementManager] Failed to load '{prefabKey}': {e.Message}");
                failedCount++;
            }
        }

        Debug.Log($"[CharacterPlacementManager] Character prefab preload complete. Loaded: {loadedCount}, Failed: {failedCount}");
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

    //JML: Create grid (Issue #420: 2행일 때 rowGap 적용)
    private void CreateGrid()
    {
        if (gridSlotPrefab == null)
        {
            Debug.LogError("[CharacterPlacementManager] GridSlot Prefab is not assigned!");
            return;
        }

        float totalWidth = (gridColumns - 1) * gridSpacingX;
        int slotIndex = 0;

        // 1행 레이아웃: 기존 로직
        if (gridRows == 1)
        {
            Vector3 startPos = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                gridCenterOffset.z
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = startPos + new Vector3(col * gridSpacingX, 0f, 0f);
                CreateGridSlot(position, slotIndex);
                slotIndex++;
            }
        }
        // 2행 레이아웃: rowGap 적용 (위쪽 행과 아래쪽 행 사이에 간격)
        else if (gridRows == 2)
        {
            // 위쪽 행 (Z+ 방향)
            Vector3 topRowStart = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                gridCenterOffset.z + rowGap / 2f + gridSpacingZ / 2f
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = topRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                CreateGridSlot(position, slotIndex);
                slotIndex++;
            }

            // 아래쪽 행 (Z- 방향)
            Vector3 bottomRowStart = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                gridCenterOffset.z - rowGap / 2f - gridSpacingZ / 2f
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = bottomRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                CreateGridSlot(position, slotIndex);
                slotIndex++;
            }
        }
        // 3행 이상: 기존 로직 + rowGap 고려
        else
        {
            float totalDepth = (gridRows - 1) * gridSpacingZ + (gridRows > 1 ? rowGap : 0f);
            Vector3 calculatedStartPosition = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                totalDepth / 2f + gridCenterOffset.z
            );

            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    Vector3 position = calculatedStartPosition + new Vector3(
                        col * gridSpacingX,
                        0f,
                        -row * gridSpacingZ
                    );
                    CreateGridSlot(position, slotIndex);
                    slotIndex++;
                }
            }
        }

        Debug.Log($"[CharacterPlacementManager] {gridSlots.Count} grids created (rows={gridRows}, cols={gridColumns}, rowGap={rowGap})");
    }

    /// <summary>
    /// JML: 그리드 슬롯 생성 헬퍼 메서드 (Issue #420)
    /// </summary>
    private void CreateGridSlot(Vector3 position, int slotIndex)
    {
        GameObject slotObj = Instantiate(gridSlotPrefab, position, gridSlotPrefab.transform.rotation, gridParent);
        slotObj.name = $"GridSlot_{slotIndex}";

        GridSlot gridSlot = slotObj.GetComponent<GridSlot>();
        if (gridSlot != null)
        {
            gridSlot.Initialize(slotIndex);
            gridSlots.Add(gridSlot);
        }
        else
        {
            Debug.LogError($"[CharacterPlacementManager] GridSlot component not found: {slotObj.name}");
        }
    }

    //JML: Place character in random empty slot by CharacterID (called from CardSelectionManager)
    //     Returns true if spawn successful, false if no empty slots
    //     Issue #447: PathTable 조회로 개별 캐릭터 프리팹 로드
    public bool SpawnCharacterById(int characterId)
    {
        // Character_ID → Path_ID → Addressable_Key → "Prefab_" + key
        string prefabKey = GetCharacterPrefabKey(characterId);
        if (string.IsNullOrEmpty(prefabKey))
        {
            Debug.LogError($"[CharacterPlacementManager] Failed to get prefab key for Character_ID: {characterId}");
            return false;
        }

        // Check if prefab is loaded
        if (!loadedCharacterPrefabs.ContainsKey(prefabKey))
        {
            Debug.LogError($"[CharacterPlacementManager] Character prefab '{prefabKey}' (ID: {characterId}) is not loaded!");
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
        Quaternion spawnRotation = GetCharacterRotation(targetSlot);
        Debug.Log($"[CharacterPlacementManager] Spawning character '{prefabKey}' at slot {targetSlot.GetSlotIndex()}, position: {spawnPosition}");

        GameObject characterObj = Instantiate(
            loadedCharacterPrefabs[prefabKey],
            spawnPosition,
            spawnRotation,
            gridParent
        );
        characterObj.name = $"Character_{characterId}_Slot{targetSlot.GetSlotIndex()}";

        // JML: CSV 데이터 기반 초기화 (Issue #320)
        var character = characterObj.GetComponent<Novelian.Combat.Character>();
        if (character != null)
        {
            character.Initialize(characterId);  // CSV에서 스킬/스텟 로드 + 책갈피 적용

            // JML: 전역 스텟 버프 적용 (Issue #349)
            ApplyGlobalBuffsToCharacter(character);
        }

        // Place in slot
        targetSlot.PlaceCharacter(characterObj);

        Debug.Log($"[CharacterPlacementManager] Character ID {characterId} ({prefabKey}) spawned at slot {targetSlot.GetSlotIndex()}");
        return true;
    }

    /// <summary>
    /// JML: Character_ID로 프리팹 키 조회 (Issue #447)
    /// Character_ID → CharacterData.Path_ID → PathData.Addressable_Key → "Prefab_" + key
    /// </summary>
    private string GetCharacterPrefabKey(int characterId)
    {
        if (CSVLoader.Instance == null)
        {
            Debug.LogError("[CharacterPlacementManager] CSVLoader.Instance is null");
            return null;
        }

        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData == null)
        {
            Debug.LogError($"[CharacterPlacementManager] CharacterData not found for ID: {characterId}");
            return null;
        }

        var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
        if (pathData == null)
        {
            Debug.LogError($"[CharacterPlacementManager] PathData not found for Path_ID: {characterData.Path_ID}");
            return null;
        }

        return $"Prefab_{pathData.Addressable_Key}";
    }

    //JML: Check if there are any empty slots available (양방향 방어: 두 그리드 모두 체크)
    public bool HasEmptySlot()
    {
        foreach (GridSlot slot in gridSlots)
        {
            if (slot.IsEmpty())
            {
                return true;
            }
        }

        // 양방향 방어 모드일 때 그리드 2도 체크
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                if (slot.IsEmpty())
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// JML: 첫 번째 빈 슬롯의 인덱스 반환 (Issue #424)
    /// CharacterCardGridManager UI 연동용
    /// </summary>
    /// <returns>빈 슬롯 인덱스 (0~3), 없으면 -1</returns>
    public int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i].IsEmpty())
            {
                return i;
            }
        }
        return -1;
    }

    //JML: Get random empty slot
    private GridSlot GetRandomEmptySlot()
    {
        List<GridSlot> emptySlots = GetEmptySlots();
        if (emptySlots.Count == 0) return null;

        int randomIndex = Random.Range(0, emptySlots.Count);
        return emptySlots[randomIndex];
    }

    //JML: Get list of empty slots (양방향 방어: 두 그리드 모두 포함)
    private List<GridSlot> GetEmptySlots()
    {
        List<GridSlot> emptySlots = new List<GridSlot>();

        // 그리드 1의 빈 슬롯
        foreach (GridSlot slot in gridSlots)
        {
            if (slot.IsEmpty())
            {
                emptySlots.Add(slot);
            }
        }

        // 그리드 2의 빈 슬롯 (양방향 방어 모드일 때)
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                if (slot.IsEmpty())
                {
                    emptySlots.Add(slot);
                }
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

    //JML: Find GridSlot at specific position (양방향 방어: 두 그리드 모두 검색)
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

        // 양방향 방어 모드일 때 그리드 2도 검색
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                float distance = Vector3.Distance(slot.GetWorldPosition(), worldPosition);
                if (distance < gridSpacingX / 2f)
                {
                    return slot;
                }
            }
        }

        return null;
    }

    //JML: Find slot that contains the character (양방향 방어: 두 그리드 모두 검색)
    private GridSlot FindSlotByCharacter(GameObject character)
    {
        foreach (GridSlot slot in gridSlots)
        {
            if (slot.GetCurrentCharacter() == character)
            {
                return slot;
            }
        }

        // 양방향 방어 모드일 때 그리드 2도 검색
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                if (slot.GetCurrentCharacter() == character)
                {
                    return slot;
                }
            }
        }

        return null;
    }

    //JML: Show all grids (양방향 방어: 두 그리드 모두)
    private void ShowAllGrids()
    {
        foreach (GridSlot slot in gridSlots)
        {
            slot.ShowGrid();
        }

        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                slot.ShowGrid();
            }
        }
    }

    //JML: Hide all grids (양방향 방어: 두 그리드 모두)
    private void HideAllGrids()
    {
        foreach (GridSlot slot in gridSlots)
        {
            slot.HideGrid();
        }

        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                slot.HideGrid();
            }
        }
    }

    //JML: Clear all slots (양방향 방어: 두 그리드 모두)
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

        // 양방향 방어 모드일 때 그리드 2도 정리
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                GameObject characterObj = slot.GetCurrentCharacter();
                if (characterObj != null)
                {
                    Destroy(characterObj);
                }
                slot.RemoveCharacter();
            }
        }

        Debug.Log("[CharacterPlacementManager] All slots cleared (characters destroyed)");
    }

    #region Issue #349 - 전역 스텟 버프 시스템

    /// <summary>
    /// JML: 현재 필드에 배치된 모든 캐릭터 조회 (양방향 방어: 두 그리드 모두)
    /// StageManager.ApplyBuffToAllCharacters()에서 호출
    /// </summary>
    public List<Novelian.Combat.Character> GetAllCharacters()
    {
        List<Novelian.Combat.Character> characters = new List<Novelian.Combat.Character>();

        foreach (GridSlot slot in gridSlots)
        {
            if (!slot.IsEmpty())
            {
                GameObject characterObj = slot.GetCurrentCharacter();
                if (characterObj != null)
                {
                    var character = characterObj.GetComponent<Novelian.Combat.Character>();
                    if (character != null)
                    {
                        characters.Add(character);
                    }
                }
            }
        }

        // 양방향 방어 모드일 때 그리드 2도 포함
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                if (!slot.IsEmpty())
                {
                    GameObject characterObj = slot.GetCurrentCharacter();
                    if (characterObj != null)
                    {
                        var character = characterObj.GetComponent<Novelian.Combat.Character>();
                        if (character != null)
                        {
                            characters.Add(character);
                        }
                    }
                }
            }
        }

        return characters;
    }

    /// <summary>
    /// JML: 현재 필드에 배치된 캐릭터 ID 목록 조회 (양방향 방어: 두 그리드 모두)
    /// 카드 풀 로직에서 필드 캐릭터 포함 시 사용
    /// </summary>
    public List<int> GetAllCharacterIds()
    {
        List<int> characterIds = new List<int>();

        foreach (GridSlot slot in gridSlots)
        {
            if (!slot.IsEmpty())
            {
                GameObject characterObj = slot.GetCurrentCharacter();
                if (characterObj != null)
                {
                    // GameObject 이름에서 ID 추출: "Character_{id}_Slot{slotIndex}"
                    string name = characterObj.name;
                    if (name.StartsWith("Character_"))
                    {
                        string[] parts = name.Split('_');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int charId))
                        {
                            characterIds.Add(charId);
                        }
                    }
                }
            }
        }

        // 양방향 방어 모드일 때 그리드 2도 포함
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                if (!slot.IsEmpty())
                {
                    GameObject characterObj = slot.GetCurrentCharacter();
                    if (characterObj != null)
                    {
                        string name = characterObj.name;
                        if (name.StartsWith("Character_"))
                        {
                            string[] parts = name.Split('_');
                            if (parts.Length >= 2 && int.TryParse(parts[1], out int charId))
                            {
                                characterIds.Add(charId);
                            }
                        }
                    }
                }
            }
        }

        return characterIds;
    }

    /// <summary>
    /// JML: 특정 캐릭터 ID를 가진 Character 컴포넌트 조회 (양방향 방어: 두 그리드 모두)
    /// 캐릭터 성급 업그레이드 시 사용
    /// </summary>
    public Novelian.Combat.Character GetCharacterById(int characterId)
    {
        foreach (GridSlot slot in gridSlots)
        {
            if (!slot.IsEmpty())
            {
                GameObject characterObj = slot.GetCurrentCharacter();
                if (characterObj != null && characterObj.name.Contains($"Character_{characterId}_"))
                {
                    return characterObj.GetComponent<Novelian.Combat.Character>();
                }
            }
        }

        // 양방향 방어 모드일 때 그리드 2도 검색
        if (splitGridMode)
        {
            foreach (GridSlot slot in gridSlots2)
            {
                if (!slot.IsEmpty())
                {
                    GameObject characterObj = slot.GetCurrentCharacter();
                    if (characterObj != null && characterObj.name.Contains($"Character_{characterId}_"))
                    {
                        return characterObj.GetComponent<Novelian.Combat.Character>();
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// JML: 새로 소환된 캐릭터에 전역 스텟 버프 적용
    /// StageManager에서 누적된 모든 버프를 한 번에 적용
    /// </summary>
    private void ApplyGlobalBuffsToCharacter(Novelian.Combat.Character character)
    {
        if (character == null) return;

        var stageManager = GameManager.Instance?.Stage;
        if (stageManager == null) return;

        var globalBuffs = stageManager.GetAllGlobalStatBuffs();
        if (globalBuffs == null || globalBuffs.Count == 0) return;

        Debug.Log($"[CharacterPlacementManager] Applying {globalBuffs.Count} global buffs to new character");

        foreach (var buff in globalBuffs)
        {
            character.ApplyStatBuff(buff.Key, buff.Value);
        }
    }

    #endregion

    #region Issue #420 - 레이아웃 프리셋 시스템

    /// <summary>
    /// JML: GridMarker에서 직접 레이아웃 적용 (자가 완결형 맵용)
    /// StageManager.ApplyGridFromMarker()에서 호출
    ///
    /// 단순화된 시스템:
    /// - GridMarker 태그: Grid1 위치 (marker.transform.position)
    /// - Grid2Marker 태그: Grid2 위치 (별도 오브젝트, 있으면 양방향 방어)
    /// </summary>
    public void ApplyLayoutFromMarker(GridMarker marker)
    {
        if (marker == null)
        {
            Debug.LogWarning("[CharacterPlacementManager] GridMarker is null!");
            return;
        }

        Debug.Log($"[CharacterPlacementManager] Applying layout from GridMarker at {marker.transform.position}");

        // 기존 그리드 슬롯 제거
        ClearAllSlots();
        foreach (var slot in gridSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        gridSlots.Clear();

        // 그리드 2도 정리
        ClearGrid2();

        // 새 그리드 설정 적용
        gridRows = marker.gridRows;
        gridColumns = marker.gridColumns;
        gridSpacingX = marker.gridSpacingX;
        gridSpacingZ = marker.gridSpacingZ;
        gridCenterOffset = marker.transform.position; // 마커 위치 = 그리드 중심
        rowGap = marker.rowGap;

        // 그리드 재생성
        CreateGrid();

        // Grid2Marker 태그로 Grid2 위치 찾기 (있으면 양방향 방어)
        GameObject grid2MarkerObj = GameObject.FindWithTag("Grid2Marker");
        if (grid2MarkerObj != null)
        {
            splitGridMode = true;
            grid2CenterOffset = grid2MarkerObj.transform.position; // 프리펩에 배치된 그대로 사용
            CreateGrid2();
            Debug.Log($"[CharacterPlacementManager] Grid 2 created at {grid2CenterOffset} (Grid2Marker found)");
        }
        else
        {
            splitGridMode = false;
        }

        Debug.Log($"[CharacterPlacementManager] Layout applied: {marker.gridRows}x{marker.gridColumns} grid at {marker.transform.position}, splitMode={splitGridMode}");
    }


    /// <summary>
    /// JML: 2번째 그리드 생성 (양방향 방어 - 하단용)
    /// </summary>
    private void CreateGrid2()
    {
        if (gridSlotPrefab == null) return;

        float totalWidth = (gridColumns - 1) * gridSpacingX;
        int slotIndex = gridSlots.Count;  // 1번째 그리드 이후 인덱스

        // 1행 그리드로 생성 (하단)
        Vector3 startPos = new Vector3(
            -totalWidth / 2f + grid2CenterOffset.x,
            grid2CenterOffset.y,
            grid2CenterOffset.z
        );

        for (int col = 0; col < gridColumns; col++)
        {
            Vector3 position = startPos + new Vector3(col * gridSpacingX, 0f, 0f);

            GameObject slotObj = Instantiate(gridSlotPrefab, position, gridSlotPrefab.transform.rotation, gridParent);
            slotObj.name = $"GridSlot_Grid2_{slotIndex}";

            GridSlot gridSlot = slotObj.GetComponent<GridSlot>();
            if (gridSlot != null)
            {
                gridSlot.Initialize(slotIndex);
                gridSlots2.Add(gridSlot);
            }
            slotIndex++;
        }

        Debug.Log($"[CharacterPlacementManager] Grid 2: {gridSlots2.Count} slots created");
    }

    /// <summary>
    /// JML: 2번째 그리드 제거 (양방향 방어 비활성화 시)
    /// </summary>
    private void ClearGrid2()
    {
        foreach (var slot in gridSlots2)
        {
            if (slot != null)
            {
                GameObject characterObj = slot.GetCurrentCharacter();
                if (characterObj != null)
                {
                    Destroy(characterObj);
                }
                Destroy(slot.gameObject);
            }
        }
        gridSlots2.Clear();
    }

    /// <summary>
    /// JML: 슬롯 위치에서 가장 가까운 스포너 방향을 바라보도록 회전값 반환
    /// SpawnArea1, SpawnArea2 태그로 스포너 위치를 찾아서 더 가까운 쪽을 바라봄
    /// </summary>
    private Quaternion GetCharacterRotation(GridSlot slot)
    {
        Vector3 slotPos = slot.GetWorldPosition();

        // 스포너 위치 찾기
        GameObject spawner1 = GameObject.FindWithTag("SpawnArea1");
        GameObject spawner2 = GameObject.FindWithTag("SpawnArea2");

        Vector3? targetDir = null;

        if (spawner1 != null && spawner2 != null)
        {
            // 두 스포너 중 더 가까운 쪽을 바라봄
            float dist1 = Vector3.Distance(slotPos, spawner1.transform.position);
            float dist2 = Vector3.Distance(slotPos, spawner2.transform.position);

            if (dist1 <= dist2)
            {
                targetDir = spawner1.transform.position - slotPos;
            }
            else
            {
                targetDir = spawner2.transform.position - slotPos;
            }
        }
        else if (spawner1 != null)
        {
            targetDir = spawner1.transform.position - slotPos;
        }
        else if (spawner2 != null)
        {
            targetDir = spawner2.transform.position - slotPos;
        }

        // 스포너를 찾았으면 그 방향을 바라봄
        if (targetDir.HasValue && targetDir.Value.sqrMagnitude > 0.01f)
        {
            Vector3 dir = targetDir.Value;
            dir.y = 0; // Y축 무시 (수평 회전만)
            return Quaternion.LookRotation(dir);
        }

        // 스포너를 못 찾으면 기본값 (Z- 방향)
        return Quaternion.Euler(0f, 180f, 0f);
    }

    /// <summary>
    /// JML: 그리드 설정만 변경 (그리드 재생성 없이)
    /// </summary>
    public void SetGridSettings(int rows, int columns, float spacingX, float spacingZ, Vector3 centerOffset)
    {
        gridRows = rows;
        gridColumns = columns;
        gridSpacingX = spacingX;
        gridSpacingZ = spacingZ;
        gridCenterOffset = centerOffset;
    }

    #endregion

    //JML: Draw grid in SceneView using Gizmos (Issue #420: rowGap 적용)
    private void OnDrawGizmos()
    {
        // Draw grid even when not playing (for visualization during setup)
        Color emptyColor = new Color(0f, 1f, 0f, 0.3f);  // Green for empty slots
        Color occupiedColor = new Color(1f, 0f, 0f, 0.5f); // Red for occupied slots
        Color wireColor = new Color(1f, 1f, 0f, 0.8f);   // Yellow wireframe

        float totalWidth = (gridColumns - 1) * gridSpacingX;
        int slotIndex = 0;
        Vector3 cubeSize = new Vector3(gridSpacingX * 0.8f, 0.1f, gridSpacingZ * 0.8f);

        // 1행 레이아웃
        if (gridRows == 1)
        {
            Vector3 startPos = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                gridCenterOffset.z
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = startPos + new Vector3(col * gridSpacingX, 0f, 0f);
                DrawSlotGizmo(position, cubeSize, slotIndex, emptyColor, occupiedColor, wireColor);
                slotIndex++;
            }
        }
        // 2행 레이아웃: rowGap 적용
        else if (gridRows == 2)
        {
            // 위쪽 행 (Z+ 방향)
            Vector3 topRowStart = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                gridCenterOffset.z + rowGap / 2f + gridSpacingZ / 2f
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = topRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                DrawSlotGizmo(position, cubeSize, slotIndex, emptyColor, occupiedColor, wireColor);
                slotIndex++;
            }

            // 아래쪽 행 (Z- 방향)
            Vector3 bottomRowStart = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                gridCenterOffset.z - rowGap / 2f - gridSpacingZ / 2f
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = bottomRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                DrawSlotGizmo(position, cubeSize, slotIndex, emptyColor, occupiedColor, wireColor);
                slotIndex++;
            }
        }
        // 3행 이상
        else
        {
            float totalDepth = (gridRows - 1) * gridSpacingZ + (gridRows > 1 ? rowGap : 0f);
            Vector3 calculatedStartPosition = new Vector3(
                -totalWidth / 2f + gridCenterOffset.x,
                gridCenterOffset.y,
                totalDepth / 2f + gridCenterOffset.z
            );

            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    Vector3 position = calculatedStartPosition + new Vector3(
                        col * gridSpacingX,
                        0f,
                        -row * gridSpacingZ
                    );
                    DrawSlotGizmo(position, cubeSize, slotIndex, emptyColor, occupiedColor, wireColor);
                    slotIndex++;
                }
            }
        }

        // Draw grid boundary
        Gizmos.color = Color.cyan;
        Vector3 gridCenter = gridCenterOffset;
        float boundaryDepth = gridRows == 2 ? (gridSpacingZ + rowGap) : (gridRows * gridSpacingZ);
        Vector3 gridBoundarySize = new Vector3(
            gridColumns * gridSpacingX,
            0.05f,
            boundaryDepth
        );
        Gizmos.DrawWireCube(gridCenter, gridBoundarySize);

        // 양방향 방어 모드: Grid 2 그리기
        if (splitGridMode)
        {
            DrawGrid2Gizmos(cubeSize, emptyColor, wireColor, slotIndex);
        }
    }

    /// <summary>
    /// JML: Grid 2 Gizmo 그리기 (양방향 방어 모드)
    /// </summary>
    private void DrawGrid2Gizmos(Vector3 cubeSize, Color emptyColor, Color wireColor, int startSlotIndex)
    {
        Color grid2Color = new Color(0f, 0.5f, 1f, 0.3f);  // Blue for Grid 2
        Color grid2WireColor = new Color(0f, 0.7f, 1f, 0.8f);   // Light blue wireframe

        float totalWidth = (gridColumns - 1) * gridSpacingX;
        int slotIndex = startSlotIndex;

        // Grid 2 위치 계산
        Vector3 startPos = new Vector3(
            -totalWidth / 2f + grid2CenterOffset.x,
            grid2CenterOffset.y,
            grid2CenterOffset.z
        );

        for (int col = 0; col < gridColumns; col++)
        {
            Vector3 position = startPos + new Vector3(col * gridSpacingX, 0f, 0f);

            // Check if slot is occupied (only in play mode)
            bool isOccupied = false;
            int grid2Index = slotIndex - startSlotIndex;
            if (Application.isPlaying && gridSlots2.Count > grid2Index)
            {
                isOccupied = !gridSlots2[grid2Index].IsEmpty();
            }

            // Set color based on occupancy
            Gizmos.color = isOccupied ? new Color(1f, 0.5f, 0f, 0.5f) : grid2Color;

            // Draw cube for slot
            Gizmos.DrawCube(position, cubeSize);

            // Draw wireframe
            Gizmos.color = grid2WireColor;
            Gizmos.DrawWireCube(position, cubeSize);

            // Draw slot index label in scene view
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * 0.2f, $"G2_{slotIndex}");
            #endif

            slotIndex++;
        }

        // Draw Grid 2 boundary
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.8f);
        Vector3 grid2BoundarySize = new Vector3(
            gridColumns * gridSpacingX,
            0.05f,
            gridSpacingZ
        );
        Gizmos.DrawWireCube(grid2CenterOffset, grid2BoundarySize);
    }

    /// <summary>
    /// JML: Gizmo 슬롯 그리기 헬퍼 메서드 (Issue #420)
    /// </summary>
    private void DrawSlotGizmo(Vector3 position, Vector3 cubeSize, int slotIndex, Color emptyColor, Color occupiedColor, Color wireColor)
    {
        // Check if slot is occupied (only in play mode)
        bool isOccupied = false;
        if (Application.isPlaying && gridSlots.Count > slotIndex)
        {
            isOccupied = !gridSlots[slotIndex].IsEmpty();
        }

        // Set color based on occupancy
        Gizmos.color = isOccupied ? occupiedColor : emptyColor;

        // Draw cube for slot
        Gizmos.DrawCube(position, cubeSize);

        // Draw wireframe
        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(position, cubeSize);

        // Draw slot index label in scene view
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(position + Vector3.up * 0.2f, $"Slot {slotIndex}");
        #endif
    }
}
