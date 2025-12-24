using UnityEngine;

/// <summary>
/// 상태 효과 VFX 설정을 중앙 관리하는 ScriptableObject
/// CC(스턴), DOT(도트 데미지) 상태 효과에 표시되는 VFX 프리팹 설정
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectVFXConfig", menuName = "Game/Status Effect VFX Config")]
public class StatusEffectVFXConfig : ScriptableObject
{
    [Header("CC 효과 (스턴)")]
    [Tooltip("스턴(기절) 효과 시 몬스터 머리 위에 표시되는 VFX 프리팹")]
    [SerializeField] private GameObject stunVFXPrefab;

    [Header("DOT 효과 (지속 데미지)")]
    [Tooltip("DOT 효과 시 몬스터 머리 위에 표시되는 VFX 프리팹")]
    [SerializeField] private GameObject dotVFXPrefab;

    [Header("위치 설정")]
    [Tooltip("Collider 상단으로부터의 추가 높이 오프셋 (기본값: 0.3)")]
    [SerializeField] private float heightOffset = 0.3f;

    #region Public Getters

    /// <summary>스턴 VFX 프리팹 반환</summary>
    public GameObject GetStunVFX() => stunVFXPrefab;

    /// <summary>DOT VFX 프리팹 반환</summary>
    public GameObject GetDOTVFX() => dotVFXPrefab;

    /// <summary>높이 오프셋 반환</summary>
    public float GetHeightOffset() => heightOffset;

    #endregion

    #region Helper Methods

    /// <summary>
    /// 몬스터 Collider 기준으로 VFX 스폰 위치 계산
    /// Collider 상단(bounds.max.y) + heightOffset
    /// </summary>
    /// <param name="monsterCollider">몬스터의 Collider</param>
    /// <param name="monsterTransform">몬스터의 Transform</param>
    /// <returns>VFX 스폰 위치</returns>
    public Vector3 CalculateVFXPosition(Collider monsterCollider, Transform monsterTransform)
    {
        if (monsterCollider != null)
        {
            // Collider 상단 위치 + 오프셋
            float topY = monsterCollider.bounds.max.y;
            Vector3 center = monsterCollider.bounds.center;
            return new Vector3(center.x, topY + heightOffset, center.z);
        }
        else
        {
            // Collider가 없으면 Transform 기준으로 대략적인 위치 계산
            return monsterTransform.position + Vector3.up * (2f + heightOffset);
        }
    }

    #endregion
}
