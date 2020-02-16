using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace AB
{ 
	public class BuildScript : Helper
	{
		[InitializeOnLoadMethod]
		public static void Clear ()
		{
			EditorUtility.ClearProgressBar (); 
		}  

		static public string CreateAssetBundleDirectory ()
		{
			// Choose the output path according to the build target.
            string outputPath = BundleSavePath;
			if (!Directory.Exists (outputPath))
				Directory.CreateDirectory (outputPath);

			return outputPath;
		}

		public static void BuildAssetBundles (List<AssetBundleBuild> builds)
		{  
			// Choose the output path according to the build target.
			string outputPath = CreateAssetBundleDirectory ();

			var options = BuildAssetBundleOptions.ChunkBasedCompression;

			if (builds == null || builds.Count == 0) {
				//@TODO: use append hash... (Make sure pipeline works correctly with it.)
				BuildPipeline.BuildAssetBundles (outputPath, options, EditorUserBuildSettings.activeBuildTarget);
			} else {
                builds.Sort((a,b)=> string.Compare(a.assetBundleName,b.assetBundleName));
				BuildPipeline.BuildAssetBundles (outputPath, builds.ToArray(), options, EditorUserBuildSettings.activeBuildTarget);
			}

            CacAssetBundleSize();
		}

        static void CacAssetBundleSize()
        {
            var BundlePath = BundleSavePath + "/";
            var srcAMF = BundlePath + Helper.BundleSaveName;
            if (File.Exists(srcAMF))
            {
                var ab = AssetBundle.LoadFromFile(srcAMF);
                var abm = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                ab.Unload(false);
	            
	            // Create Map from asset to AssetBundle.
                var data = new BundleDatas();
                var bundles = abm.GetAllAssetBundles();
                foreach (var bundle in bundles)
                {
	                var bd = new BundleData
	                {
		                bundleInPack = false,
		                bundleName = bundle,
		                bundleHash128 = abm.GetAssetBundleHash(bundle),
		                bundleDependencies = abm.GetDirectDependencies(bundle),
		                bundleSize = new FileInfo(BundlePath + bundle).Length
	                };
	                data.Datas.Add(bundle, bd);

                    var bdm = AssetBundle.LoadFromFile(BundlePath + bundle);
                    var bdabns = bdm.GetAllAssetNames();
                    var bdabps = bdm.GetAllScenePaths();
                    bdm.Unload(true);
                    foreach(var asset in bdabns){
                        if (!data.Names.ContainsKey(asset.ToLower()))
                        {
                            data.Names.Add(asset.ToLower(), new BundleName() { bundleName = bundle });
                        }
                        if(asset.EndsWith(".spriteatlas", StringComparison.Ordinal))
                        {
                            var deps = AssetDatabase.GetDependencies(asset, false);
                            foreach (var dep in deps)
                            {
                                string spriteName = string.Empty;
                                if (Helper.ConvertToAtlasSpriteName(dep, out spriteName))
                                {
                                    data.Names[dep.ToLower()] = new BundleName() { atlasSprite = true, bundleName = bundle, atlasName = asset, spriteName = spriteName };
                                }
                            }
                        }
                    }
                    foreach (var asset in bdabps)
                    {
                        data.Names.Add(asset.ToLower(), new BundleName() { bundleName = bundle });
                    }
                }

                var bytes = BundleDatas.Serialize(data);
				var nbytes = Crypto.ZLib.Zip(bytes);
				//bytes = Crypto.UnZip(nbytes);
                //var obj = ScriptableBundleData.Deserialize(bytes);
                File.WriteAllBytes(srcAMF + "_size", nbytes);
            }
        }

		public static void BuildPlayerWithoutAssetBundles ()
		{
			var outputPath = EditorUtility.SaveFolderPanel ("Choose Location of the Built Game", "", "");
			if (outputPath.Length == 0)
				return;

			string[] levels = GetLevelsFromBuildSettings ();
			if (levels.Length == 0) {
				Debug.Log ("Nothing to build.");
				return;
			}

			string targetName = GetBuildTargetName (EditorUserBuildSettings.activeBuildTarget);
			if (targetName == null)
				return; 

			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions ();
			buildPlayerOptions.scenes = levels;
			buildPlayerOptions.locationPathName = outputPath + targetName;
			buildPlayerOptions.assetBundleManifestPath = GetAssetBundleManifestFilePath ();
			buildPlayerOptions.target = EditorUserBuildSettings.activeBuildTarget;
			buildPlayerOptions.options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
			BuildPipeline.BuildPlayer (buildPlayerOptions);
		}

		public static void BuildStandalonePlayer ()
		{
			var outputPath = EditorUtility.SaveFolderPanel ("Choose Location of the Built Game", "", "");
			if (outputPath.Length == 0)
				return;

			string[] levels = GetLevelsFromBuildSettings ();
			if (levels.Length == 0) {
				Debug.Log ("Nothing to build.");
				return;
			}

			string targetName = GetBuildTargetName (EditorUserBuildSettings.activeBuildTarget);
			if (targetName == null)
				return; 
			
            CopyAssetBundlesTo (Path.Combine (Application.streamingAssetsPath, Helper.BundleSaveName));
			AssetDatabase.Refresh ();

			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions ();
			buildPlayerOptions.scenes = levels;
			buildPlayerOptions.locationPathName = outputPath + targetName;
			buildPlayerOptions.assetBundleManifestPath = GetAssetBundleManifestFilePath ();
			buildPlayerOptions.target = EditorUserBuildSettings.activeBuildTarget;
			buildPlayerOptions.options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
			BuildPipeline.BuildPlayer (buildPlayerOptions);
		}

		public static string GetBuildTargetName (BuildTarget target)
		{
			string name = PlayerSettings.productName + "_" + PlayerSettings.bundleVersion;
			switch (target) {
			case BuildTarget.Android:
				return "/" + name + PlayerSettings.Android.bundleVersionCode + ".apk";
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return "/" + name + PlayerSettings.Android.bundleVersionCode + ".exe";
			case BuildTarget.StandaloneOSX:
				return "/" + name + ".app";
			//                case BuildTarget.WebPlayer:
			//                case BuildTarget.WebPlayerStreamed:
			case BuildTarget.WebGL:
			case BuildTarget.iOS:
				return "";
			// Add more build targets for your own.
			default:
				Debug.Log ("Target not implemented.");
				return null;
			}
		}

		static public void CopyAssetBundlesTo (string outputPath)
		{
			// Clear streaming assets folder.
			//            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
			if (!Directory.Exists (outputPath)) {
				Directory.CreateDirectory (outputPath);  
			}

			// Setup the source folder for assetbundles.
            var source = Helper.BundleSavePath;
			if (!System.IO.Directory.Exists (source))
				Debug.Log ("No assetBundle output folder, try to build the assetBundles first.");

			// Setup the destination folder for assetbundles.
            if (System.IO.Directory.Exists (outputPath))
                FileUtil.DeleteFileOrDirectory (outputPath);

            FileUtil.CopyFileOrDirectory (source, outputPath);

            AssetDatabase.Refresh();
		}

		static string[] GetLevelsFromBuildSettings ()
		{
			List<string> levels = new List<string> ();
			for (int i = 0; i < EditorBuildSettings.scenes.Length; ++i) {
				if (EditorBuildSettings.scenes [i].enabled)
					levels.Add (EditorBuildSettings.scenes [i].path);
			}

			return levels.ToArray ();
		}

		static string GetAssetBundleManifestFilePath ()
		{
            return Path.Combine (Helper.BundleSavePath, Helper.BundleSaveName) + ".manifest";
		}

	}
}