using System;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Settings
{
    /// <summary>
    /// 스폰 영역 데이터를 정의하는 Serializable 클래스
    /// </summary>
    [Serializable]
    public class SpawnAreaData
    {
        [Tooltip("스폰 영역 최소 X 좌표")]
        public float minX = -0.4f;

        [Tooltip("스폰 영역 최대 X 좌표")]
        public float maxX = 0.4f;

        [Tooltip("스폰 영역 최소 Y 좌표")]
        public float minY = 2f;

        [Tooltip("스폰 영역 최대 Y 좌표")]
        public float maxY = 4f;

        [Tooltip("고정된 Z 좌표")]
        public float fixedZ = -7.5f;

        /// <summary>
        /// 영역 내 랜덤 위치 반환
        /// </summary>
        public Vector3 GetRandomPosition()
        {
            float randomX = UnityEngine.Random.Range(minX, maxX);
            float randomY = UnityEngine.Random.Range(minY, maxY);
            return new Vector3(randomX, randomY, fixedZ);
        }

        /// <summary>
        /// 영역의 중심점 반환
        /// </summary>
        public Vector3 GetCenter()
        {
            return new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, fixedZ);
        }

        /// <summary>
        /// 영역의 크기 반환
        /// </summary>
        public Vector3 GetSize()
        {
            return new Vector3(maxX - minX, maxY - minY, 0.1f);
        }
    }

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

        [Header("Spawn Areas")]
        [Tooltip("일반 몬스터 스폰 영역")]
        public SpawnAreaData normalSpawnArea = new();

        [Tooltip("보스 몬스터 스폰 영역")]
        public SpawnAreaData bossSpawnArea = new();

        /// <summary>
        /// 랜덤 스폰 위치 가져오기
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            return normalSpawnArea.GetRandomPosition();
        }

        /// <summary>
        /// 보스 스폰 위치 가져오기
        /// </summary>
        public Vector3 GetBossSpawnPosition()
        {
            return bossSpawnArea.GetRandomPosition();
        }
    }
}
