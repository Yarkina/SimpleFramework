using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AB
{
    public sealed class Bundles
    {
        static Dictionary<string, BundleData> s_BundleDatas = new Dictionary<string, BundleData>();
        static Dictionary<string, BundleName> s_BundleNames = new Dictionary<string, BundleName>();
        static Dictionary<string, string> s_AtlasNames = new Dictionary<string, string>();
        public static Dictionary<string, BundleData> GetChangedList()
        {
            var changes = new Dictionary<string, BundleData>();
            foreach (var bd in s_BundleDatas)
            {
                if (!bd.Value.bundleInPack && !Caching.IsVersionCached(bd.Value.bundleName, bd.Value.bundleHash128))
                {
                    changes.Add(bd.Key, bd.Value);
                }
            }
            return changes;
        }

#if UNITY_EDITOR
        public static void InitializeForEditor()
        {
            if (Assets.LoadType != AssetType.LocalAsset) return;
            s_AtlasNames.Clear();
            //Init Atlas
            if (true)
            {
                var atlases = Directory.GetFiles(Application.dataPath, "*.spriteatlas", SearchOption.AllDirectories);
                string atlasName = string.Empty;
                string bundleName = string.Empty;
                for (int i = 0; i < atlases.Length; ++i)
                {
                    var aspath = atlases[i].ToLower().Replace("\\", "/");
                    var path = aspath.Substring(Application.dataPath.Length - ("Assets").Length);
                    if (Helper.ConvertToAtlasName(path, out atlasName))
                    {
                        s_AtlasNames.Add(atlasName.ToLower(), path);
                        bundleName = Helper.ConvertToBundleName(path);
                        var deps = AssetDatabase.GetDependencies(path);
                        foreach (var dep in deps)
                        {
                            string spriteName = string.Empty;
                            if (Helper.ConvertToAtlasSpriteName(dep, out spriteName))
                            {
                                s_BundleNames.Add(dep.ToLower(), new BundleName()
                                {
                                    atlasSprite = true,
                                    bundleName = bundleName,
                                    atlasName = path,
                                    spriteName = spriteName,
                                });
                            }
                        }
                    }
                }
            }
        }
#endif

        public static BundleName GetBundleName(string assetPath){
            BundleName bName = null;
            Helper.FixTextAssetExt(ref assetPath);
            if (!s_BundleNames.TryGetValue(assetPath.ToLower(), out bName)){
#if UNITY_EDITOR
                if (Assets.LoadType == AssetType.LocalAsset)
                {
                    var tmp = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - ("Assets").Length), assetPath);
                    Helper.TrimTextAssetExt(ref tmp);
                    if (File.Exists(tmp))
                    {
                        var bundleName = Helper.ConvertToBundleName(assetPath);
                        bName = new BundleName() { bundleName = bundleName };
                        s_BundleNames.Add(assetPath.ToLower(), bName);
                    }
                }
#endif
            }
            return bName;
        }

        public static string GetAtlasPath(string atlasName)
        {
            var path = string.Empty;
            if (!s_AtlasNames.TryGetValue(atlasName.ToLower(), out path))
            {
#if UNITY_EDITOR
                if (Assets.LoadType == AssetType.LocalAsset)
                {
                    var atlases = Directory.GetFiles(Application.dataPath, "*.spriteatlas", SearchOption.AllDirectories);
                    var atlasFullName = (atlasName + ".spriteatlas").ToLower();
                    for(int i=0;i< atlases.Length;++i)
                    {
                        var aspath = atlases[i].ToLower().Replace("\\", "/");
                        if(aspath.EndsWith(atlasFullName,StringComparison.Ordinal))
                        {
                            path = aspath.Substring(Application.dataPath.Length - ("Assets").Length);
                            s_AtlasNames.Add(atlasName.ToLower(), path);
                            break;
                        }
                    }
                }
#endif
            }
            return path;
        }

        public static string[] GetBundleDependencies(string bundleName)
        {
            BundleData bd;
            if (s_BundleDatas.TryGetValue(bundleName, out bd))
            {
                return bd.bundleDependencies;
            }
            return null;
        }

        public static bool IsInternal(string bundleName){
            BundleData bd;
            if (s_BundleDatas.TryGetValue(bundleName, out bd))
            {
                return bd.bundleInPack;
            }
            return false;
        }

        public static Hash128 GetHash128(string bundleName)
        {
            BundleData bd;
            if (s_BundleDatas.TryGetValue(bundleName, out bd))
            {
                return bd.bundleHash128;
            }
            return new Hash128();
        }

        public static IEnumerator Initialize ()
		{
            bool netError = false;
            s_BundleDatas.Clear();
            s_BundleNames.Clear();
            if (true)
            {
                string netAMF = Helper.GetDataPath(Helper.BundleSaveName) + "_size";
                var www = UnityWebRequest.Get(netAMF);
                Debug.Log(netAMF);
                yield return www.SendWebRequest();
                if (!www.isNetworkError && !www.isHttpError)
                {
                    var bytes = www.downloadHandler.data;
                    var nbytes = Crypto.ZLib.UnZip(bytes);
                    var data = BundleDatas.Deserialize(nbytes);
                    foreach (var bd in data.Datas)
                    {
                        s_BundleDatas.Add(bd.Key, bd.Value);
                    }
                    string atlasName = string.Empty;
                    foreach (var kv in data.Names)
                    {
                        var key = kv.Key.ToLower();
                        if (s_BundleNames.ContainsKey(key))
                        {
                            Debug.LogError(key + " " + kv.Value + " " + s_BundleNames[key]);
                            continue;
                        }
                        s_BundleNames.Add(key, kv.Value);
                        if(Helper.ConvertToAtlasName(key,out atlasName))
                        {
                            if (s_AtlasNames.ContainsKey(atlasName.ToLower()))
                            {
                                Debug.LogError(atlasName + " - " + key + " : " + s_AtlasNames[atlasName]);
                                continue;
                            }
                            s_AtlasNames.Add(atlasName.ToLower(), key);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Get " + netAMF + " -> " + www.error);
                    netError = true;
                }
                www.Dispose();
            }

            if (netError)
            {
                var locAMF = Helper.GetInternalPath(Helper.BundleSaveName) + "_size";
                Debug.Log(locAMF);
#if UNITY_ANDROID
                var www = new WWW(locAMF);
#else
                var www = new WWW("file:///" + locAMF);
#endif
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    var bytes = www.bytes;
                    var nbytes = Crypto.ZLib.UnZip(bytes);
                    var data = BundleDatas.Deserialize(nbytes);
                    foreach (var bd in data.Datas)
                    {
                        s_BundleDatas.Add(bd.Key, bd.Value);
                    }
                    string atlasName = string.Empty;
                    foreach (var kv in data.Names)
                    {
                        var key = kv.Key.ToLower();
                        s_BundleNames.Add(key, kv.Value);
                        if (Helper.ConvertToAtlasName(key, out atlasName))
                        {
                            if (s_AtlasNames.ContainsKey(atlasName.ToLower()))
                            {
                                Debug.LogError(atlasName + " - " + key + " : " + s_AtlasNames[atlasName]);
                                continue;
                            }
                            s_AtlasNames.Add(atlasName.ToLower(), key);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Get " + locAMF + " -> " + www.error);
                }
            }

            if (true)
            {
                var srcAMF = Helper.GetInternalPath(Helper.BundleSaveName);
#if UNITY_ANDROID
                var www = new WWW(srcAMF);
#else
                var www = new WWW("file:///" + srcAMF);
#endif
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    var ab = www.assetBundle;
                    var abm = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    ab.Unload(false);
                    foreach (var bd in s_BundleDatas)
                    {
                        if (bd.Value.bundleHash128 == abm.GetAssetBundleHash(bd.Key))
                        {
                            bd.Value.bundleInPack = true;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Get " + srcAMF + " -> " + www.error);
                }
            }
		}

		public static Bundle Load (string bundleName,bool isRoot = false)
		{
            return LoadInternal (bundleName, false, isRoot);
		}

        public static Bundle LoadAsync(string bundleName,bool isRoot = false)
		{
            return LoadInternal (bundleName, true, isRoot);
		}

		public static void Unload (Bundle bundle)
		{
			bundle.Unload ();
		}
        public static bool IsCyclicDependsLoaded(string bundleName){
            return CyclicDependsLoadedBundleName.Contains(bundleName);
        }
        static List<string> CyclicDependsLoadedBundleName = new List<string>();
        static Bundle LoadInternal(string bundleName, bool asyncRequest, bool isRoot)
		{
            if(isRoot){
                CyclicDependsLoadedBundleName.Clear();
            }
            else
            {
                Debug.Log("Dep -> " + bundleName);
            }
            CyclicDependsLoadedBundleName.Add(bundleName);
            Bundle bundle = bundles.Find (obj => { return obj.name == bundleName; });
			if (bundle == null) {
				if (asyncRequest) {
                    if (IsInternal(bundleName))
                    {
                        bundle = new BundleInternalAsync(bundleName);
                    }
                    else
                    {
                        bundle = new BundleAsync(bundleName);
                    }

				} else {
                    if (IsInternal(bundleName))
                    {
                        bundle = new BundleInternal(bundleName);
                    }
                    else
                    {
                        bundle = new Bundle(bundleName);
                    }
				}
                bundle.Initialize();
				bundles.Add (bundle);
			}
			bundle.Load ();
			return bundle;
		}

		static List<Bundle> bundles = new List<Bundle> ();
		internal static void Update()
		{
			for (int i = 0; i < bundles.Count; i++) {
				var bundle = bundles [i];
				if (bundle.isDone && bundle.references <= 0) {
					bundle.Dispose ();
					bundle = null;
					bundles.RemoveAt (i);
					i--;
				}
			}
		}
	}
}