using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool : Pool<GameObject>
{
  private string abname_;
  private string prefab_;
  private Transform pool_root_;
  private string key_;

  public static string GetKey(string abname, string prefab)
  {
    return abname + "/" + prefab;
  }

  public GameObjectPool(Transform parent, string abname, string prefab, int cap = 0) : base(cap)
  {
    abname_ = abname;
    prefab_ = prefab;
    key_ = GetKey(abname, prefab);

    GameObject obj = new GameObject();
    obj.name = "pools(" + key_ + ")";
    // obj.transform.parent = parent;
    // ResourceManager.Identify(obj);
    pool_root_ = obj.transform;

  }

  public override GameObject OnCreate()
  {
    // GameObject obj = ResourceManager.Instance.Instantiate(abname_, prefab_, pool_root_);
    GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>(key_), pool_root_);
    obj.name = GetKey(abname_, prefab_);
    return obj;
  }

  public override void OnGet(GameObject t)
  {
    t.SetActive(true);
  }

  public override void OnRecycle(GameObject t)
  {
    t.SetActive(false);
    t.transform.SetParent(pool_root_);
  }

  public override void OnDestroy(GameObject obj)
  {
    GameObject.Destroy(obj);
  }

  public override void OnClear()
  {
    GameObject.Destroy(pool_root_.gameObject);
  }
}