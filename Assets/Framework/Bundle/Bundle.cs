using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_2017
using UnityWebRequestAssetBundle = UnityEngine.Networking.UnityWebRequest;
#endif
namespace AB
{    
	public class Bundle
	{ 
		public int references{ get; private set; }  
		public virtual string error { get; protected set; }
		public virtual float progress { get { return 1; } } 
		public virtual bool isDone { get { return true; } } 
		public virtual AssetBundle assetBundle { get { return _assetBundle; } }  
		public string name { get; protected set; }
		protected List<Bundle> dependencies = new List<Bundle>();
        protected AssetBundle _assetBundle = null;

        internal Bundle (string bundleName) { 
            name = bundleName;
		}

		public T LoadAsset<T> (string assetName) where T:UnityEngine.Object
		{
			if (error != null) {
				return null;
			}
			return assetBundle.LoadAsset (assetName, typeof(T)) as T;
		}

		public UnityEngine.Object LoadAsset (string assetName, System.Type assetType)
		{
			if (error != null) {
				return null;
			}
            var ab = assetBundle;
            if (name.EndsWith(".unity",System.StringComparison.Ordinal))
            {
                return null;
            }

            return ab?.LoadAsset(assetName.ToLower(), assetType);
		}

        public AssetBundleRequest LoadAssetAsync (string assetName, System.Type assetType)
		{
			if (error != null) {
				return null;
			}
            var ab = assetBundle;
            if (name.EndsWith(".unity", System.StringComparison.Ordinal))
            {
                return null;
            }
            return ab?.LoadAssetAsync(assetName.ToLower(), assetType);
		}

        public void Load()
		{
			references++;   
		}

		public void Unload()
		{  
			if (--references < 0) {
                Debug.LogError ("refCount < 0");
			} 
		}

        public virtual void Initialize ()
		{
            var path = Helper.GetDataPath(name.ToLower());
            //使用了Caching的资源 AssetBundle.LoadFromFile 找不到路径，这里直接阻塞等待加载完成，
            //按照官方文档上说的，对已经Cache的资源UnityWebRequest.GetAssetBundle 和 AssetBundle.LoadFromFile 功能一样，都会立刻返回
            var _request = UnityWebRequestAssetBundle.GetAssetBundle(path, Bundles.GetHash128(name), 0);
            _request.SendWebRequest();
            while (!_request.isDone)
            {
                Thread.Yield();
            }
            _assetBundle = DownloadHandlerAssetBundle.GetContent(_request);

            _request.Dispose();

            if (_assetBundle == null) {
				error = name + " LoadFromFile failed.";
			}  
            var items = Bundles.GetBundleDependencies (name.ToLower()); 
			if (items != null && items.Length > 0) {
				for (int i = 0, I = items.Length; i < I; i++) {
					var item = items [i];
                    if (Bundles.IsCyclicDependsLoaded(item))
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("Cyclic dependence " + name);
#endif
                    }
                    else
                    {
                        dependencies.Add(Bundles.Load(item));
                    }
				}
			} 
		}

		protected virtual void OnDispose()
		{ 
			if (_assetBundle != null) { 
				_assetBundle.Unload (false);
				_assetBundle = null;
			}
		}

		public void Dispose ()
		{
            Debug.Log ("Unload " + name); 
			OnDispose (); 
			for (int i = 0, I = dependencies.Count; i < I; i++) {
				var item = dependencies [i];
				item.Unload ();
			} 
			dependencies.Clear();
		}  
	}

    public class BundleInternal : Bundle
    {
        public override void Initialize()
        {
            var path = Helper.GetInternalPath(name);
            _assetBundle = AssetBundle.LoadFromFile(path);

            if (_assetBundle == null)
            {
                error = name + " LoadFromFile failed.";
            }
            var items = Bundles.GetBundleDependencies(name.ToLower());
            if (items != null && items.Length > 0)
            {
                for (int i = 0, I = items.Length; i < I; i++)
                {
                    var item = items[i];
                    if (Bundles.IsCyclicDependsLoaded(item))
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("Cyclic dependence " + name);
#endif
                    }
                    else
                    {
                        dependencies.Add(Bundles.Load(item));
                    }
                }
            }
        }

        internal BundleInternal(string bundleName) : base(bundleName)
        {

        }
    }

    public class BundleInternalAsync : Bundle, IEnumerator
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
                return assetBundle;
            }
        }

        #endregion

        public override AssetBundle assetBundle
        {
            get
            {
                if (error != null)
                {
                    return null;
                }

                if (_assetBundle != null)
                {
                    return _assetBundle;
                }

                if (!isDone)
                {
                    return null;
                }

                if (dependencies.Count == 0)
                {
                    _assetBundle = _request.assetBundle;
                }
                else
                {
                    for (int i = 0, I = dependencies.Count; i < I; i++)
                    {
                        var item = dependencies[i];
                        if (item.assetBundle == null)
                        {
                            return null;
                        }
                    }
                    _assetBundle = _request.assetBundle;
                }
                return _assetBundle;
            }
        }

        public override float progress
        {
            get
            {
                if (error != null)
                {
                    return 1;
                }

                if (dependencies.Count == 0)
                {
                    return _request.progress;
                }

                float value = _request.progress;
                for (int i = 0, I = dependencies.Count; i < I; i++)
                {
                    var item = dependencies[i];
                    value += item.progress;
                }
                return value / (dependencies.Count + 1);
            }
        }

        public override bool isDone
        {
            get
            {
                if (error != null)
                {
                    return true;
                }

                if (_request == null)
                {
                    return true;
                }

                if (dependencies.Count == 0)
                {
                    return _request.isDone;
                }

                for (int i = 0, I = dependencies.Count; i < I; i++)
                {
                    var item = dependencies[i];
                    if (item.error != null)
                    {
                        error = "Falied to load Dependencies " + item + " : " + item.error;
                        return true;
                    }
                    if (!item.isDone)
                    {
                        return false;
                    }
                }
                return _request.isDone;
            }
        }
        AssetBundleCreateRequest _request;
        public override void Initialize()
        {
            var path = Helper.GetInternalPath(name.ToLower());
            _request = AssetBundle.LoadFromFileAsync(path);
            if (_request == null)
            {
                error = name + " LoadFromFileAsync falied.";
            }
            var items = Bundles.GetBundleDependencies(name.ToLower());
            if (items != null && items.Length > 0)
            {
                for (int i = 0, I = items.Length; i < I; i++)
                {
                    var item = items[i];
                    if (Bundles.IsCyclicDependsLoaded(item))
                    {
                        Debug.LogWarning("Cyclic dependence " + name);
                    }
                    else
                    {
                        dependencies.Add(Bundles.LoadAsync(item));
                    }
                }
            }
        }

        protected override void OnDispose()
        {
            _request = null;
            base.OnDispose();
        }

        internal BundleInternalAsync(string bundleName) : base(bundleName)
        {

        }
    }

    public class BundleAsync : Bundle, IEnumerator
	{ 
		#region IEnumerator implementation

		public bool MoveNext ()
		{
			return !isDone;
		}

		public void Reset ()
		{ 
		}

		public object Current {
			get {
				return assetBundle;
			}
		}

		#endregion

		public override AssetBundle assetBundle { 
			get {  
				if (error != null) {
					return null;
				}

                if(_assetBundle != null){
                    return _assetBundle;
                }

                if(!isDone){
                    return null;
                }

                if (_request.error != null){
                    Debug.LogError(name);
                    Debug.LogError(_request.error);
                    return null;
                }

                if (dependencies.Count == 0)
                {
                    _assetBundle = DownloadHandlerAssetBundle.GetContent(_request);
                }
                else
                {
                    for (int i = 0, I = dependencies.Count; i < I; i++)
                    {
                        var item = dependencies[i];
                        if (item.assetBundle == null)
                        {
                            return null;
                        }
                    }
                    _assetBundle = DownloadHandlerAssetBundle.GetContent(_request);
                }
                return _assetBundle;
			}
		} 

		public override float progress {
			get {  
				if (error != null) {
					return 1;
				}

				if (dependencies.Count == 0) {
                    return _request.downloadProgress;  
				} 

                float value = _request.downloadProgress;
				for (int i = 0, I = dependencies.Count; i < I; i++) {
					var item = dependencies [i];
					value += item.progress;
				}  
				return value / (dependencies.Count + 1); 
			}
		}

		public override bool isDone {
			get { 
				if (error != null) {
					return true;
				}

                if(_request == null){
                    return true;
                }

				if (dependencies.Count == 0) {
					return _request.isDone;  
				}

				for (int i = 0, I = dependencies.Count; i < I; i++) {
					var item = dependencies [i];
					if (item.error != null) {
                        error = "Falied to load Dependencies " + item + " : " + item.error;
						return true;
					} 
					if (!item.isDone) {
						return false;
					}
				}   
				return _request.isDone;
			}
		}

        UnityWebRequest _request;
        public override void Initialize ()
		{
            var path = Helper.GetDataPath(name.ToLower());
            if (Bundles.IsInternal(name))
            {
                path = "file:///" + Helper.GetInternalPath(name);
            }
            _request = UnityWebRequestAssetBundle.GetAssetBundle(path, Bundles.GetHash128(name), 0);
            _request.SendWebRequest();
			if (_request == null) {
				error = name + " LoadFromFileAsync falied.";
			}
            var items = Bundles.GetBundleDependencies(name.ToLower());
            if (items != null && items.Length > 0)
            {
                for (int i = 0, I = items.Length; i < I; i++)
                {
                    var item = items[i];
                    if (Bundles.IsCyclicDependsLoaded(item))
                    {
                        Debug.LogWarning("Cyclic dependence " + name);
                    }
                    else
                    {
                        dependencies.Add(Bundles.LoadAsync(item));
                    }
                }
            }
		}

		protected override void OnDispose ()
		{
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }
            base.OnDispose();
		}

        internal BundleAsync (string bundleName) : base (bundleName)
		{ 
			 
		} 
	} 
}
