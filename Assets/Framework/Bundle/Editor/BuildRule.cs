using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AB
{
    public abstract class BuildRule
    {
        protected static List<string> packedAssets = new List<string>();
        protected static List<AssetBundleBuild> builds = new List<AssetBundleBuild>();

        static BuildRule()
        {
        }

        static List<string> GetFilesWithoutDirectories(string prefabPath, string searchPattern,
            SearchOption searchOption)
        {
            var files = new List<string>();
            var patterns = searchPattern.Split(';');
            var orls = Directory.GetFiles(prefabPath, "*.*", searchOption);
            foreach (var p in patterns)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    if (p.Contains("*"))
                    {
                        if (p.IndexOf("*") > 0)
                        {
                            var pre = p.Substring(0, p.IndexOf("*")).ToLower();
                            var ext = p.Substring(p.LastIndexOf("*") + 1).ToLower();
                            foreach (var f in orls)
                            {
                                var fn = f.Substring(f.LastIndexOf("/") + 1).ToLower();
                                if (fn.StartsWith(pre) && fn.EndsWith(ext))
                                {
                                    files.Add(f);
                                }
                            }
                        }
                        else
                        {
                            var ext = p.Substring(p.LastIndexOf("*") + 1).ToLower();
                            foreach (var f in orls)
                            {
                                var fn = f.Substring(f.LastIndexOf("/") + 1).ToLower();
                                if (fn.EndsWith(ext))
                                {
                                    files.Add(f);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var f in orls)
                        {
                            var fn = f.ToLower();
                            var ext = p.ToLower();
                            if (fn.EndsWith(ext))
                            {
                                files.Add(f);
                            }
                        }
                    }
                }
            }

            List<string> items = new List<string>();
            foreach (var item in files)
            {
                var assetPath = item.Replace('\\', '/');
                if (!System.IO.Directory.Exists(assetPath))
                {
                    items.Add(assetPath);
                }
            }

            return items;
        }

        protected static List<string> GetFilesWithoutPacked(string searchPath, string searchPattern,
            SearchOption searchOption)
        {
            var files = GetFilesWithoutDirectories(searchPath, searchPattern, searchOption);
            var filesCount = files.Count;
            var removeAll = files.RemoveAll((string obj) => { return packedAssets.Contains(obj.ToLower()); });
            Debug.Log(string.Format("RemoveAll {0} size: {1}", removeAll, filesCount));
            return files;
        }

        public string searchPath;
        public string searchPattern;
        public SearchOption searchOption = SearchOption.AllDirectories;
        public string bundleName;

        public BuildRule(string path, string pattern, SearchOption option, string bname)
        {
            searchPath = path;
            searchPattern = pattern;
            searchOption = option;
            bundleName = bname;
        }

        public virtual void BuildAtlas() { }

        public virtual void BuildOther() { }

        public static List<AssetBundleBuild> GetBuilds(List<BuildRule> rules)
        {
            packedAssets.Clear();
            builds.Clear();

            foreach (var item in rules)
            {
                item.BuildAtlas();
            }

            foreach (var item in rules)
            {
                item.BuildOther();
            }

            EditorUtility.ClearProgressBar();

            return builds;
        }
    }

    public class BuildAssetsWithFilename : BuildRule
    {
        public BuildAssetsWithFilename(string path, string pattern, SearchOption option, string bname) : base(path,
            pattern, option, bname)
        {
        }

        public override void BuildOther()
        {
            if (!Directory.Exists(searchPath))
            {
                Debug.LogWarning("Not exist " + searchPath);
                return;
            }

            var files = GetFilesWithoutPacked(searchPath, searchPattern, searchOption);

            // Collect dependencies.
            var commonAssets = new Dictionary<string, List<string>>();
            for (var i = 0; i < files.Count; i++)
            {
                var item = files[i];
                var dependencies = AssetDatabase.GetDependencies(item);
                if (EditorUtility.DisplayCancelableProgressBar($"Collecting... [{i}/{files.Count}]", item,
                    i * 1f / files.Count))
                {
                    break;
                }

                foreach (var assetPath in dependencies)
                {
                    if (!commonAssets.ContainsKey(assetPath))
                    {
                        commonAssets[assetPath] = new List<string>();
                    }

                    if (!commonAssets[assetPath].Contains(item))
                    {
                        commonAssets[assetPath].Add(item);
                    }
                }
            }

            // Generate AssetBundleBuild items.
            for (var i = 0; i < files.Count; i++)
            {
                var item = files[i];
                if (EditorUtility.DisplayCancelableProgressBar($"Packing... [{i}/{files.Count}]",
                    item, i * 1f / files.Count))
                {
                    break;
                }

                if (packedAssets.Contains(item.ToLower()))
                {
                    continue;
                }

                var build = new AssetBundleBuild
                {
                    assetBundleName = string.IsNullOrEmpty(bundleName) ? BuildScript.ConvertToBundleName(item) : bundleName,
                    assetNames = new[]{ item }
                };
                builds.Add(build);
                packedAssets.Add(item.ToLower());
            }

            // Pack the asset which is dependent by more than one asset in an AssetBundle along.
            foreach (var item in commonAssets)
            {
                var assetPath = item.Key;
                
                // Skip CS scripts.
                if (assetPath.EndsWith(".cs"))
                {
                    continue;
                }
                
                // Skip the packed assets.
                if (packedAssets.Contains(assetPath.ToLower()))
                {
                    continue;
                }

                // Pack the common assets.
                if (item.Value.Count > 1)
                {
                    var build = new AssetBundleBuild
                    {
                        assetBundleName = BuildScript.ConvertToBundleName(assetPath, true),
                        assetNames = new[] { assetPath }
                    };
                    builds.Add(build);
                    packedAssets.Add(assetPath.ToLower());
                }
            }
        }
    }

    public class BuildAssetsSpriteAtlas : BuildRule
    {
        public BuildAssetsSpriteAtlas(string path, string pattern, SearchOption option, string bname) : base(path,
            pattern, option, bname)
        {
        }

        public override void BuildAtlas()
        {
            if (!Directory.Exists(searchPath))
            {
                Debug.LogWarning("Not exist " + searchPath);
                return;
            }

            var files = GetFilesWithoutPacked(searchPath, searchPattern, searchOption);

            // Generate AssetBundleBuild items.
            for (var i = 0; i < files.Count; i++)
            {
                var item = files[i];
                if (EditorUtility.DisplayCancelableProgressBar($"Packing... [{i}/{files.Count}]",
                    item, i * 1f / files.Count))
                {
                    break;
                }

                if (packedAssets.Contains(item.ToLower()))
                {
                    continue;
                }

                var build = new AssetBundleBuild
                {
                    assetBundleName = string.IsNullOrEmpty(bundleName) ? BuildScript.ConvertToBundleName(item) : bundleName,
                    assetNames = new[] { item }
                };
                builds.Add(build);
                packedAssets.Add(item.ToLower());
            }

            // Collect dependencies.
            for (var i = 0; i < files.Count; i++)
            {
                var item = files[i];
                var dependencies = AssetDatabase.GetDependencies(item);
                if (EditorUtility.DisplayCancelableProgressBar($"Collecting... [{i}/{files.Count}]", item,
                    i * 1f / files.Count))
                {
                    break;
                }

                foreach (var assetPath in dependencies)
                {
                    if (!packedAssets.Contains(assetPath.ToLower()))
                    {
                        packedAssets.Add(assetPath.ToLower());
                    }
                }
            }
        }
    }
}