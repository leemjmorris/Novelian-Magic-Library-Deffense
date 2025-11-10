using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class ObjectPoolManager
{
    private static ObjectPoolManager instance;
    public static ObjectPoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ObjectPoolManager();
            }
            return instance;
        }
    }

    private Dictionary<Type, object> pools = new Dictionary<Type, object>();    // JML: Generic Pool Dictionary
    private Dictionary<Type, GameObject> prefabs = new Dictionary<Type, GameObject>(); // JML: Prefab Dictionary
    private Dictionary<Type, AsyncOperationHandle<GameObject>> handles = new Dictionary<Type, AsyncOperationHandle<GameObject>>();  // JML: Handle Dictionary
    private Dictionary<Type, HashSet<Component>> activeObjects = new Dictionary<Type, HashSet<Component>>(); // JML: Active Objects Dictionary

    private ObjectPoolManager() { }

    public async UniTask<bool> CreatePoolAsync<T>(string addressableKey, int defaultCapacity = 10, int maxSize = 1000)
        where T : Component, IPoolable
    {
        Type type = typeof(T);

        if (pools.ContainsKey(type))
        {
            Debug.LogWarning($"Pool for type '{type.Name}' already exists.");
            return true;
        }
        try
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load addressable asset with key '{addressableKey}'.");
                return false;
            }

            GameObject prefab = handle.Result;

            if (prefab.GetComponent<T>() == null)
            {
                Debug.LogError($"Prefab does not have component of type {type.Name}");
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
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create pool for type '{type.Name}': {e.Message}");
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
        UnityEngine.Object.Destroy(component.gameObject);
    }

    public T Spawn<T>(Vector3 position, Quaternion rotation) where T : Component, IPoolable
    {
        Type type = typeof(T);

        if (!pools.ContainsKey(type))
        {
            Debug.LogError($"Pool for type '{type.Name}' does not exist. Call CreatePoolAsync first.");
            return null;
        }

        ObjectPool<T> pool = pools[type] as ObjectPool<T>;
        T component = pool.Get();
        component.transform.position = position;
        component.transform.rotation = rotation;
        return component;
    }

    public T Spawn<T>(Vector3 position) where T : Component, IPoolable
    {
        return Spawn<T>(position, Quaternion.identity);
    }

    public void Despawn<T>(T component) where T : Component, IPoolable
    {
        if (component == null)
        {
            return;
        } 
            
        Type type = typeof(T);

        if (!pools.ContainsKey(type))
        {
            Debug.LogError($"Pool for type '{type.Name}' does not exist.");
            return;
        }

        ObjectPool<T> pool = pools[type] as ObjectPool<T>;
        pool.Release(component);
    }

    public void DespawnAll<T>() where T : Component, IPoolable
    {
        Type type = typeof(T);

        if (!activeObjects.ContainsKey(type))
        {
            Debug.LogWarning($"No active objects tracked for type '{type.Name}'.");
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

    public int GetActiveCount<T>() where T : Component, IPoolable
    {
        Type type = typeof(T);
        return activeObjects.ContainsKey(type) ? activeObjects[type].Count : 0;
    }

    public void WarmUp<T>(int count) where T : Component, IPoolable
    {
        Type type = typeof(T);
        
        if (!pools.ContainsKey(type))
        {
            Debug.LogWarning($"Pool for type '{type.Name}' does not exist.");
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
    }

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
        }
    }

    public void ClearAll()
    {
        foreach (var pool in pools.Values)
        {
            (pool as IDisposable)?.Dispose();
        }
        
        foreach (var handle in handles.Values)
        {
            Addressables.Release(handle);
        }
        
        pools.Clear();
        prefabs.Clear();
        handles.Clear();
        activeObjects.Clear();
    }

    public bool HasPool<T>() where T : Component, IPoolable
    {
        return pools.ContainsKey(typeof(T));
    }
}
