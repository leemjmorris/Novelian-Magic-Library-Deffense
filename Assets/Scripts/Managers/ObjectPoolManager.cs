using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manages object pooling using Unity's ObjectPool and Addressables
    /// MonoBehaviour 기반 Manager (VContainer 지원)
    /// </summary>
    public class ObjectPoolManager : BaseManager
    {
        // Type 기반 풀링 (기존 - Projectile, FloatingDamageText 등에서 사용)
        private Dictionary<Type, object> pools = new Dictionary<Type, object>();
        private Dictionary<Type, GameObject> prefabs = new Dictionary<Type, GameObject>();
        private Dictionary<Type, AsyncOperationHandle<GameObject>> handles = new Dictionary<Type, AsyncOperationHandle<GameObject>>();
        private Dictionary<Type, HashSet<Component>> activeObjects = new Dictionary<Type, HashSet<Component>>();

        // JML: 키 기반 풀링 (Monster 등 Addressable 키로 다양한 프리팹 사용)
        private Dictionary<string, object> keyBasedPools = new Dictionary<string, object>();
        private Dictionary<string, GameObject> keyBasedPrefabs = new Dictionary<string, GameObject>();
        private Dictionary<string, AsyncOperationHandle<GameObject>> keyBasedHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private Dictionary<string, HashSet<Component>> keyBasedActiveObjects = new Dictionary<string, HashSet<Component>>();

        protected override void OnInitialize()
        {
            // Debug.Log("[ObjectPoolManager] Initialized");
        }

        protected override void OnReset()
        {
            // Debug.Log("[ObjectPoolManager] Resetting all pools");
            ClearAll();
        }

        protected override void OnDispose()
        {
            // Debug.Log("[ObjectPoolManager] Disposing all pools");
            ClearAll();
        }

        /// <summary>
        /// JML: Create a new object pool with direct prefab reference (synchronous)
        /// </summary>
        public bool CreatePool<T>(GameObject prefab, int defaultCapacity = 10, int maxSize = 1000)
            where T : Component, IPoolable
        {
            Type type = typeof(T);

            if (pools.ContainsKey(type))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool for type '{type.Name}' already exists.");
                return true;
            }

            if (prefab == null)
            {
                Debug.LogError($"[ObjectPoolManager] Prefab is null for type '{type.Name}'.");
                return false;
            }

            if (prefab.GetComponent<T>() == null)
            {
                Debug.LogError($"[ObjectPoolManager] Prefab does not have component of type {type.Name}");
                return false;
            }

            prefabs[type] = prefab;
            activeObjects[type] = new HashSet<Component>();

            var pool = new ObjectPool<T>(
                createFunc: () => CreatePooledObject<T>(),
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            pools[type] = pool;
            // Debug.Log($"[ObjectPoolManager] Pool created for {type.Name} (capacity: {defaultCapacity}, max: {maxSize})");
            return true;
        }

        /// <summary>
        /// LMJ: Create a new object pool with Addressables
        /// </summary>
        public async UniTask<bool> CreatePoolAsync<T>(string addressableKey, int defaultCapacity = 10, int maxSize = 1000)
            where T : Component, IPoolable
        {
            Type type = typeof(T);

            if (pools.ContainsKey(type))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool for type '{type.Name}' already exists.");
                return true;
            }

            try
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
                await handle.ToUniTask();

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[ObjectPoolManager] Failed to load addressable asset with key '{addressableKey}'.");
                    return false;
                }

                GameObject prefab = handle.Result;

                if (prefab.GetComponent<T>() == null)
                {
                    Debug.LogError($"[ObjectPoolManager] Prefab does not have component of type {type.Name}");
                    Addressables.Release(handle);
                    return false;
                }

                prefabs[type] = prefab;
                handles[type] = handle;
                activeObjects[type] = new HashSet<Component>();

                var pool = new ObjectPool<T>(
                    createFunc: () => CreatePooledObject<T>(),
                    actionOnGet: OnGetFromPool,
                    actionOnRelease: OnReleaseToPool,
                    actionOnDestroy: OnDestroyPoolObject,
                    collectionCheck: true,
                    defaultCapacity: defaultCapacity,
                    maxSize: maxSize
                );

                pools[type] = pool;
                // Debug.Log($"[ObjectPoolManager] Pool created for {type.Name} (capacity: {defaultCapacity}, max: {maxSize})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ObjectPoolManager] Failed to create pool for type '{type.Name}': {e.Message}");
                return false;
            }
        }

        private T CreatePooledObject<T>() where T : Component, IPoolable
        {
            Type type = typeof(T);
            GameObject obj = UnityEngine.Object.Instantiate(prefabs[type]);
            obj.name = prefabs[type].name;
            // JML: 새로 생성된 오브젝트는 비활성 상태로 시작 (Spawn에서 위치 설정 후 활성화)
            obj.SetActive(false);
            return obj.GetComponent<T>();
        }

        private void OnGetFromPool<T>(T component) where T : Component, IPoolable
        {
            // 활성화하지 않음 - Spawn()에서 위치 설정 후 활성화
            // (OnGetFromPool은 pool.Get() 시점에 호출되므로, 여기서 활성화하면 잘못된 위치에서 보임)

            Type type = typeof(T);
            if (activeObjects.ContainsKey(type))
            {
                activeObjects[type].Add(component);
            }
        }

        private void OnReleaseToPool<T>(T component) where T : Component, IPoolable
        {
            component.OnDespawn();
            component.gameObject.SetActive(false);

            Type type = typeof(T);
            if (activeObjects.ContainsKey(type))
            {
                activeObjects[type].Remove(component);
            }
        }

        private void OnDestroyPoolObject<T>(T component) where T : Component, IPoolable
        {
            Type type = typeof(T);
            if (activeObjects.ContainsKey(type))
            {
                activeObjects[type].Remove(component);
            }

            // LMJ: Check if component still exists before destroying
            if (component != null && component.gameObject != null)
            {
                UnityEngine.Object.Destroy(component.gameObject);
            }
        }

        /// <summary>
        /// LMJ: Spawn object from pool with position and rotation
        /// </summary>
        public T Spawn<T>(Vector3 position, Quaternion rotation) where T : Component, IPoolable
        {
            Type type = typeof(T);

            if (!pools.ContainsKey(type))
            {
                Debug.LogError($"[ObjectPoolManager] Pool for type '{type.Name}' does not exist. Call CreatePoolAsync first.");
                return null;
            }

            ObjectPool<T> pool = pools[type] as ObjectPool<T>;
            T component = pool.Get();

            // 1. 위치 설정 (활성화 전에 먼저!)
            component.transform.position = position;
            component.transform.rotation = rotation;

            // 2. 활성화 (올바른 위치에서 나타남)
            component.gameObject.SetActive(true);

            // 3. OnSpawn() 호출 (목적지 계산에 현재 위치가 필요함)
            component.OnSpawn();

            return component;
        }

        /// <summary>
        /// LMJ: Spawn object from pool with position only
        /// </summary>
        public T Spawn<T>(Vector3 position) where T : Component, IPoolable
        {
            return Spawn<T>(position, Quaternion.identity);
        }

        /// <summary>
        /// LMJ: Return object to pool
        /// </summary>
        public void Despawn<T>(T component) where T : Component, IPoolable
        {
            // LMJ: Safety checks
            if (component == null || component.gameObject == null)
            {
                return;
            }

            // LMJ: Skip if already inactive (already despawned)
            if (!component.gameObject.activeSelf)
            {
                return;
            }

            Type type = typeof(T);

            if (!pools.ContainsKey(type))
            {
                Debug.LogError($"[ObjectPoolManager] Pool for type '{type.Name}' does not exist.");
                return;
            }

            if (!activeObjects.ContainsKey(type) || !activeObjects[type].Contains(component))
            {
                // LMJ: Object not in active list - might be already despawned, just deactivate it
                component.gameObject.SetActive(false);
                return;
            }

            ObjectPool<T> pool = pools[type] as ObjectPool<T>;
            pool.Release(component);
        }

        /// <summary>
        /// LMJ: Return all active objects of type to pool
        /// </summary>
        public void DespawnAll<T>() where T : Component, IPoolable
        {
            Type type = typeof(T);

            if (!activeObjects.ContainsKey(type))
            {
                Debug.LogWarning($"[ObjectPoolManager] No active objects tracked for type '{type.Name}'.");
                return;
            }

            List<T> objectsToReturn = new List<T>();
            foreach (var obj in activeObjects[type])
            {
                objectsToReturn.Add(obj as T);
            }

            foreach (var obj in objectsToReturn)
            {
                Despawn(obj);
            }
        }

        /// <summary>
        /// LMJ: Get count of active objects for type
        /// </summary>
        public int GetActiveCount<T>() where T : Component, IPoolable
        {
            Type type = typeof(T);
            return activeObjects.ContainsKey(type) ? activeObjects[type].Count : 0;
        }

        /// <summary>
        /// LMJ: Pre-instantiate objects to avoid runtime spikes
        /// WarmUp은 객체를 생성만 하고, 화면에 보이지 않게 즉시 반환
        /// </summary>
        public void WarmUp<T>(int count) where T : Component, IPoolable
        {
            Type type = typeof(T);

            if (!pools.ContainsKey(type))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool for type '{type.Name}' does not exist.");
                return;
            }

            List<T> temp = new List<T>();
            ObjectPool<T> pool = pools[type] as ObjectPool<T>;

            for (int i = 0; i < count; i++)
            {
                T obj = pool.Get();
                // WarmUp 시에는 비활성화 (OnGetFromPool에서 위치가 Vector3.zero로 초기화됨)
                obj.gameObject.SetActive(false);
                temp.Add(obj);
            }

            foreach (var obj in temp)
            {
                pool.Release(obj);
            }

            // Debug.Log($"[ObjectPoolManager] Warmed up {count} objects of type {type.Name}");
        }

        /// <summary>
        /// LMJ: Clear specific pool
        /// </summary>
        public void ClearPool<T>() where T : Component, IPoolable
        {
            Type type = typeof(T);

            if (pools.ContainsKey(type))
            {
                DespawnAll<T>();
                (pools[type] as ObjectPool<T>)?.Clear();
                pools.Remove(type);
                prefabs.Remove(type);
                activeObjects.Remove(type);

                if (handles.ContainsKey(type))
                {
                    Addressables.Release(handles[type]);
                    handles.Remove(type);
                }

                Debug.Log($"[ObjectPoolManager] Pool cleared for {type.Name}");
            }
        }

        /// <summary>
        /// LMJ: Clear all pools
        /// </summary>
        public void ClearAll()
        {
            // LMJ: Deactivate all active objects safely
            foreach (var type in activeObjects.Keys.ToList())
            {
                foreach (var obj in activeObjects[type].ToList())
                {
                    if (obj != null && obj.gameObject != null)
                    {
                        obj.gameObject.SetActive(false);
                    }
                }
            }

            // LMJ: Dispose pools (this may trigger OnDestroyPoolObject)
            foreach (var pool in pools.Values)
            {
                try
                {
                    (pool as IDisposable)?.Dispose();
                }
                catch (System.Exception e)
                {
                    // LMJ: Ignore errors during disposal (objects may already be destroyed)
                    Debug.LogWarning($"[ObjectPoolManager] Error disposing pool: {e.Message}");
                }
            }

            // LMJ: Release addressable handles
            foreach (var handle in handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            pools.Clear();
            prefabs.Clear();
            handles.Clear();
            activeObjects.Clear();

            Debug.Log("[ObjectPoolManager] All pools cleared");
        }

        /// <summary>
        /// LMJ: Check if pool exists for type
        /// </summary>
        public bool HasPool<T>() where T : Component, IPoolable
        {
            return pools.ContainsKey(typeof(T));
        }

        #region Key-Based Pooling (JML)

        /// <summary>
        /// JML: 키 기반 풀 생성 (Addressable)
        /// </summary>
        public async UniTask<bool> CreatePoolByKeyAsync<T>(string key, int defaultCapacity = 10, int maxSize = 100)
            where T : Component, IPoolable
        {
            if (keyBasedPools.ContainsKey(key))
            {
                Debug.LogWarning($"[ObjectPoolManager] Key-based pool '{key}' already exists.");
                return true;
            }

            try
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.ToUniTask();

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[ObjectPoolManager] Failed to load addressable asset with key '{key}'.");
                    return false;
                }

                GameObject prefab = handle.Result;

                if (prefab.GetComponent<T>() == null)
                {
                    Debug.LogError($"[ObjectPoolManager] Prefab '{key}' does not have component of type {typeof(T).Name}");
                    Addressables.Release(handle);
                    return false;
                }

                keyBasedPrefabs[key] = prefab;
                keyBasedHandles[key] = handle;
                keyBasedActiveObjects[key] = new HashSet<Component>();

                var pool = new ObjectPool<T>(
                    createFunc: () => CreateKeyBasedPooledObject<T>(key),
                    actionOnGet: obj => OnGetFromKeyBasedPool(key, obj),
                    actionOnRelease: obj => OnReleaseToKeyBasedPool(key, obj),
                    actionOnDestroy: obj => OnDestroyKeyBasedPoolObject(key, obj),
                    collectionCheck: true,
                    defaultCapacity: defaultCapacity,
                    maxSize: maxSize
                );

                keyBasedPools[key] = pool;
                Debug.Log($"[ObjectPoolManager] Key-based pool created: {key}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ObjectPoolManager] Failed to create key-based pool '{key}': {e.Message}");
                return false;
            }
        }

        private T CreateKeyBasedPooledObject<T>(string key) where T : Component, IPoolable
        {
            GameObject obj = UnityEngine.Object.Instantiate(keyBasedPrefabs[key]);
            obj.name = keyBasedPrefabs[key].name;
            obj.SetActive(false);
            return obj.GetComponent<T>();
        }

        private void OnGetFromKeyBasedPool<T>(string key, T component) where T : Component, IPoolable
        {
            if (keyBasedActiveObjects.ContainsKey(key))
            {
                keyBasedActiveObjects[key].Add(component);
            }
        }

        private void OnReleaseToKeyBasedPool<T>(string key, T component) where T : Component, IPoolable
        {
            component.OnDespawn();
            component.gameObject.SetActive(false);

            if (keyBasedActiveObjects.ContainsKey(key))
            {
                keyBasedActiveObjects[key].Remove(component);
            }
        }

        private void OnDestroyKeyBasedPoolObject<T>(string key, T component) where T : Component, IPoolable
        {
            if (keyBasedActiveObjects.ContainsKey(key))
            {
                keyBasedActiveObjects[key].Remove(component);
            }

            if (component != null && component.gameObject != null)
            {
                UnityEngine.Object.Destroy(component.gameObject);
            }
        }

        /// <summary>
        /// JML: 키 기반 스폰
        /// </summary>
        public T SpawnByKey<T>(string key, Vector3 position, Quaternion rotation) where T : Component, IPoolable
        {
            if (!keyBasedPools.ContainsKey(key))
            {
                Debug.LogError($"[ObjectPoolManager] Key-based pool '{key}' does not exist.");
                return null;
            }

            ObjectPool<T> pool = keyBasedPools[key] as ObjectPool<T>;

            // JML: 웜업 초과 감지용 - Get() 전 풀 상태 확인
            int countBeforeGet = pool.CountInactive;

            T component = pool.Get();

            // JML: 풀에서 가져온 게 아니라 새로 생성된 경우 (CountInactive가 0이었던 경우)
            if (countBeforeGet == 0)
            {
                Debug.LogWarning($"[ObjectPoolManager] 웜업 초과! '{key}' 새 인스턴스 생성됨. Active: {pool.CountActive}, Inactive: {pool.CountInactive}");
            }

            component.transform.position = position;
            component.transform.rotation = rotation;
            component.gameObject.SetActive(true);
            component.OnSpawn();

            return component;
        }

        public T SpawnByKey<T>(string key, Vector3 position) where T : Component, IPoolable
        {
            return SpawnByKey<T>(key, position, Quaternion.identity);
        }

        /// <summary>
        /// JML: 키 기반 디스폰
        /// </summary>
        public void DespawnByKey<T>(string key, T component) where T : Component, IPoolable
        {
            if (component == null || component.gameObject == null)
            {
                Debug.LogWarning($"[ObjectPoolManager] DespawnByKey '{key}' - component or gameObject is null");
                return;
            }

            if (!component.gameObject.activeSelf)
            {
                Debug.LogWarning($"[ObjectPoolManager] DespawnByKey '{key}' - gameObject already inactive");
                return;
            }

            if (!keyBasedPools.ContainsKey(key))
            {
                Debug.LogError($"[ObjectPoolManager] Key-based pool '{key}' does not exist.");
                return;
            }

            if (!keyBasedActiveObjects.ContainsKey(key))
            {
                Debug.LogError($"[ObjectPoolManager] DespawnByKey '{key}' - activeObjects key 없음! 풀로 반환 불가");
                component.gameObject.SetActive(false);
                return;
            }

            if (!keyBasedActiveObjects[key].Contains(component))
            {
                Debug.LogError($"[ObjectPoolManager] DespawnByKey '{key}' - component가 activeObjects에 없음! Count: {keyBasedActiveObjects[key].Count}");
                component.gameObject.SetActive(false);
                return;
            }

            ObjectPool<T> pool = keyBasedPools[key] as ObjectPool<T>;
            if (pool == null)
            {
                Debug.LogError($"[ObjectPoolManager] DespawnByKey '{key}' - pool cast 실패!");
                return;
            }

            pool.Release(component);
        }

        /// <summary>
        /// JML: 키 기반 풀 존재 확인
        /// </summary>
        public bool HasPoolByKey(string key)
        {
            return keyBasedPools.ContainsKey(key);
        }

        /// <summary>
        /// JML: 비동기 키 기반 웜업 (프레임 분산)
        /// </summary>
        public async UniTask WarmUpByKeyAsync<T>(string key, int count, int perFrame = 3)
            where T : Component, IPoolable
        {
            if (!keyBasedPools.ContainsKey(key))
            {
                Debug.LogWarning($"[ObjectPoolManager] Key-based pool '{key}' does not exist.");
                return;
            }

            List<T> temp = new List<T>();
            ObjectPool<T> pool = keyBasedPools[key] as ObjectPool<T>;

            for (int i = 0; i < count; i++)
            {
                T obj = pool.Get();
                obj.gameObject.SetActive(false);
                temp.Add(obj);

                if ((i + 1) % perFrame == 0)
                {
                    await UniTask.Yield();
                }
            }

            foreach (var obj in temp)
            {
                pool.Release(obj);
            }

            Debug.Log($"[ObjectPoolManager] Async warmed up {count} objects for key: {key}");
        }

        /// <summary>
        /// JML: 모든 키 기반 풀 클리어
        /// </summary>
        public void ClearAllKeyBasedPools()
        {
            foreach (var key in keyBasedActiveObjects.Keys.ToList())
            {
                foreach (var obj in keyBasedActiveObjects[key].ToList())
                {
                    if (obj != null && obj.gameObject != null)
                    {
                        obj.gameObject.SetActive(false);
                    }
                }
            }

            foreach (var pool in keyBasedPools.Values)
            {
                try
                {
                    (pool as IDisposable)?.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ObjectPoolManager] Error disposing key-based pool: {e.Message}");
                }
            }

            foreach (var handle in keyBasedHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            keyBasedPools.Clear();
            keyBasedPrefabs.Clear();
            keyBasedHandles.Clear();
            keyBasedActiveObjects.Clear();

            Debug.Log("[ObjectPoolManager] All key-based pools cleared");
        }

        #endregion
    }
}
