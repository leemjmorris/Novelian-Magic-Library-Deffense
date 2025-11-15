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

    [Header("Current Character")]
    private GenreType currentGenreType; // LCB: Current slot's genre type
    private GameObject instantiatedCharacter; // LCB: Instantiated character object (physics object)
    private CharacterData currentCharacterData; // LCB: Current character data

    void Start()
    {
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
        }

        currentCharacterData = characterData;
        currentGenreType = characterData.genreType;
        isOccupied = true;

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
    /// </summary>
    private Vector3 GetWorldPositionFromSlot()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            Debug.LogWarning($"Slot {slotIndex}: No RectTransform!");
            return Vector3.zero;
        }

        // LCB: Convert UI position to world coordinates
        Vector3 screenPoint = rectTransform.position;

        // LCB: Z plane where game object will be placed (same value as Character.cs)
        float targetZ = -7.5f;

        // LCB: Calculate distance from camera
        if (Camera.main == null)
        {
            Debug.LogError("Cannot find main camera!");
            return Vector3.zero;
        }

        float distanceFromCamera = targetZ - Camera.main.transform.position.z;

        // LCB: Convert to world coordinates
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, distanceFromCamera));
        worldPoint.z = targetZ;

        Debug.Log($"[PlayerSlot {slotIndex}] Screen: {screenPoint} -> World: {worldPoint}");

        return worldPoint;
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
        isOccupied = false;

        // LCB: Remove instantiated character object
        if (instantiatedCharacter != null)
        {
            Destroy(instantiatedCharacter);
            instantiatedCharacter = null;
        }

        if (characterImage != null)
        {
            characterImage.sprite = null;
            characterImage.enabled = false;
        }

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
        return !isOccupied;
    }

    /// <summary>
    /// LCB: Return current genre type
    /// </summary>
    public GenreType GetGenreType()
    {
        return currentGenreType;
    }
}
