using UnityEngine;

namespace NovelianMagicLibraryDefense.Settings
{
    /// <summary>
    /// Wave 관련 설정값들을 관리하는 ScriptableObject
    /// Inspector에서 쉽게 조정 가능
    /// </summary>
    [CreateAssetMenu(fileName = "WaveSettings", menuName = "Settings/Wave Settings")]
    public class WaveSettings : ScriptableObject
    {
        [Header("Spawn Settings")]
        [Tooltip("일반 몬스터 스폰 간격 (초)")]
        public float spawnInterval = 2f;

        [Header("Spawn Position")]
        [Tooltip("스폰 위치 X 최소값")]
        public float spawnMinX = -0.4f;

        [Tooltip("스폰 위치 X 최대값")]
        public float spawnMaxX = 0.4f;

        [Tooltip("스폰 위치 Y")]
        public float spawnY = 3f;

        [Tooltip("스폰 위치 Z")]
        public float spawnZ = -7.5f;

        [Header("Boss Settings")]
        [Tooltip("보스 스폰 위치 X")]
        public float bossSpawnX = 0f;

        [Tooltip("보스 스폰 위치 Y")]
        public float bossSpawnY = 2f;

        [Tooltip("보스 스폰 위치 Z")]
        public float bossSpawnZ = -7.5f;

        /// <summary>
        /// 랜덤 스폰 위치 가져오기
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            float randomX = Random.Range(spawnMinX, spawnMaxX);
            return new Vector3(randomX, spawnY, spawnZ);
        }

        /// <summary>
        /// 보스 스폰 위치 가져오기
        /// </summary>
        public Vector3 GetBossSpawnPosition()
        {
            return new Vector3(bossSpawnX, bossSpawnY, bossSpawnZ);
        }
    }
}
