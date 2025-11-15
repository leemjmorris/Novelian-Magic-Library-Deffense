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
        private Dictionary<Type, object> pools = new Dictionary<Type, object>();
        private Dictionary<Type, GameObject> prefabs = new Dictionary<Type, GameObject>();
        private Dictionary<Type, AsyncOperationHandle<GameObject>> handles = new Dictionary<Type, AsyncOperationHandle<GameObject>>();
        private Dictionary<Type, HashSet<Component>> activeObjects = new Dictionary<Type, HashSet<Component>>();

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
            return obj.GetComponent<T>();
        }

        private void OnGetFromPool<T>(T component) where T : Component, IPoolable
        {
            component.gameObject.SetActive(true);

            Type type = typeof(T);
            if (activeObjects.ContainsKey(type))
            {
                activeObjects[type].Add(component);
            }

            component.OnSpawn();
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
            component.transform.position = position;
            component.transform.rotation = rotation;
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
                temp.Add(pool.Get());
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
    }
}
