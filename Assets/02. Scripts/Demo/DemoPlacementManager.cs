using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Novelian.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Demo
{
    /// <summary>
    /// Demo placement mode
    /// </summary>
    public enum DemoPlacementMode
    {
        Top,    // 1 row, all characters face down (Z-)
        Center  // 2 rows with gap, top row faces up (Z+), bottom row faces down (Z-)
    }

    /// <summary>
    /// Demo-specific character placement manager
    /// Supports switching between Top and Center placement modes
    /// Uses direct prefab instantiation (no Addressables/CSVLoader)
    /// </summary>
    public class DemoPlacementManager : MonoBehaviour
    {
        [Header("Placement Mode")]
        [SerializeField] private DemoPlacementMode currentMode = DemoPlacementMode.Top;

        [Header("Grid Settings")]
        [SerializeField] private GameObject gridSlotPrefab;
        [SerializeField] private Transform gridParent;

        [Header("Grid Layout - Common")]
        [SerializeField] private int gridColumns = 4;
        [SerializeField] private float gridSpacingX = 2f;
        [SerializeField] private float gridSpacingZ = 2f;

        [Header("Grid Layout - Top Mode")]
        [SerializeField] private Vector3 topModeGridCenter = new Vector3(0f, 0f, 25.5f);

        [Header("Grid Layout - Center Mode")]
        [SerializeField] private Vector3 centerModeGridCenter = new Vector3(0f, 0.5f, 25f);
        [SerializeField, Tooltip("Gap between top and bottom rows (for ProtectionObj)")]
        private float rowGap = 5f;

        [Header("Protection Object")]
        [SerializeField] private Transform protectionObj;
        [SerializeField] private Vector3 topModeProtectionPos = new Vector3(0f, 0f, 28f);
        [SerializeField] private Vector3 centerModeProtectionPos = new Vector3(0f, 0f, 25f);

        [Header("Character Prefabs")]
        [SerializeField] private GameObject characterPrefab;

        [Header("Character Settings")]
        [SerializeField, Tooltip("Character ID from CharacterTable CSV")]
        private int defaultCharacterId = 10001;

        [Header("Camera Settings (Cinemachine)")]
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private Vector3 topModeCameraPos = new Vector3(0f, 20f, 0f);
        [SerializeField] private Vector3 topModeCameraRot = new Vector3(60f, 0f, 0f);
        [SerializeField] private Vector3 centerModeCameraPos = new Vector3(0f, 30f, 20f);
        [SerializeField] private Vector3 centerModeCameraRot = new Vector3(90f, 0f, 0f);

        // Grid slots
        private List<GridSlot> gridSlots = new List<GridSlot>();

        // Spawned characters tracking
        private List<GameObject> spawnedCharacters = new List<GameObject>();

        // Camera view state
        private bool isTopDownView = false;

        public static DemoPlacementManager Instance { get; private set; }

        public DemoPlacementMode CurrentMode => currentMode;
        public bool IsReady { get; private set; }
        public bool IsTopDownView => isTopDownView;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            // Wait for CSVLoader to be ready (needed for character skills)
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.Log("[DemoPlacementManager] Waiting for CSVLoader...");
                await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);
                Debug.Log("[DemoPlacementManager] CSVLoader ready!");
            }

            // Auto-find virtual camera if not assigned
            if (virtualCamera == null)
            {
                virtualCamera = FindFirstObjectByType<CinemachineCamera>();
            }

            // Create initial grid
            CreateGrid();

            // Set initial protection position
            UpdateProtectionPosition();

            // Set initial camera position
            ApplyCameraView();

            IsReady = true;
            Debug.Log($"[DemoPlacementManager] Initialized in {currentMode} mode");
        }

        #region Grid Management

        /// <summary>
        /// Create grid based on current placement mode
        /// </summary>
        private void CreateGrid()
        {
            // Clear existing grid
            ClearGrid();

            if (gridSlotPrefab == null)
            {
                Debug.LogError("[DemoPlacementManager] GridSlot Prefab is not assigned!");
                return;
            }

            Vector3 gridCenter = currentMode == DemoPlacementMode.Top ? topModeGridCenter : centerModeGridCenter;
            float totalWidth = (gridColumns - 1) * gridSpacingX;
            int slotIndex = 0;

            if (currentMode == DemoPlacementMode.Top)
            {
                // Top mode: 1 row
                Vector3 startPos = new Vector3(
                    -totalWidth / 2f + gridCenter.x,
                    gridCenter.y,
                    gridCenter.z
                );

                for (int col = 0; col < gridColumns; col++)
                {
                    Vector3 position = startPos + new Vector3(col * gridSpacingX, 0f, 0f);
                    CreateGridSlot(position, slotIndex, 0);
                    slotIndex++;
                }
            }
            else
            {
                // Center mode: 2 rows with gap
                // Top row (faces up/Z+)
                Vector3 topRowStart = new Vector3(
                    -totalWidth / 2f + gridCenter.x,
                    gridCenter.y,
                    gridCenter.z + rowGap / 2f + gridSpacingZ / 2f
                );

                for (int col = 0; col < gridColumns; col++)
                {
                    Vector3 position = topRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                    CreateGridSlot(position, slotIndex, 0); // Row 0 = top row
                    slotIndex++;
                }

                // Bottom row (faces down/Z-)
                Vector3 bottomRowStart = new Vector3(
                    -totalWidth / 2f + gridCenter.x,
                    gridCenter.y,
                    gridCenter.z - rowGap / 2f - gridSpacingZ / 2f
                );

                for (int col = 0; col < gridColumns; col++)
                {
                    Vector3 position = bottomRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                    CreateGridSlot(position, slotIndex, 1); // Row 1 = bottom row
                    slotIndex++;
                }
            }

            Debug.Log($"[DemoPlacementManager] Created {gridSlots.Count} grid slots in {currentMode} mode");
        }

        private void CreateGridSlot(Vector3 position, int index, int rowIndex)
        {
            GameObject slotObj = Instantiate(gridSlotPrefab, position, gridSlotPrefab.transform.rotation, gridParent);
            slotObj.name = $"DemoGridSlot_{index}_Row{rowIndex}";

            GridSlot gridSlot = slotObj.GetComponent<GridSlot>();
            if (gridSlot != null)
            {
                gridSlot.Initialize(index);
                gridSlots.Add(gridSlot);
            }
        }

        private void ClearGrid()
        {
            // Remove all characters first
            ClearAllCharacters();

            // Destroy grid slots
            foreach (var slot in gridSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            gridSlots.Clear();
        }

        #endregion

        #region Character Spawning

        /// <summary>
        /// Spawn character in a random empty slot
        /// </summary>
        public bool SpawnCharacter()
        {
            if (characterPrefab == null)
            {
                Debug.LogError("[DemoPlacementManager] Character prefab not assigned!");
                return false;
            }

            GridSlot targetSlot = GetRandomEmptySlot();
            if (targetSlot == null)
            {
                Debug.LogWarning("[DemoPlacementManager] No empty slots available!");
                return false;
            }

            Vector3 spawnPosition = targetSlot.GetWorldPosition();
            Quaternion rotation = GetCharacterRotation(targetSlot);

            GameObject characterObj = Instantiate(
                characterPrefab,
                spawnPosition,
                rotation,
                gridParent
            );
            characterObj.name = $"DemoCharacter_Slot{targetSlot.GetSlotIndex()}";

            // Initialize character with CSV data
            var character = characterObj.GetComponent<Character>();
            if (character != null)
            {
                character.Initialize(defaultCharacterId);
                Debug.Log($"[DemoPlacementManager] Character initialized with ID: {defaultCharacterId}");
            }
            else
            {
                Debug.LogWarning("[DemoPlacementManager] Character component not found on prefab!");
            }

            targetSlot.PlaceCharacter(characterObj);
            spawnedCharacters.Add(characterObj);

            Debug.Log($"[DemoPlacementManager] Spawned character at slot {targetSlot.GetSlotIndex()}");
            return true;
        }

        /// <summary>
        /// Get character rotation based on slot position and placement mode
        /// </summary>
        private Quaternion GetCharacterRotation(GridSlot slot)
        {
            if (currentMode == DemoPlacementMode.Top)
            {
                // Top mode: all face down (negative Z)
                return Quaternion.Euler(0f, 180f, 0f);
            }
            else
            {
                // Center mode: determine by row
                // Slot indices 0-3 are top row (face up/Z+)
                // Slot indices 4-7 are bottom row (face down/Z-)
                int slotIndex = slot.GetSlotIndex();
                bool isTopRow = slotIndex < gridColumns;

                if (isTopRow)
                {
                    // Top row faces up (positive Z)
                    return Quaternion.Euler(0f, 0f, 0f);
                }
                else
                {
                    // Bottom row faces down (negative Z)
                    return Quaternion.Euler(0f, 180f, 0f);
                }
            }
        }

        private GridSlot GetRandomEmptySlot()
        {
            List<GridSlot> emptySlots = new List<GridSlot>();
            foreach (var slot in gridSlots)
            {
                if (slot.IsEmpty())
                {
                    emptySlots.Add(slot);
                }
            }

            if (emptySlots.Count == 0) return null;
            return emptySlots[Random.Range(0, emptySlots.Count)];
        }

        /// <summary>
        /// Clear all spawned characters
        /// </summary>
        public void ClearAllCharacters()
        {
            foreach (var character in spawnedCharacters)
            {
                if (character != null)
                {
                    Destroy(character);
                }
            }
            spawnedCharacters.Clear();

            foreach (var slot in gridSlots)
            {
                if (slot != null)
                {
                    slot.RemoveCharacter();
                }
            }

            Debug.Log("[DemoPlacementManager] All characters cleared");
        }

        #endregion

        #region Mode Switching

        /// <summary>
        /// Toggle between Top and Center placement modes
        /// </summary>
        public void TogglePlacementMode()
        {
            SetPlacementMode(currentMode == DemoPlacementMode.Top ? DemoPlacementMode.Center : DemoPlacementMode.Top);
        }

        /// <summary>
        /// Set placement mode explicitly
        /// </summary>
        public void SetPlacementMode(DemoPlacementMode mode)
        {
            if (currentMode == mode) return;

            Debug.Log($"[DemoPlacementManager] Switching mode: {currentMode} -> {mode}");

            currentMode = mode;

            // Recreate grid for new mode
            CreateGrid();

            // Update protection object position
            UpdateProtectionPosition();

            Debug.Log($"[DemoPlacementManager] Mode switched to {currentMode}");
        }

        private void UpdateProtectionPosition()
        {
            if (protectionObj == null) return;

            Vector3 newPos = currentMode == DemoPlacementMode.Top ? topModeProtectionPos : centerModeProtectionPos;
            protectionObj.position = newPos;

            Debug.Log($"[DemoPlacementManager] ProtectionObj moved to {newPos}");
        }

        /// <summary>
        /// Toggle camera view between TopDown and Normal view
        /// </summary>
        public void ToggleCameraView()
        {
            isTopDownView = !isTopDownView;
            ApplyCameraView();
            Debug.Log($"[DemoPlacementManager] Camera view toggled to: {(isTopDownView ? "TopDown" : "Normal")}");
        }

        private void ApplyCameraView()
        {
            if (virtualCamera == null) return;

            Vector3 newPos;
            Vector3 newRot;

            if (isTopDownView)
            {
                // TopDown view
                newPos = topModeCameraPos;
                newRot = topModeCameraRot;
            }
            else
            {
                // Normal view
                newPos = centerModeCameraPos;
                newRot = centerModeCameraRot;
            }

            virtualCamera.transform.position = newPos;
            virtualCamera.transform.rotation = Quaternion.Euler(newRot);

            Debug.Log($"[DemoPlacementManager] Virtual Camera moved to {newPos}, rotation {newRot}");
        }

        #endregion

        #region Public Accessors

        public int GetEmptySlotCount()
        {
            int count = 0;
            foreach (var slot in gridSlots)
            {
                if (slot.IsEmpty()) count++;
            }
            return count;
        }

        public int GetTotalSlotCount()
        {
            return gridSlots.Count;
        }

        public int GetSpawnedCharacterCount()
        {
            return spawnedCharacters.Count;
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
