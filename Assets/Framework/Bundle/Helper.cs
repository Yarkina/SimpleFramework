using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Text;

namespace AB
{ 
	public class Helper
	{
        public const string BundleSaveName = "AssetBundles";
		public static void TraceTime (string name, System.Action action)
		{  
			var time = System.DateTime.Now.TimeOfDay.TotalSeconds;  
			if (action != null) {
				action ();
			}
			var elasped = System.DateTime.Now.TimeOfDay.TotalSeconds - time; 
			Debug.Log (string.Format (name + " elasped {0}.", elasped));   
		} 

		static string GetPlatformName()
		{
			#if UNITY_EDITOR
			return GetPlatformForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
			#else
			return GetPlatformForAssetBundles(Application.platform);
			#endif
		}
 
		private static string GetPlatformForAssetBundles(RuntimePlatform platform)
		{
			switch (platform)
			{
			case RuntimePlatform.Android:
				return "Android";
			case RuntimePlatform.IPhonePlayer:
				return "iOS";
				#if UNITY_TVOS
				case RuntimePlatform.tvOS:
				return "tvOS";
				#endif
			case RuntimePlatform.WebGLPlayer:
				return "WebGL";
			case RuntimePlatform.WindowsPlayer:
				return "Windows";
			case RuntimePlatform.OSXPlayer:
				return "OSX";
				// Add more build targets for your own.
				// If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
			default:
				return null;
			}
		}

		#if UNITY_EDITOR
		private static string GetPlatformForAssetBundles(BuildTarget target)
		{
			switch (target)
			{
			case BuildTarget.Android:
				return "Android";
		#if UNITY_TVOS
		case BuildTarget.tvOS:
		return "tvOS";
		#endif
			case BuildTarget.iOS:
				return "iOS";
			case BuildTarget.WebGL:
				return "WebGL";
				//                case BuildTarget.WebPlayer:
				//                    return "WebPlayer";
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return "Windows";
			case BuildTarget.StandaloneOSX:
				return "OSX";
				// Add more build targets for your own.
				// If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
			default:
				return null;
			}
		}

		public static void RemoveScriptingDefineSymbol (string name)
		{
			var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone).Split (';');
			if (System.Array.Exists (symbols, obj => {
				return obj.Equals (name);
			})) {
				ArrayUtility.Remove (ref symbols, name); 
			}
			PlayerSettings.SetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone, string.Join (";", symbols));
		}

		public static void AddScriptingDefineSymbol (string name)
		{
			var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone).Split (';');
			if (!System.Array.Exists (symbols, obj => {
				return obj.Equals (name);
			})) {
				ArrayUtility.Add (ref symbols, name); 
			}
			PlayerSettings.SetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone, string.Join (";", symbols));
		}
		#endif 

        #if UNITY_EDITOR
        /// <summary>
        /// AB 保存的路径
        /// </summary>
        public static string BundleSavePath { get { return "DLC/" + Application.version + "/" + Helper.GetPlatformName() + "/" + BundleSaveName; } }
        public static string BundleDlcvPath { get { return "DLC/" + Application.version + "/ver.txt"; } }
        public static string BundleElogPath { get { return "DLC/" + Application.version + "/elog.txt"; } }
#endif
        /// <summary>
        /// 获取 AB 源文件路径（网络的）
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static StringBuilder temPath = new StringBuilder();
        public static string GetDataPath(string path)
        {
            temPath.Clear();
            temPath.Append(Assets.AssetPath);
            if (!(Assets.AssetPath.EndsWith("/")))
            {
                temPath.Append("/");
            }
            temPath.Append(Helper.GetPlatformName());
            temPath.Append("/");
            temPath.Append(BundleSaveName);
#if UNITY_EDITOR
            if (Assets.LoadType == AssetType.LocalAsset || Assets.LoadType == AssetType.LocalBundle)
            {
                temPath.Clear();
                temPath.Append("file://");
                temPath.Append(Application.dataPath);
                temPath.Append("/../");
                temPath.Append(BundleSavePath);
            }
#endif
            temPath.Append("/");
            temPath.Append(path);
            return temPath.ToString();
        }

        /// <summary>
        /// 获取 AB 源文件路径（打包进安装包的）
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetInternalPath(string path)
        {
            temPath.Clear();
            temPath.Append(Application.streamingAssetsPath);
            temPath.Append("/");
            temPath.Append(BundleSaveName);
            temPath.Append("/");
            temPath.Append(path);
            return temPath.ToString();
        }

        public static string ConvertToBundleName(string assetPath,bool merge = false)
        {
            string bn = string.Empty;
            bn = assetPath.Replace(Application.dataPath, "");
            bn = bn
                .Replace('\\', '.')
                .Replace('/', '.')
                .Replace(" ", "_")
                .ToLower() + ".ab";
            if (merge)
            {
                var bns = bn.Split('.');
	            
	            // Promote bundle granularity.
                if (bns.Length >= 3)
                {
                    bn = bns[0] + "." + bns[1] + "." + bns[2] + ".merge.ab";
                }
            }
            return bn;
        }

        //这些文件打包的时候转成txt打包
        public static void FixTextAssetExt(ref string path)
        {
            if (path.EndsWith(".proto")
                || path.EndsWith(".def")
                || path.EndsWith(".tsv")
                || path.EndsWith(".tmx")
                || path.EndsWith(".lua"))
            {
                path += ".txt";
            }
        }
        public static void TrimTextAssetExt(ref string path)
        {
            if (path.EndsWith(".proto.txt")
                || path.EndsWith(".def.txt")
                || path.EndsWith(".tsv.txt")
                || path.EndsWith(".tmx.txt")
                || path.EndsWith(".lua.txt"))
            {
                path = path.Substring(0, path.Length - 4);
            }
        }
        public static bool ConvertToAtlasSpriteName(string spritePath,out string spriteName)
        {
            var bn = spritePath.ToLower();
            if (bn.EndsWith(".png", StringComparison.Ordinal)
                || bn.EndsWith(".tga", StringComparison.Ordinal)
                || bn.EndsWith(".jpg", StringComparison.Ordinal))
            {
                var sn = spritePath.Replace("\\", "");
                var idxs = sn.LastIndexOf("/");
                var idxe = sn.LastIndexOf(".");
                if (idxe - idxs > 1)
                {
                    spriteName = sn.Substring(idxs + 1, idxe - idxs - 1);
                    return true;
                }
            }
            spriteName = string.Empty;
            return false;
        }
        public static bool ConvertToAtlasName(string atlasPath, out string atlasName)
        {
            var bn = atlasPath.ToLower();
            if (bn.EndsWith(".spriteatlas", StringComparison.Ordinal))
            {
                var sn = atlasPath.Replace("\\", "");
                var idxs = sn.LastIndexOf("/");
                var idxe = sn.LastIndexOf(".spriteatlas");
                if (idxe - idxs > 1)
                {
                    atlasName = sn.Substring(idxs + 1, idxe - idxs - 1);
                    return true;
                }
            }
            atlasName = string.Empty;
            return false;
        }
        public static string LogoLoadingPrefabName
        {
            get
            {
                return "LogoLoading.prefab.ab";
            }
        }
    }
}

namespace UnityEngine
{
    public class ABTextAsset : TextAsset
    {
        public byte[] ABBytes
        {
            get;
            set;
        }
        public string ABText
        {
            get;
            set;
        }
    }
}

public static class TextAssetHelper
{
    public static byte[] GetBytes(this TextAsset asset)
    {
        if (asset is ABTextAsset)
        {
            var ab = asset as ABTextAsset;
            return ab.ABBytes;
        }
        return asset.bytes;
    }
    public static string GetText(this TextAsset asset)
    {
        if (asset is ABTextAsset)
        {
            var ab = asset as ABTextAsset;
            return ab.ABText;
        }
        return asset.text;
    }
}