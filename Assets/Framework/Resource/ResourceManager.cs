using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
{
  #region singleton
  private static ResourceManager instance_ = null;
  public static ResourceManager Instance
  {
    get
    {
      if (instance_ == null)
      {
        instance_ = new ResourceManager();
      }
      return instance_;
    }
  }
  private ResourceManager() { }
  #endregion

  // 预制件字典
  private Dictionary<string, GameObject> prefab_dict_ = new Dictionary<string, GameObject>();
  // 声音文件字典
  private Dictionary<string, AudioClip> clip_map_ = new Dictionary<string, AudioClip>();

  public T LoadAsset<T>(string abname, string asset)where T : Object
  {
    T ab = Resources.Load<T>(abname + "/" + asset);
    if (null == ab)
    {
      LogSystem.Error("not find asset bundle by name {0}", abname);
      return null;
    }
    return ab;
  }

  public Sprite LoadSprite(string abname, string asset)
  {
    return LoadAsset<Sprite>(abname, asset);
  }

  public static Transform CreateRoot(string name, Transform parent_)
  {
    GameObject obj = new GameObject();
    obj.transform.parent = parent_;
    Identify(obj);
    obj.name = name;
    return obj.transform;
  }

  public static void Identify(GameObject obj)
  {
    obj.transform.localPosition = Vector3.zero;
    obj.transform.localRotation = Quaternion.identity;
    obj.transform.localScale = Vector3.one;
  }
  public static void Identify(GameObject obj, Transform parent)
  {
    obj.transform.parent = parent;
    obj.transform.localPosition = Vector3.zero;
    obj.transform.localRotation = Quaternion.identity;
    obj.transform.localScale = Vector3.one;
  }

  // 实例化对象，group为该对象的分组，不同的分组用于加载和销毁策略
  public GameObject Instantiate(string abname, string prefab, Transform parent = null)
  {
    GameObject prefabObj = GetPrefab(abname, prefab);
    if (null == prefabObj)
    {
      prefabObj = LoadAsset<GameObject>(abname, prefab);
      AddPrefab(abname, prefab, prefabObj);
    }
    if (null != prefabObj)
    {
      GameObject target = GameObject.Instantiate(prefabObj);
      if (parent != null)
      {
        //target.transform.parent = parent;
        target.transform.SetParent(parent);
      }
      Identify(target);
      return target;
    }
    else
    {
      LogSystem.Error("not find asset abname={0} prefab_name={1}", abname, prefab);
    }
    return null;
  }

  // 创建或取得声音文件，声音文件只需要加载一次，同一个声音文件可以同时播放多个
  public AudioClip GetAudioClip(string abname, string prefab)
  {
    AudioClip clip;
    if (clip_map_.TryGetValue(abname + prefab, out clip))
    {
      return clip;
    }
    clip = LoadAsset<AudioClip>(abname, prefab);
    if (null == clip)
    {
      Debug.LogError("load audio clip failed by name " + prefab);
      return null;
    }
    clip_map_.Add(abname + prefab, clip);
    return clip;
  }

  private GameObject GetPrefab(string abname, string asset)
  {
    GameObject target = null;
    if (prefab_dict_.TryGetValue(abname + asset, out target))
    {
      return target;
    }
    return target;
  }

  private void AddPrefab(string abname, string asset, GameObject prefab)
  {
    if (null != prefab)
    {
      prefab_dict_.Add(abname + asset, prefab);
    }
  }

}