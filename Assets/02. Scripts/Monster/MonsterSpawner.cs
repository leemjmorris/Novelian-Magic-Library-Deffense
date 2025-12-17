using UnityEngine;
using UnityEngine.AI;

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

        [Header("Gizmo Settings")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);
        [SerializeField] private Color wireColor = Color.yellow;

        /// <summary>
        /// 스폰 영역 내 랜덤 위치 반환 (NavMesh 표면으로 보정)
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = transform.position;
            Vector3 halfSize = spawnAreaSize / 2f;

            float randomX = Random.Range(center.x - halfSize.x, center.x + halfSize.x);
            float randomY = Random.Range(center.y - halfSize.y, center.y + halfSize.y);
            float randomZ = Random.Range(center.z - halfSize.z, center.z + halfSize.z);

            Vector3 randomPos = new Vector3(randomX, randomY, randomZ);

            // NavMesh 표면으로 보정
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            // NavMesh를 못 찾으면 원래 위치 반환 (fallback)
            Debug.LogWarning($"[MonsterSpawner] NavMesh를 찾을 수 없음: {randomPos}");
            return randomPos;
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
