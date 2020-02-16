using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Threading;
#if UNITY_2017
using UnityWebRequestAssetBundle = UnityEngine.Networking.UnityWebRequest;
#endif

namespace AB
{
    public enum AssetType
    {
        LocalAsset = 0, //本地Asset
        LocalBundle = 1,//本地Bundle
        RemoteBundle = 2,//网络Bundle
    }

    public enum LocationType
    {
        /// <summary>
        /// 打进包的素材
        /// </summary>
        InPack = 1,
        /// <summary>
        /// 在本地Cache的素材
        /// </summary>
        InCache = 1 << 1,
        /// <summary>
        /// 在网络上还没被下载的素材
        /// </summary>
        InNetwork = 1 << 2,
        /// <summary>
        /// 在网络上还没被下载的素材
        /// </summary>
        OnlyNetwork = 1 << 3,
    }

    [Serializable]
    public class BundleData
    {
        string bundleHash;
        public bool bundleInPack;
        public string bundleName;
        public string[] bundleDependencies;
        public long bundleSize;

        public Hash128 bundleHash128
        {
            get
            {
                return Hash128.Parse(bundleHash);
            }
            set
            {
                bundleHash = value.ToString();
            }
        }
    }

    [Serializable]
    public class BundleName
    {
        public bool atlasSprite;
        public string bundleName;
        public string atlasName;
        public string spriteName;
    }

    [Serializable]
    public class BundleDatas
    {
        public Dictionary<string, BundleData> Datas = new Dictionary<string, BundleData>();
        public Dictionary<string, BundleName> Names = new Dictionary<string, BundleName>();
        public static byte[] Serialize(BundleDatas o)
        {
            var s = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(s, o);
            return s.ToArray();
        }

        public static BundleDatas Deserialize(byte[] arr)
        {
            var s = new MemoryStream(arr);
            var formatter = new BinaryFormatter();
            var o = (BundleDatas)formatter.Deserialize(s);
            return o;
        }
    }

    [ExecuteInEditMode]
    public sealed class Assets : MonoBehaviour
    {
        static Assets s_Instance = null;

#if UNITY_EDITOR
        public static AssetType LoadType = AssetType.LocalAsset;
#endif

        static Assets Instance
        {
            get
            {
                if (s_Instance == null)
                {
#if UNITY_EDITOR
                    s_Instance = FindObjectOfType<Assets>();

                    if (s_Instance != null)
                    {
                        return s_Instance;
                    }
#endif
                    var go = new GameObject("Assets");
                    DontDestroyOnLoad(go);
                    s_Instance = go.AddComponent<Assets>();
                }
                return s_Instance;
            }
        }

		public static IEnumerator Initialize()
		{
#if UNITY_EDITOR
            var ins = Instance;
            if (LoadType == AssetType.LocalAsset)
            {
                Bundles.InitializeForEditor();
            }
            else
#endif
            yield return InitializeBundle();
        }
        public static string AssetPath{
            get;
            set;
        }
        public static bool Exists(string path)
        {
            path = path.ToLower();
            return Bundles.GetBundleName(path) != null;
        }
        public static Asset LoadAsset(string path, Action<Asset> overAction, bool asyncMode = true)
        {
            return LoadAssetT<UnityEngine.Object>(path, overAction, asyncMode);
        }
        
        public static Asset LoadAssetT<T>(string path, Action<Asset> overAction, bool asyncMode = true)
        {
            Helper.FixTextAssetExt(ref path);
            var asset = LoadInternal(path, typeof(T), asyncMode);
            if (asyncMode)
            {
                Instance.StartCoroutine(LoadAssetInternal<UnityEngine.Object>(asset, overAction));
            }
            else
            {
                overAction?.Invoke(asset);
            }
            return asset;
        }
        static IEnumerator LoadAssetInternal<T>(Asset asset,Action<Asset> overAction){
            yield return asset;
            overAction?.Invoke(asset);
        }

		public static void Unload (Asset asset)
		{
			asset.Unload ();
		}

        static IEnumerator InitializeBundle()
		{
            yield return Bundles.Initialize();
		}

        static Asset LoadInternal (string path, System.Type type, bool asyncMode)
		{
            path = path.ToLower();
            Asset asset = assets.Find (obj => { return obj.assetPath == path; });
			if (asset == null) {
                var bundleName = Bundles.GetBundleName(path);
#if UNITY_EDITOR
                if (LoadType == AssetType.LocalAsset)
                {
                    asset = new Asset(path, bundleName?.bundleName, type);
                } else
#endif
                if(true)
                {
                    if (asyncMode)
                    {
                        asset = new BundleAssetAsync(path, bundleName?.bundleName, type);
                    }
                    else
                    {
                        asset = new BundleAsset(path, bundleName?.bundleName, type);
                    }
                }
                //处理正在卸载的情况
                if (AsyncOperationUnloadUnusedAssets != null)
                {
                    var tick = DateTime.Now.Ticks;
                    Debug.Log("Wait UnloadUnusedAssets Begin");
                    if (asyncMode)
                    {
                        Instance.StartCoroutine(Instance.AssetInitialize(asset));
                    }
                    else
                    {
                        while (!AsyncOperationUnloadUnusedAssets.isDone)
                        {
                            Thread.Yield();
                        }
                    }
                    Debug.Log("Wait UnloadUnusedAssets End use :" + (DateTime.Now.Ticks - tick));
                }
                else
                {
                    asset.Initialize();
                }
				assets.Add (asset);
			}
			asset.Load ();
			return asset;
		}



        private void OnDestroy()
        {
#if UNITY_EDITOR
            LoadType = AssetType.LocalAsset;
#endif
            assets.Clear();
        }

        static List<Asset> assets = new List<Asset> ();

        float times = 0;
        void Update()
		{
            bool loading = false;
            for (int i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset.isDone)
                {
                    asset.Update();
                    if (asset.references <= 0)
                    {
                        asset.Dispose();
                        asset = null;
                        assets.RemoveAt(i);
                        i--;
                    }
                }
                else
                {
                    loading = true;
                }
            }

            if (!loading)
            {
                Bundles.Update();
                times += Time.deltaTime;
                if (times > 120)
                {
                    times = 0;
                    if (AsyncOperationUnloadUnusedAssets == null)
                    {
                        StartCoroutine(UnloadUnusedAssets());
                    }
                }
            }
        }

        static AsyncOperation AsyncOperationUnloadUnusedAssets;
        IEnumerator UnloadUnusedAssets()
        {
            if (AsyncOperationUnloadUnusedAssets != null) yield break;
            AsyncOperationUnloadUnusedAssets = Resources.UnloadUnusedAssets();
            yield return AsyncOperationUnloadUnusedAssets;
            AsyncOperationUnloadUnusedAssets = null;
        }

        IEnumerator AssetInitialize(Asset asset)
        {
            yield return AsyncOperationUnloadUnusedAssets;
            asset.Initialize();
        }


#region download & decompress
        public static void ObtainNetwork(BundleData bundleData, Action<string, float, bool> handler = null)
        {
            var sourceFile = Helper.GetDataPath(bundleData.bundleName);
            if (bundleData.bundleInPack)
            {
                sourceFile = "file:///" + Helper.GetInternalPath(bundleData.bundleName);
            }
            Instance.StartCoroutine(Obtain(sourceFile, bundleData.bundleName, bundleData.bundleHash128, handler));
        }

        static IEnumerator Obtain(string sourceFile, string bundleName, Hash128 bundleHash, Action<string, float, bool> handle)
        {
            //List<Hash128> listOfCachedVersions = new List<Hash128>();
            //Caching.GetCachedVersions(bundleName, listOfCachedVersions);
            //foreach(var s in listOfCachedVersions){
            //    Debug.Log(sourceFile+" -> "+s);
            //}
            var request = UnityWebRequestAssetBundle.GetAssetBundle(sourceFile, bundleHash, 0);
            request.SendWebRequest();
            while (!request.isDone)
            {
                handle?.Invoke(bundleName, request.downloadProgress, false);
                yield return new WaitForEndOfFrame();
            }
            if(bundleName.Contains(Helper.LogoLoadingPrefabName))
            {
                File.WriteAllBytes(Application.persistentDataPath + "/" + Helper.LogoLoadingPrefabName, request.downloadHandler.data);
            }
            //yield return request.Send();
            request.Dispose();
            request = null;
            handle?.Invoke(bundleName, 1.0f, true);
        }
        /// <summary>
        /// 资源更新
        /// dealcb 更新进度回调 cur_size,max_size
        /// overcb 更新完成回调 cur_size,max_size
        /// maxp 最大更新进度比例
        /// </summary>
        /// <param name="dealcb"></param>
        /// <param name="overcb"></param>
        public static IEnumerator Upgrade(Action<float, float> dealcb, Action<float, float> overcb)
        {
            yield return Instance.StartCoroutine(UpgradeAssets(dealcb, overcb));
        }
        static IEnumerator UpgradeAssets(Action<float, float> dealcb, Action<float, float> overcb)
        {
            long allsize = 0;
            float cursize = 0;
#if UNITY_EDITOR
            if (LoadType != AssetType.LocalAsset)
#endif
            {
                var bundles = Bundles.GetChangedList();
                foreach (var b in bundles)
                {
                    allsize += b.Value.bundleSize;
                }
                if (allsize > 0)
                {
                    var nbundles = new Dictionary<string, float>();

                    int Mx = 8;
                    int count = Mx;
                    long curTime = System.DateTime.UtcNow.Ticks;
                    foreach (var b in bundles)
                    {
                        count--;
                        AB.Assets.ObtainNetwork(b.Value, (bundleName, bundleProgress, bundleOver) =>
                        {
                            if (bundleOver)
                            {
                                count++;
                            }
                            bundleProgress = bundleProgress < 0 ? 0 : bundleProgress;
                            nbundles[bundleName] = bundles[bundleName].bundleSize * bundleProgress;
                            cursize = 0;
                            foreach (var nb in nbundles)
                            {
                                cursize += nb.Value;
                            }

                            dealcb?.Invoke(cursize, allsize);
                        });
                        while (count <= 0)
                        {
                            yield return new WaitForEndOfFrame();
                        }
                    }
                    while (count < Mx)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                    Debug.Log("Upgrade Assets UseTime:" + (System.DateTime.UtcNow.Ticks - curTime));
                }
            }
            overcb?.Invoke(cursize, allsize);
        }
#endregion


        public static void LoadImage(Image imgobj, string url)
        {
            LoadAsset(url, (asset) =>
            {
                if (asset.asset != null)
                {
                    if (imgobj == null)
                    {
                        return;
                    }
                    if (asset.asset is Texture2D)
                    {
                        var tex2d = asset.asset as Texture2D;
                        imgobj.sprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0.5f, 0.5f));
                    }
                    else if (asset.asset is Sprite)
                    {
                        imgobj.sprite = asset.asset as Sprite;
                    }
                    else
                    {
                        var gameObject = asset.asset as GameObject;
                        if (gameObject.GetComponent<SpriteRenderer>() != null)
                        {
                            imgobj.sprite = gameObject.GetComponent<SpriteRenderer>().sprite;
                        }
                        else if (gameObject.GetComponent<Image>() != null)
                        {
                            imgobj.sprite = gameObject.GetComponent<Image>().sprite;
                        }
                    }
                    imgobj.enabled = true;
                }
            });
        }
#if UNITY_EDITOR
        public static void FixShader(GameObject go)
        {
            if (go != null)
            {
                foreach (Transform tran in go.GetComponentsInChildren<Transform>())
                {
                    if (tran.gameObject.GetComponent<Renderer>() != null)
                    {
                        var materials = tran.gameObject.GetComponent<Renderer>().sharedMaterials;
                        foreach (var m in materials)
                        {
                            if (m != null)
                            {
                                m.shader = Shader.Find(m.shader.name);
                            }
                        }
                    }
                }
            }
        }
#endif
    }
}