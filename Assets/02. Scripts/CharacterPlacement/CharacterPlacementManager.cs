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
            GameLog.LogError("[CharacterPlacementManager] Main camera not found!");
        }

        // 그리드 즉시 생성
        CreateGrid();

        // Preload all character prefabs IMMEDIATELY in Awake
        await PreloadCharacterPrefabs();
    }

    //JML: 모든 캐릭터 프리팹 로드 (Issue #447 - 개별 캐릭터 프리팹 시스템)
    //     CharacterTable → PathTable → "Prefab_" + Addressable_Key 로 프리팹 로드
    private async UniTask PreloadCharacterPrefabs()
    {
        GameLog.Log("[CharacterPlacementManager] Preloading all character prefabs...");

        // CSVLoader 초기화 대기
        while (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
        {
            GameLog.Log("[CharacterPlacementManager] Waiting for CSVLoader to initialize...");
            await UniTask.Delay(100);
        }

        // CharacterTable에서 모든 캐릭터 데이터 조회
        var characterTable = CSVLoader.Instance.GetTable<CharacterData>();
        if (characterTable == null || characterTable.Count == 0)
        {
            GameLog.LogError("[CharacterPlacementManager] No character data found in CharacterTable!");
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
                GameLog.LogWarning($"[CharacterPlacementManager] PathData not found for Path_ID: {characterData.Path_ID}");
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
                GameLog.Log($"[CharacterPlacementManager] Loaded: {prefabKey} (Character_ID: {characterData.Character_ID})");
            }
            catch (System.Exception e)
            {
                GameLog.LogError($"[CharacterPlacementManager] Failed to load '{prefabKey}': {e.Message}");
                failedCount++;
            }
        }

        GameLog.Log($"[CharacterPlacementManager] Character prefab preload complete. Loaded: {loadedCount}, Failed: {failedCount}");
        isPreloadComplete = true;
    }

    //JML: Check if character prefabs are preloaded
    public bool IsPreloadComplete()
    {
        return isPreloadComplete;
    }

    /// <summary>
    /// Issue #476: 그리드 슬롯이 생성되었는지 확인
    /// </summary>
    public bool HasGridSlots()
    {
        return gridSlots != null && gridSlots.Count > 0;
    }

    /// <summary>
    /// Issue #476: 완전 초기화 여부 (프리로드 + 그리드 생성 완료)
    /// </summary>
    public bool IsFullyInitialized => isPreloadComplete && HasGridSlots();

    /// <summary>
    /// Issue #476: 초기화 완료까지 대기 (BossDungeonManager에서 호출)
    /// 프리로드 완료 + 그리드 생성까지 대기
    /// </summary>
    public async UniTask WaitForInitializationAsync()
    {
        GameLog.Log("[CharacterPlacementManager] 초기화 대기 시작...");

        // 1. 프리로드 완료 대기
        while (!isPreloadComplete)
        {
            await UniTask.Yield();
        }
        GameLog.Log("[CharacterPlacementManager] 프리로드 완료");

        // 2. 그리드 생성 (waitForLayoutPreset=true일 때 직접 생성)
        if (!HasGridSlots())
        {
            GameLog.Log("[CharacterPlacementManager] 그리드가 없어서 생성...");
            CreateGrid();
        }
        GameLog.Log($"[CharacterPlacementManager] 초기화 완료: 그리드 {gridSlots.Count}개 슬롯");
    }

    private void OnEnable()
    {
        GameLog.Log("[CharacterPlacementManager] OnEnable called! Starting InputManager event subscription");
        // Subscribe to InputManager events
        InputManager.OnLongPressStart += HandleLongPressStart;
        InputManager.OnDragUpdate += HandleDragUpdate;
        InputManager.OnDrop += HandleDrop;
        GameLog.Log("[CharacterPlacementManager] InputManager event subscription completed");
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
            GameLog.LogError("[CharacterPlacementManager] GridSlot Prefab is not assigned!");
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

        GameLog.Log($"[CharacterPlacementManager] {gridSlots.Count} grids created (rows={gridRows}, cols={gridColumns}, rowGap={rowGap})");
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
            GameLog.LogError($"[CharacterPlacementManager] GridSlot component not found: {slotObj.name}");
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
            GameLog.LogError($"[CharacterPlacementManager] Failed to get prefab key for Character_ID: {characterId}");
            return false;
        }

        // Check if prefab is loaded
        if (!loadedCharacterPrefabs.ContainsKey(prefabKey))
        {
            GameLog.LogError($"[CharacterPlacementManager] Character prefab '{prefabKey}' (ID: {characterId}) is not loaded!");
            return false;
        }

        // Find empty slot
        GridSlot targetSlot = GetRandomEmptySlot();
        if (targetSlot == null)
        {
            GameLog.LogWarning("[CharacterPlacementManager] No empty slots available!");
            return false;
        }

        // Instantiate character prefab
        Vector3 spawnPosition = targetSlot.GetWorldPosition();
        Quaternion spawnRotation = GetCharacterRotation(targetSlot);
        GameLog.Log($"[CharacterPlacementManager] Spawning character '{prefabKey}' at slot {targetSlot.GetSlotIndex()}, position: {spawnPosition}");

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

            // JML: 파티 시너지 버프 적용 (Issue #331)
            ApplySynergyBuffToCharacter(character);
        }

        // Place in slot
        targetSlot.PlaceCharacter(characterObj);

        GameLog.Log($"[CharacterPlacementManager] Character ID {characterId} ({prefabKey}) spawned at slot {targetSlot.GetSlotIndex()}");
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
            GameLog.LogError("[CharacterPlacementManager] CSVLoader.Instance is null");
            return null;
        }

        var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
        if (characterData == null)
        {
            GameLog.LogError($"[CharacterPlacementManager] CharacterData not found for ID: {characterId}");
            return null;
        }

        var pathData = CSVLoader.Instance.GetData<PathData>(characterData.Path_ID);
        if (pathData == null)
        {
            GameLog.LogError($"[CharacterPlacementManager] PathData not found for Path_ID: {characterData.Path_ID}");
            return null;
        }

        return $"Prefab_{pathData.Addressable_Key}";
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
        GameLog.Log($"[CharacterPlacementManager] HandleLongPressStart called! screenPosition={screenPosition}");

        // Convert screen coordinates to world coordinates using raycast to grid plane
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));

        // Raycast to detect character
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            GameLog.Log($"[CharacterPlacementManager] Raycast hit={hitObject.name}");

            // Check if it's a character
            GridSlot ownerSlot = FindSlotByCharacter(hitObject);
            GameLog.Log($"[CharacterPlacementManager] ownerSlot={ownerSlot?.GetSlotIndex().ToString() ?? "null"}");

            if (ownerSlot != null)
            {
                draggingCharacter = hitObject;
                originalSlot = ownerSlot;

                // Show all grids
                ShowAllGrids();

                GameLog.Log($"[CharacterPlacementManager] Character drag started: slot {ownerSlot.GetSlotIndex()}");
            }
        }
        else
        {
            GameLog.Log("[CharacterPlacementManager] Raycast hit nothing");
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

                GameLog.Log($"[CharacterPlacementManager] Character moved: slot {originalSlot.GetSlotIndex()} → {targetSlot.GetSlotIndex()}");
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

                GameLog.Log($"[CharacterPlacementManager] Characters swapped: slot {originalSlot.GetSlotIndex()} ↔ {targetSlot.GetSlotIndex()}");
            }
        }
        else
        {
            // Not a valid slot or same slot: return to original position
            draggingCharacter.transform.position = originalSlot.GetWorldPosition();
            GameLog.Log($"[CharacterPlacementManager] Character returned to original position: slot {originalSlot.GetSlotIndex()}");
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
                Destroy(characterObj);
            }
            slot.RemoveCharacter();
        }
        GameLog.Log("[CharacterPlacementManager] All slots cleared (characters destroyed)");
    }

    #region Issue #349 - 전역 스텟 버프 시스템

    /// <summary>
    /// JML: 현재 필드에 배치된 모든 캐릭터 조회
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

        return characters;
    }

    /// <summary>
    /// JML: 현재 필드에 배치된 캐릭터 ID 목록 조회
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

        return characterIds;
    }

    /// <summary>
    /// JML: 특정 캐릭터 ID를 가진 Character 컴포넌트 조회
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

        GameLog.Log($"[CharacterPlacementManager] Applying {globalBuffs.Count} global buffs to new character");

        foreach (var buff in globalBuffs)
        {
            character.ApplyStatBuff(buff.Key, buff.Value);
        }
    }

    /// <summary>
    /// JML: 새로 소환된 캐릭터에 파티 시너지 버프 적용 (Issue #331)
    /// PartySynergyManager에서 활성 시너지 확인 후 장르 버프 적용
    /// </summary>
    private void ApplySynergyBuffToCharacter(Novelian.Combat.Character character)
    {
        if (character == null) return;

        if (PartySynergyManager.Instance == null)
        {
            GameLog.LogWarning("[CharacterPlacementManager] PartySynergyManager.Instance is null");
            return;
        }

        PartySynergyManager.Instance.ApplySynergyToCharacter(character);
    }

    /// <summary>
    /// JML: 모든 배치된 캐릭터에 파티 시너지 적용 (게임 시작 시 호출)
    /// </summary>
    public void ApplySynergyToAllCharacters()
    {
        if (PartySynergyManager.Instance == null)
        {
            GameLog.LogWarning("[CharacterPlacementManager] PartySynergyManager.Instance is null");
            return;
        }

        var characters = GetAllCharacters();
        if (characters.Count == 0)
        {
            GameLog.Log("[CharacterPlacementManager] 배치된 캐릭터가 없습니다.");
            return;
        }

        PartySynergyManager.Instance.ApplySynergyToCharacters(characters);
    }

    #endregion

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

        // 스포너를 못 찾으면 기본값 (Z+ 방향, 몬스터 스폰 방향)
        return Quaternion.identity;
    }

    //JML: Draw grid in SceneView using Gizmos
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
    }

    /// <summary>
    /// JML: Gizmo 슬롯 그리기 헬퍼 메서드
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
