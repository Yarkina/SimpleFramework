using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AB
{
    [System.Serializable]
    public class BuildAssetFilter
    {
        public string path = string.Empty;
        public string filter = "*.prefab";
        public string abname = "";
        public bool subdirs = false; //包含子文件夹
        public bool atlas = false;//合并到一个包中
    }

    public class BuildConfig : ScriptableObject
    {
        public List<BuildAssetFilter> filters = new List<BuildAssetFilter>();
    }
}