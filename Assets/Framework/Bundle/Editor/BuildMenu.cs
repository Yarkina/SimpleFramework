using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO; 
using System.Linq;
using UnityEngine.UI;


namespace AB
{  
	public class BuildMenu
	{
        [MenuItem("Bundle/Build Window")]
        static void BuildWindow()
        {
            if (EditorApplication.isCompiling)
            {
                return;
            }

            EditorWindow.GetWindowWithRect<BuildPanel>(new Rect(0, 0, 900, 600), true, "Bundle");
        }
        [MenuItem("Bundle/Build AssetBundles", priority = 200)]
        public static void BuildAssetBundles()
        {
            BuildScript.BuildAssetBundles(BuildPanel.GetAssetBundleBuildList());
        }
        [MenuItem ("Bundle/Copy AssetBundles to StreamingAssets")]  
		public static void CopyAssetBundlesToStreamingAssets ()
		{        
            BuildScript.CopyAssetBundlesTo (Path.Combine (Application.streamingAssetsPath, Helper.BundleSaveName));
            AssetDatabase.Refresh();
		}  

        [MenuItem ("Bundle/Build Player")]  
		public static void BuildPlayer ()
		{
			if (EditorApplication.isCompiling) {
				return;
			}  
			BuildScript.BuildStandalonePlayer ();
		}

        [MenuItem("Bundle/Remove AssetBundle Name")]
        static void RemoveAssetBundleName()
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
            var nms = AssetDatabase.GetAllAssetBundleNames();
            foreach (var nm in nms)
            {
                AssetDatabase.RemoveAssetBundleName(nm, true);
            }

            AssetDatabase.Refresh();
        }


    }
}