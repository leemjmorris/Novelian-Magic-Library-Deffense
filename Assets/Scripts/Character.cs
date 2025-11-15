using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Character Data")]
    private CharacterData characterData;  // 캐릭터 데이터
    [SerializeField] private SpriteRenderer spriteRenderer;  // 스프라이트 렌더러

    [Header("Prefab References")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Targeting")]
    [SerializeField] private Transform target;

    [Header("Character Attributes")]
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackRange = 1000.0f;

    [Header("Physics")]
    [SerializeField] private bool enablePhysics = true;  // 물리 충돌 활성화 여부
    [SerializeField] private Vector2 colliderSize = new Vector2(1f, 1f);  // Collider 크기

    private ITargetable currentTarget;
    private float timer = 0.0f;
    private BoxCollider2D boxCollider;

    /// <summary>
    /// CharacterData를 받아서 캐릭터 초기화
    /// </summary>
    public void Initialize(CharacterData data)
    {
        characterData = data;

        // SpriteRenderer 자동 설정
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        // 스프라이트 설정
        if (characterData != null && characterData.characterSprite != null)
        {
            spriteRenderer.sprite = characterData.characterSprite;
            spriteRenderer.sortingLayerName = "Default"; // LCB: Use Default sorting layer
            spriteRenderer.sortingOrder = 1000; // LCB: Very high order to render above UI Canvas
            Debug.Log($"[Character] {characterData.characterName} 초기화 완료 - Sorting Order: {spriteRenderer.sortingOrder}");
        }
        else
        {
            Debug.LogWarning("[Character] CharacterData 또는 Sprite가 null입니다!");
        }
    }

    private async void Start()
    {
        // 물리 충돌 설정
        SetupPhysics();

        // LMJ: Skip initialization if Pool manager doesn't exist (e.g., in LobbyScene)
        if (GameManager.Instance == null || GameManager.Instance.Pool == null)
        {
            Debug.Log("[Character] Pool manager not available, skipping projectile pool initialization");
            return;
        }

        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        await GameManager.Instance.Pool.CreatePoolAsync<Projectile>(AddressableKey.Projectile, defaultCapacity: 5, maxSize: 20);
        GameManager.Instance.Pool.WarmUp<Projectile>(20);
    }

    /// <summary>
    /// 물리 충돌 설정 - BoxCollider2D 자동 추가
    /// </summary>
    private void SetupPhysics()
    {
        if (!enablePhysics)
        {
            Debug.Log($"[Character] {gameObject.name}: 물리 충돌 비활성화됨");
            return;
        }

        // BoxCollider2D가 없다면 추가
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.size = colliderSize;
            boxCollider.isTrigger = false;  // 물리 충돌 활성화 (벽처럼 동작)
            Debug.Log($"[Character] {gameObject.name}: BoxCollider2D 추가됨 (size: {colliderSize})");
        }
        else
        {
            Debug.Log($"[Character] {gameObject.name}: 기존 BoxCollider2D 사용");
        }

        // Rigidbody2D 추가 (물리 엔진에서 인식되도록)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;  // Kinematic: 스크립트로 제어, 물리 충돌은 감지
            rb.gravityScale = 0f;  // 중력 비활성화
            Debug.Log($"[Character] {gameObject.name}: Rigidbody2D 추가됨 (Kinematic)");
        }
    }
    private void Update()
    {
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            currentTarget = TargetRegistry.Instance.FindTarget(transform.position, attackRange);
        }

        if (currentTarget != null)
        {
            timer += Time.deltaTime;
            if (timer >= attackInterval)
            {
                Attack(currentTarget);
                timer = 0.0f;
            }
        }
    }

    private void Attack(ITargetable target)
    {
        // 현재 캐릭터의 월드 좌표에서 발사
        Vector3 spawnPosition = transform.position;

        Projectile projectile = GameManager.Instance.Pool.Spawn<Projectile>(spawnPosition);
        projectile.SetTarget(target.GetTransform());

        //Debug.Log($"[Character] {characterData?.characterName ?? "Unknown"} 공격! 발사 위치: {spawnPosition}");
    }
}
