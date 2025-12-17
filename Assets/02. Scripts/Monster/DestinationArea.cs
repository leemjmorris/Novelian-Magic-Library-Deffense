using UnityEngine;

namespace NovelianMagicLibraryDefense.Spawners
{
    /// <summary>
    /// 몬스터 목적지 영역을 정의하는 컴포넌트
    /// Scene에서 Transform 위치와 크기로 목적지 영역을 설정
    /// </summary>
    public class DestinationArea : MonoBehaviour
    {
        [Header("Destination Area Settings")]
        [Tooltip("목적지 영역 크기")]
        [SerializeField] private Vector3 areaSize = new Vector3(4f, 2f, 0.5f);

        [Header("Gizmo Settings")]
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color wireColor = Color.green;

        /// <summary>
        /// 목적지 영역 내 랜덤 위치 반환
        /// </summary>
        public Vector3 GetRandomDestinationPosition()
        {
            Vector3 center = transform.position;
            Vector3 halfSize = areaSize / 2f;

            float randomX = Random.Range(center.x - halfSize.x, center.x + halfSize.x);
            float randomY = Random.Range(center.y - halfSize.y, center.y + halfSize.y);
            float randomZ = Random.Range(center.z - halfSize.z, center.z + halfSize.z);

            return new Vector3(randomX, randomY, randomZ);
        }

        /// <summary>
        /// 목적지 영역 중심 위치 반환
        /// </summary>
        public Vector3 GetCenter()
        {
            return transform.position;
        }

        /// <summary>
        /// 목적지 영역 크기 반환
        /// </summary>
        public Vector3 GetSize()
        {
            return areaSize;
        }

        private void OnDrawGizmos()
        {
            // 반투명 박스 그리기
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(transform.position, areaSize);

            // 와이어프레임 그리기
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(transform.position, areaSize);
        }

        private void OnDrawGizmosSelected()
        {
            // 선택 시 강조
            Gizmos.color = new Color(wireColor.r, wireColor.g, wireColor.b, 0.8f);
            Gizmos.DrawWireCube(transform.position, areaSize);

#if UNITY_EDITOR
            // 라벨 표시
            Vector3 labelPos = transform.position + Vector3.up * (areaSize.y / 2 + 0.5f);
            UnityEditor.Handles.Label(labelPos,
                $"Destination Area\nSize: {areaSize.x:F1} x {areaSize.y:F1} x {areaSize.z:F1}");
#endif
        }
    }
}
