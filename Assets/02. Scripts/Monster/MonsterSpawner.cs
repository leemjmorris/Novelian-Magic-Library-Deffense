using UnityEngine;

namespace NovelianMagicLibraryDefense.Spawners
{
    /// <summary>
    /// 몬스터 스폰 위치를 정의하는 컴포넌트
    /// Scene에서 Transform 위치와 BoxCollider 크기로 스폰 영역을 설정
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("Spawn Area Settings")]
        [Tooltip("스폰 영역 크기 (BoxCollider 없이 직접 설정)")]
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 1f, 2f);

        [Header("Ground Detection")]
        [Tooltip("지면 레이어 마스크 (기본: 모든 레이어)")]
        [SerializeField] private LayerMask groundLayer = ~0; // Everything
        [Tooltip("지면 검출 레이캐스트 높이")]
        [SerializeField] private float raycastHeight = 50f;
        [Tooltip("스폰 높이 오프셋 (지면 위로 띄움)")]
        [SerializeField] private float spawnHeightOffset = 0.5f;

        [Header("Spawn Collision Check")]
        [Tooltip("스폰 시 충돌 체크 반경")]
        [SerializeField] private float spawnCheckRadius = 1.0f;
        [Tooltip("몬스터 레이어 (충돌 체크용)")]
        [SerializeField] private LayerMask monsterLayer;

        [Header("Gizmo Settings")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);
        [SerializeField] private Color wireColor = Color.yellow;

        /// <summary>
        /// 스폰 영역 내 랜덤 위치 반환 (Raycast로 지면 보정)
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = transform.position;
            Vector3 halfSize = spawnAreaSize / 2f;

            float randomX = Random.Range(center.x - halfSize.x, center.x + halfSize.x);
            float randomZ = Random.Range(center.z - halfSize.z, center.z + halfSize.z);

            // 위에서 아래로 Raycast하여 지면 찾기
            Vector3 rayOrigin = new Vector3(randomX, center.y + raycastHeight, randomZ);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
            {
                // 지면 위에 오프셋 추가하여 스폰 (지면에 박히는 현상 방지)
                Vector3 spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                return spawnPos;
            }

            // Raycast 실패 시 스폰 영역 중심 Y 사용 + 오프셋
            GameLog.LogWarning($"[MonsterSpawner] 지면을 찾을 수 없음: ({randomX}, {randomZ})");
            return new Vector3(randomX, center.y + spawnHeightOffset, randomZ);
        }

        /// <summary>
        /// 스폰 영역 중심 위치 반환
        /// </summary>
        public Vector3 GetCenter()
        {
            return transform.position;
        }

        /// <summary>
        /// 스폰 영역 크기 반환
        /// </summary>
        public Vector3 GetSize()
        {
            return spawnAreaSize;
        }

        /// <summary>
        /// 빈 공간에 스폰 위치 찾기 (다른 몬스터와 겹치지 않는 위치)
        /// </summary>
        /// <param name="position">찾은 빈 공간 위치</param>
        /// <param name="maxAttempts">최대 시도 횟수</param>
        /// <returns>빈 공간을 찾았으면 true</returns>
        public bool TryGetEmptySpawnPosition(out Vector3 position, int maxAttempts = 10)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 candidate = GetRandomSpawnPosition();

                // 해당 위치에 다른 몬스터가 없는지 확인
                if (!Physics.CheckSphere(candidate, spawnCheckRadius, monsterLayer))
                {
                    position = candidate;
                    return true;
                }
            }

            // 빈 공간을 찾지 못함
            position = Vector3.zero;
            return false;
        }

        /// <summary>
        /// 스폰 체크 반경 반환
        /// </summary>
        public float GetSpawnCheckRadius()
        {
            return spawnCheckRadius;
        }

        private void OnDrawGizmos()
        {
            // 반투명 박스 그리기
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(transform.position, spawnAreaSize);

            // 와이어프레임 그리기
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(transform.position, spawnAreaSize);
        }

        private void OnDrawGizmosSelected()
        {
            // 선택 시 강조
            Gizmos.color = new Color(wireColor.r, wireColor.g, wireColor.b, 0.8f);
            Gizmos.DrawWireCube(transform.position, spawnAreaSize);

#if UNITY_EDITOR
            // 라벨 표시
            Vector3 labelPos = transform.position + Vector3.up * (spawnAreaSize.y / 2 + 0.5f);
            UnityEditor.Handles.Label(labelPos,
                $"Monster Spawner\nSize: {spawnAreaSize.x:F1} x {spawnAreaSize.y:F1} x {spawnAreaSize.z:F1}");
#endif
        }
    }
}
