using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LCB: Manages individual player slots
/// Displays the sprite of the selected character
/// </summary>
public class PlayerSlot : MonoBehaviour
{
    [Header("Slot Information")]
    public int slotIndex;             // LCB: Slot number (0-9)
    public bool isOccupied = false;   // LCB: Whether the slot is occupied

    [Header("UI Elements")]
    public Image characterImage;      // LCB: Image to display character image (child Image)
    public Image slotBackgroundImage; // LCB: Slot background image (PlayerSlot's own Image)
    public GameObject emptySlotVisual; // LCB: Empty slot visual (optional)

    [Header("Device Specific Offset")]
    public bool useAspectAdjust = true;    // LCB: Enable/disable per-aspect Y adjustment
    public float ipad34ExtraY = 0.3f;      // LCB: Extra Y offset for 3:4 iPad (world units)

    [Header("Current Character")]
    private GenreType currentGenreType; // LCB: Current slot's genre type
    private GameObject instantiatedCharacter; // LCB: Instantiated character object (physics object)
    private CharacterData currentCharacterData; // LCB: Current character data

    [Header("Position Manager")]
    private DynamicSlotPositionManager positionManager; // Dynamic position manager reference

    void Start()
    {
        // Find DynamicSlotPositionManager
        positionManager = FindFirstObjectByType<DynamicSlotPositionManager>();
        if (positionManager == null)
        {
            Debug.LogWarning($"[PlayerSlot {slotIndex}] DynamicSlotPositionManager not found! Using fallback positioning.");
        }

        // LCB: Auto-assign slotBackgroundImage - PlayerSlot's own Image
        if (slotBackgroundImage == null)
        {
            slotBackgroundImage = GetComponent<Image>();
        }

        // LCB: Find characterImage if not set - child Image only (optional)
        if (characterImage == null)
        {
            // LCB: Find GameObject named "Image" among children
            Transform imageTransform = transform.Find("Image");
            if (imageTransform != null)
            {
                characterImage = imageTransform.GetComponent<Image>();
            }

            // LCB: If not found, search in children (excluding self)
            if (characterImage == null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image img in images)
                {
                    if (img.gameObject != gameObject)
                    {
                        characterImage = img;
                        break;
                    }
                }
            }

            if (characterImage != null)
            {
                Debug.Log($"Slot {slotIndex}: Character Image auto-connected - {characterImage.gameObject.name}");
            }
            // LCB: else: It's okay if characterImage is not found (using prefab method)
        }

        // LCB: Slot background is always kept active
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        // LCB: Child Image GameObject is deactivated at runtime start (only if it exists)
        if (characterImage != null && characterImage.gameObject != gameObject)
        {
            characterImage.gameObject.SetActive(false);
        }

        UpdateSlotVisual();
    }

    /// <summary>
    /// LCB: Place physics object in slot based on character data (new method)
    /// </summary>
    public void AssignCharacterData(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning($"Slot {slotIndex}: CharacterData is null!");
            return;
        }

        if (characterData.characterPrefab == null)
        {
            Debug.LogWarning($"Slot {slotIndex}: characterPrefab is null! CharacterData: {characterData.characterName}");
            return;
        }

        // LCB: Remove existing character object if exists
        if (instantiatedCharacter != null)
        {
            Destroy(instantiatedCharacter);
             instantiatedCharacter = null;
        }

        currentCharacterData = characterData;
        currentGenreType = characterData.genreType;
    
        // LCB: Instantiate character prefab in world space (at slot position)
        Vector3 worldPosition = GetWorldPositionFromSlot();
        instantiatedCharacter = Instantiate(characterData.characterPrefab, worldPosition, Quaternion.identity);
        instantiatedCharacter.name = $"Character_{characterData.characterName}_{slotIndex}";

        // LCB: Set data to Character script
        Character characterScript = instantiatedCharacter.GetComponent<Character>();
        if (characterScript != null)
        {
            characterScript.Initialize(characterData);
            Debug.Log($"Slot {slotIndex} physics object created and initialized: {characterData.characterName} at {worldPosition}");
        }
        else
        {
            Debug.LogWarning($"Slot {slotIndex}: Cannot find Character script! Add Character component to prefab.");
        }
            isOccupied = (instantiatedCharacter != null);
        // LCB: Completely hide UI image (only show physics object)
        if (characterImage != null)
        {
            characterImage.enabled = false;
            characterImage.sprite = null;
            characterImage.gameObject.SetActive(false);
            Debug.Log($"Slot {slotIndex}: UI image deactivation completed");
        }

        // LCB: Slot background is always active
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        gameObject.SetActive(true);
        UpdateSlotVisual();
    }

    /// <summary>
    /// LCB: Convert UI slot position to world coordinates
    /// Uses DynamicSlotPositionManager if available, otherwise fallback to legacy method
    /// </summary>
    private Vector3 GetWorldPositionFromSlot()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            Debug.LogWarning($"Slot {slotIndex}: No RectTransform!");
            return Vector3.zero;
        }

        // Use DynamicSlotPositionManager if available (NEW METHOD)
        if (positionManager != null)
        {
            // Try to get cached position first
            Vector3 cachedPos = positionManager.GetCachedPosition(slotIndex);
            if (cachedPos != Vector3.zero)
            {
                Debug.Log($"[PlayerSlot {slotIndex}] Using cached position: {cachedPos}");
                return ApplyAspectAdjust(cachedPos);    // LCB: Apply device-specific adjust
            }

            // Calculate new position using raycast
            Vector3 calculatedPos = positionManager.CalculateWorldPositionForSlot(rectTransform);

            // Cache for future use
            positionManager.CacheSlotPosition(slotIndex, calculatedPos);

            Debug.Log($"[PlayerSlot {slotIndex}] Calculated position via raycast: {calculatedPos}");
            return ApplyAspectAdjust(calculatedPos);    // LCB: Apply device-specific adjust
        }

        // FALLBACK: Legacy method (if DynamicSlotPositionManager not found)
        Debug.LogWarning($"[PlayerSlot {slotIndex}] Using legacy position calculation");
        return ApplyAspectAdjust(GetWorldPositionFromSlot_Legacy()); // LCB: Apply adjust even for legacy
    }
    // LCB: Adjust world position only for specific aspect ratios (e.g., 3:4 iPad)
    private Vector3 ApplyAspectAdjust(Vector3 worldPos)
    {
        if (!useAspectAdjust)
            return worldPos; // LCB: Skip adjustment if disabled

        float aspect = (float)Screen.width / Screen.height;

        // LCB: 3:4 iPad has aspect ≈ 0.75
        if (Mathf.Abs(aspect - 0.75f) <= 0.02f)
        {
            // LCB: Raise character a bit so it doesn't go too low on this device
            worldPos.y += ipad34ExtraY;
        }

        return worldPos;
    }
    /// <summary>
    /// Legacy position calculation method (fallback)
    /// </summary>
    private Vector3 GetWorldPositionFromSlot_Legacy()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return Vector3.zero;

        // LCB: Check if camera exists
        if (Camera.main == null)
        {
            Debug.LogError("Cannot find main camera!");
            return Vector3.zero;
        }

        // LCB: Get Canvas to check render mode
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"Slot {slotIndex}: Cannot find parent Canvas!");
            return Vector3.zero;
        }

        Vector3 worldPosition;

        // LCB: Convert based on Canvas render mode
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // LCB: For Screen Space - Camera mode, use canvas camera
            Camera canvasCamera = canvas.worldCamera;
            if (canvasCamera == null)
            {
                Debug.LogError($"Slot {slotIndex}: Canvas has no camera assigned!");
                return Vector3.zero;
            }

            // LCB: Get screen position of UI element
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);

            // LCB: Convert screen position to world position at target Z depth
            float targetZ = -7.5f;
            Vector3 worldPosAtZ = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.WorldToScreenPoint(new Vector3(0, 0, targetZ)).z));
            worldPosition = worldPosAtZ;

            Debug.Log($"[PlayerSlot {slotIndex}] Screen Space Camera - Screen: {screenPos}, World: {worldPosition}");
        }
        else
        {
            // LCB: For other modes, use direct position
            worldPosition = rectTransform.position;
            worldPosition.z = -7.5f;
            Debug.Log($"[PlayerSlot {slotIndex}] Direct Position - World: {worldPosition}");
        }

        return worldPosition;
    }

    /// <summary>
    /// LCB: Place character prefab and sprite in slot (legacy - UI method)
    /// </summary>
    public void AssignCharacter(GameObject characterPrefab, Sprite characterSprite, GenreType genreType)
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning($"Slot {slotIndex}: Character prefab is null!");
            return;
        }

        if (characterSprite == null)
        {
            Debug.LogWarning($"Slot {slotIndex}: Character sprite is null!");
            return;
        }

        // LCB: Remove existing character object if exists
        if (instantiatedCharacter != null)
        {
            Destroy(instantiatedCharacter);
        }

        currentGenreType = genreType;
        isOccupied = true;

        // LCB: Instantiate character prefab (create as child of slot)
        instantiatedCharacter = Instantiate(characterPrefab, transform);
        instantiatedCharacter.name = $"Character_{genreType}";

        // LCB: Find Image component in created object
        Image charImage = instantiatedCharacter.GetComponentInChildren<Image>();
        if (charImage != null)
        {
            charImage.sprite = characterSprite;
            charImage.enabled = true;
            charImage.color = new Color(1, 1, 1, 1);
            Debug.Log($"Slot {slotIndex} prefab creation and image setup completed: {characterSprite.name}");
        }
        else
        {
            Debug.LogWarning($"Slot {slotIndex}: Cannot find Image in created prefab!");
        }

        // LCB: Slot background is always active
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        gameObject.SetActive(true);
        UpdateSlotVisual();
    }

    /// <summary>
    /// LCB: Place character sprite in slot (simplified version - legacy compatibility)
    /// </summary>
    public void AssignCharacterSprite(Sprite characterSprite, GenreType genreType)
    {
        if (characterSprite == null)
        {
            Debug.LogWarning("Character sprite is null!");
            return;
        }

        currentGenreType = genreType;
        isOccupied = true;

        // LCB: Slot background is always active and visible
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        // LCB: Set character sprite
        if (characterImage != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.enabled = true;
            characterImage.color = new Color(1, 1, 1, 1); // LCB: Fully opaque

            // LCB: Activate GameObject
            characterImage.gameObject.SetActive(true);

            // LCB: Disable Raycast Target - allow clicks to pass through to slot background
            characterImage.raycastTarget = false;

            // LCB: Check RectTransform
            RectTransform rect = characterImage.GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"Slot {slotIndex} Image size: {rect.rect.width}x{rect.rect.height}");
            }

            Debug.Log($"Slot {slotIndex} genre {genreType} character placed - Image active: {characterImage.gameObject.activeSelf}, enabled: {characterImage.enabled}");
        }

        // LCB: PlayerSlot GameObject itself is always kept active
        gameObject.SetActive(true);

        UpdateSlotVisual();
    }

    /// <summary>
    /// LCB: Clear slot
    /// </summary>
    public void ClearSlot()
    {
       

        // LCB: Remove instantiated character object
        if (instantiatedCharacter != null)
        {
            Destroy(instantiatedCharacter);
            instantiatedCharacter = null;
        }
        currentCharacterData = null;
        currentGenreType = default;

        if (characterImage != null)
        {
            characterImage.sprite = null;
            characterImage.enabled = false;
        }
        isOccupied = false;
        UpdateSlotVisual();
    }

    /// <summary>
    /// LCB: Update slot visual
    /// </summary>
    void UpdateSlotVisual()
    {
        // LCB: Slot background (slotBackgroundImage) is always kept visible
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        // LCB: emptySlotVisual is always kept active (semi-transparent background)
        // LCB: Removed: emptySlotVisual.SetActive(!isOccupied);

        // LCB: Only child Image GameObject is activated/deactivated (PlayerSlot itself is always active)
        if (characterImage != null && characterImage.gameObject != gameObject)
        {
            characterImage.gameObject.SetActive(isOccupied);
        }
    }

    /// <summary>
    /// LCB: Check if slot is empty
    /// </summary>
    public bool IsEmpty()
    {
          bool empty = (instantiatedCharacter == null) && (isOccupied == false);
        Debug.Log($"[PlayerSlot {slotIndex}] IsEmpty? {empty} (instantiatedCharacter null? {instantiatedCharacter == null}, isOccupied={isOccupied})");
        return empty;
    }

    /// <summary>
    /// LCB: Return current genre type
    /// </summary>
    public GenreType GetGenreType()
    {
        return currentGenreType;
    }
}
