using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AB
{
    public class Asset : IEnumerator
    {
        #region IEnumerator implementation

        public bool MoveNext()
        {
            return !isDone;
        }

        public void Reset()
        {
        }

        public object Current
        {
            get
            {
                return null;
            }
        }

        #endregion

        public int references { get; private set; }

        public string assetPath { get; protected set; }

        public string bundlePath { get; protected set; }

        public System.Type assetType { get; protected set; }

        public virtual bool isDone { get { return true; } }

        public virtual float progress { get { return 1; } }

        public Object asset { get; protected set; }

        internal Asset(string asset, string bundle, System.Type type)
        {
            assetPath = asset;
            bundlePath = bundle;
            assetType = type;
            //Debug.Log ("Load " + bundlePath + " -> " + assetPath); 
        }

        public virtual void Initialize()
        {
#if UNITY_EDITOR
            asset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, assetType);
            if (asset == null && assetType.FullName == typeof(TextAsset).FullName)
            {
                var p = assetPath;
                Helper.TrimTextAssetExt(ref p);
                asset = new ABTextAsset()
                {
                    ABBytes = System.IO.File.ReadAllBytes(p),
                    ABText = System.IO.File.ReadAllText(p),
                };
            }
#endif
        }

        protected virtual void OnDispose()
        {

        }

        internal void Dispose()
        {
            Debug.Log("Unload " + assetPath);
            asset = null;
            OnDispose();
            assetPath = null;
        }

        public void Load()
        {
            references++;
        }

        public void Unload()
        {
            if (--references < 0)
            {
                Debug.LogError("refCount < 0");
            }
        }

        List<float> delayUnloadTime = new List<float>();
        public void DelayUnload(float delay = 30.0f)
        {
            delayUnloadTime.Add(delay);
        }

        public void Update()
        {
            for (int i = 0; i < delayUnloadTime.Count; i++)
            {
                delayUnloadTime[i] -= Time.deltaTime;
                if (delayUnloadTime[i] <= 0)
                {
                    delayUnloadTime.RemoveAt(i);
                    i--;
                    Unload();
                }
            }
        }
    }

    public class BundleAsset : Asset
    {
        public Bundle request = null;

        internal BundleAsset(string asset, string bundle, System.Type type) : base(asset, bundle, type)
        {
        }

        public override void Initialize()
        {
            if (string.IsNullOrEmpty(bundlePath))
            {
                Debug.LogError("bundlePath is Null <= " + assetPath);
                return;
            }
            request = Bundles.Load(bundlePath, true);
            asset = request?.LoadAsset(assetPath, assetType);
        }

        protected override void OnDispose()
        {
            request?.Unload();
            request = null;
        }
    }

    public class BundleAssetAsync : BundleAsset
    {
        AssetBundleRequest abRequest;

        internal BundleAssetAsync(string asset, string bundle, System.Type type) : base(asset, bundle, type)
        {

        }

        protected override void OnDispose()
        {
            base.OnDispose();
            abRequest = null;
            loadState = 0;
        }

        public override void Initialize()
        {
            loadInited = true;
            if (string.IsNullOrEmpty(bundlePath))
            {
                loadState = 2;
                Debug.LogError("bundlePath is Null <= " + assetPath);
                return;
            }
            request = Bundles.LoadAsync(bundlePath, true);
        }
        bool loadInited = false;
        int loadState = 0;

        public override bool isDone
        {
            get
            {
                if (!loadInited)
                {
                    return false;
                }

                if (loadState == 2)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(request?.error))
                {
                    Debug.LogError(request?.error);
                    loadState = 2;
                    return true;
                }

                if (loadState == 1)
                {
                    if (abRequest.isDone)
                    {
                        asset = abRequest.asset;
                        loadState = 2;
                        return true;
                    }
                }
                else
                {
                    if (request.isDone)
                    {
                        abRequest = request.LoadAssetAsync(assetPath, assetType);
                        if (abRequest == null)
                        {
                            loadState = 2;
                            return true;
                        }
                        else
                        {
                            loadState = 1;
                        }
                    }
                }
                return false;

            }
        }

        public override float progress
        {
            get
            {
                if (request.error != null)
                {
                    return 1;
                }

                if (loadState == 2)
                {
                    return 1;
                }
                else
                {
                    if (loadState == 1)
                    {
                        return (abRequest.progress + request.progress) * 0.5f;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }
    }
}
