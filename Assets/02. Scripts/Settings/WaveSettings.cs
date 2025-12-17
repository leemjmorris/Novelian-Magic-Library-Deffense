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

        [Tooltip("스폰 영역 최소 Z 좌표")]
        public float minZ = -7.5f;

        [Tooltip("스폰 영역 최대 Z 좌표")]
        public float maxZ = -7.5f;

        /// <summary>
        /// 영역 내 랜덤 위치 반환
        /// </summary>
        public Vector3 GetRandomPosition()
        {
            float randomX = UnityEngine.Random.Range(minX, maxX);
            float randomY = UnityEngine.Random.Range(minY, maxY);
            float randomZ = UnityEngine.Random.Range(minZ, maxZ);
            return new Vector3(randomX, randomY, randomZ);
        }

        /// <summary>
        /// 영역의 중심점 반환
        /// </summary>
        public Vector3 GetCenter()
        {
            return new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
        }

        /// <summary>
        /// 영역의 크기 반환
        /// </summary>
        public Vector3 GetSize()
        {
            return new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
        }

        /// <summary>
        /// 최소 코너 위치 반환
        /// </summary>
        public Vector3 GetMinCorner()
        {
            return new Vector3(minX, minY, minZ);
        }

        /// <summary>
        /// 최대 코너 위치 반환
        /// </summary>
        public Vector3 GetMaxCorner()
        {
            return new Vector3(maxX, maxY, maxZ);
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

        [Header("Destination Area")]
        [Tooltip("몬스터 목적지 영역")]
        public SpawnAreaData destinationArea = new()
        {
            minX = -2f,
            maxX = 2f,
            minY = 0f,
            maxY = 2f,
            minZ = 5f,
            maxZ = 5f
        };

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

        /// <summary>
        /// 랜덤 목적지 위치 가져오기
        /// </summary>
        public Vector3 GetRandomDestinationPosition()
        {
            return destinationArea.GetRandomPosition();
        }
    }
}
