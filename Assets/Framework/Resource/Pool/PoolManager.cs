using System.Collections.Generic;
using UnityEngine;

// unity对象池管理器
// 创建的对象时，如果指定了父级，则使用指定的父级。否则会在root下创建一个
// 对应池的父节点。回收时会将对象的父节点调整回默认的父节点。
public class PoolManager
{
  #region singleton
  private static PoolManager instance_ = null;
  public static PoolManager Instance
  {
    get
    {
      if (instance_ == null)
      {
        instance_ = new PoolManager();
      }
      return instance_;
    }
  }
  private PoolManager() { }
  #endregion

  public static Transform pool_root
  {
    get { return root_; }
    set { root_ = value; }
  }

  // 清除所有对象池
  public void Clear()
  {
    foreach (var v in pool_dict_.Values)
    {
      v.Clear();
    }
    pool_dict_.Clear();
  }

  // 清除指定key的对象池
  public void Clear(string key)
  {
    GameObjectPool pool;
    if (pool_dict_.TryGetValue(key, out pool))
    {
      pool.Clear();
    }
  }

  // 创建对象池
  public void CreatePool(string abname, string name)
  {
    string key = GameObjectPool.GetKey(abname, name);
    if (!pool_dict_.ContainsKey(key))
    {
      GameObjectPool pool = new GameObjectPool(root_, abname, name);
      pool_dict_.Add(key, pool);
    }
  }

  // public static GameObject GetOrCreate(ABAsset asset) {
  //   return GetOrCreate(asset.ab, asset.prefab);
  // }

  // 取得一个对象，如果指定parent，则创建的对象将会使用指定的parent，否则放在默认的池节点
  public GameObject GetOrCreate(string abname, string name, Transform parent = null)
  {
    GameObjectPool pool;
    string key = GameObjectPool.GetKey(abname, name);
    if (!pool_dict_.TryGetValue(key, out pool))
    {
      pool = new GameObjectPool(root_, abname, name);
      pool_dict_.Add(key, pool);
    }
    GameObject obj = pool.GetOrCreate();
    if (parent != null && obj != null)
    {
      obj.transform.SetParent(parent);
      // ResourceManager.Identify(obj);
    }
    return obj;
  }

  // 通过key值取得或创建一个对象, 如果没有对象池，不创建
  public GameObject GetOrCreate(string key)
  {
    GameObjectPool pool;
    if (pool_dict_.TryGetValue(key, out pool))
    {
      return pool.GetOrCreate();
    }
    LogSystem.Error("pool manager not find pool by key " + key);
    return null;
  }

  public void Recycle(GameObject obj)
  {
    if (obj == null)
    {
      return;
    }
    GameObjectPool pool;
    if (pool_dict_.TryGetValue(obj.name, out pool))
    {
      pool.Recycle(obj);
    }
    else
    {
      GameObject.Destroy(obj);
    }
  }
  public void Recycle(List<GameObject> objList)
  {
     if (objList[0] == null)
    {
      return;
    }
    GameObjectPool pool;

    if (pool_dict_.TryGetValue(objList[0].name, out pool))
    {
      pool.Recycle(objList);
    }
    else
    {
      for (int i = 0; i < objList.Count; i++)
      {
        GameObject.Destroy(objList[i]);
      }
    }
  }

  private Dictionary<string, GameObjectPool> pool_dict_ = new Dictionary<string, GameObjectPool>();
  private static Transform root_; // 所有对象池的根结点
}