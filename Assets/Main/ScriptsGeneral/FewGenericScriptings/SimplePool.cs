using System.Collections.Generic;
using UnityEngine;

public static class SimplePool
{
    private static Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private static GameObject poolContainer;

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }

        GameObject obj;
        if (pools[prefab].Count > 0)
        {
            obj = pools[prefab].Dequeue();
            if (obj == null) // In case it was destroyed manually
            {
                obj = Object.Instantiate(prefab, position, rotation);
            }
            else
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
        }
        else
        {
            obj = Object.Instantiate(prefab, position, rotation);
        }

        // Store the prefab reference in a custom component to know where to return it
        var poolItem = obj.GetComponent<PoolItem>() ?? obj.AddComponent<PoolItem>();
        poolItem.prefab = prefab;

        return obj;
    }

    public static void Despawn(GameObject obj)
    {
        if (obj == null) return;

        var poolItem = obj.GetComponent<PoolItem>();
        if (poolItem != null && poolItem.prefab != null)
        {
            GameObject prefab = poolItem.prefab;
            
            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new Queue<GameObject>();
            }

            obj.SetActive(false);
            
            // Organize in hierarchy
            if (poolContainer == null)
            {
                poolContainer = new GameObject("VFX_Pool_Container");
                Object.DontDestroyOnLoad(poolContainer);
            }
            obj.transform.SetParent(poolContainer.transform);
            
            pools[prefab].Enqueue(obj);
        }
        else
        {
            Object.Destroy(obj);
        }
    }

    public static void DespawnAfter(GameObject obj, float delay)
    {
        var helper = obj.GetComponent<PoolDespawnHelper>() ?? obj.AddComponent<PoolDespawnHelper>();
        helper.DespawnAfter(delay);
    }
}

public class PoolItem : MonoBehaviour
{
    public GameObject prefab;
}

public class PoolDespawnHelper : MonoBehaviour
{
    public void DespawnAfter(float delay)
    {
        CancelInvoke("DoDespawn");
        Invoke("DoDespawn", delay);
    }

    private void DoDespawn()
    {
        SimplePool.Despawn(gameObject);
    }
}
