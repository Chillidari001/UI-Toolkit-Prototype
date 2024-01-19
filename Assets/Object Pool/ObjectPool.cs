using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private GameObject parent;
    private PoolableObject prefab;
    private int size;
    private List<PoolableObject> available_objects_pool;
    private static Dictionary<PoolableObject, ObjectPool> object_pools = new Dictionary<PoolableObject, ObjectPool>();

    private ObjectPool(PoolableObject prefab, int size)
    {
        this.prefab = prefab;
        this.size = size;
        available_objects_pool = new List<PoolableObject>(size);
    }

    public static ObjectPool CreateInstance(PoolableObject prefab, int size)
    {
        ObjectPool pool = null;

        if (object_pools.ContainsKey(prefab))
        {
            pool = object_pools[prefab];
        }
        else
        {
            pool = new ObjectPool(prefab, size);

            pool.parent = new GameObject(prefab + " Pool");
            pool.CreateObjects();

            object_pools.Add(prefab, pool);
        }


        return pool;
    }

    private void CreateObjects()
    {
        for (int i = 0; i < size; i++)
        {
            CreateObject();
        }
    }

    private void CreateObject()
    {
        PoolableObject poolableObject = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent.transform);
        poolableObject.parent = this;
        poolableObject.gameObject.SetActive(false); // PoolableObject handles re-adding the object to the AvailableObjects
    }

    public PoolableObject GetObject(Vector3 Position, Quaternion Rotation)
    {
        if (available_objects_pool.Count == 0) // auto expand pool size if out of objects
        {
            CreateObject();
        }

        PoolableObject instance = available_objects_pool[0];

        available_objects_pool.RemoveAt(0);

        instance.transform.position = Position;
        instance.transform.rotation = Rotation;

        instance.gameObject.SetActive(true);

        return instance;
    }

    public PoolableObject GetObject()
    {
        return GetObject(Vector3.zero, Quaternion.identity);
    }

    public void ReturnObjectToPool(PoolableObject _object)
    {
        available_objects_pool.Add(_object);
    }
}
