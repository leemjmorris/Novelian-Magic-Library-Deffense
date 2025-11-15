using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Character Obj")]
    [SerializeField] private GameObject characterObj;
    [Header("Prefab References")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Targeting")]
    [SerializeField] private Transform target;

    [Header("Character Attributes")]
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackRange = 1000.0f;  // UI-based character has infinite range

    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Image characterImage;

    private ITargetable currentTarget;
    private float timer = 0.0f;
    private async void Start()
    {
        // LMJ: Skip initialization if Pool manager doesn't exist (e.g., in LobbyScene)
        if (GameManager.Instance == null || GameManager.Instance.Pool == null)
        {
            // Debug.Log("[Character] Pool manager not available, skipping projectile pool initialization");
            return;
        }

        // LMJ: Only create pool if it doesn't exist yet
        if (!GameManager.Instance.Pool.HasPool<Projectile>())
        {
            await GameManager.Instance.Pool.CreatePoolAsync<Projectile>(AddressableKey.Projectile, defaultCapacity: 5, maxSize: 20);
            GameManager.Instance.Pool.WarmUp<Projectile>(20);
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
        if (!characterObj.activeSelf)
            return;
        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        Vector3 spawnPosition = GetWorldPositionFromUI(transform as RectTransform);
        GameManager.Instance.Pool.Spawn<Projectile>(spawnPosition).SetTarget(target.GetTransform());
    }
    
    private Vector3 GetWorldPositionFromUI(RectTransform rectTransform)
    {
        // Screen Space Overlay Canvas: RectTransform.position은 스크린 좌표
        Vector3 screenPoint = rectTransform.position;

        // 게임 오브젝트가 있는 z 평면 (-7.5)
        float targetZ = -7.5f;

        // 카메라로부터의 거리 계산
        float distanceFromCamera = targetZ - Camera.main.transform.position.z;  // -7.5 - (-10) = 2.5

        // ScreenToWorldPoint를 사용하여 World 좌표로 변환
        // z 파라미터는 카메라로부터의 거리(depth)
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, distanceFromCamera));

        // z 좌표를 정확히 게임 오브젝트 평면으로 설정
        worldPoint.z = targetZ;

        // Debug.Log($"[Character] Screen: {screenPoint}, Distance: {distanceFromCamera}, World: {worldPoint}, Camera: {Camera.main.transform.position}");

        return worldPoint;
    }
}
