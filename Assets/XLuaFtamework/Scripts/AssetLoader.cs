using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 模块资源加载器
/// </summary>
public class AssetLoader : Singleton<AssetLoader>
{

    /// <summary>
    /// key对应模块名字
    /// value对应模块所有的资源
    /// </summary>
    public Dictionary<string, Hashtable> base2Assets;

    /// <summary>
    /// 平台对应的可读可写路径
    /// key对应模块名字
    /// value对应模块所有的资源
    /// </summary>
    public Dictionary<string, Hashtable> update2Assets;

    public AssetLoader()
    {
        base2Assets = new Dictionary<string, Hashtable>();

        update2Assets = new Dictionary<string, Hashtable>();
    }

    /// <summary>
    /// 加载模块的AB资源配置文件
    /// </summary>
    /// <param name="baseOrUpdate">只读路径还是可读可写路径</param>
    /// <param name="moduleName">模块名字</param>
    /// <param name="bundleConfigName">AB资源配置文件的名字</param>
    /// <returns></returns>
    public async Task<ModuleABConfig> LoadAssetBundleConfig(BaseOrUpdate baseOrUpdate, string moduleName, string bundleConfigName)
    {
        string url = BundlePath(baseOrUpdate, moduleName, bundleConfigName);

        UnityWebRequest request = UnityWebRequest.Get(url);

        await request.SendWebRequest();

        if (string.IsNullOrEmpty(request.error) == true)
        {
            return JsonMapper.ToObject<ModuleABConfig>(request.downloadHandler.text);
        }

        return null;
    }

    
    /// <summary>
    /// 根据ModuleABConfig的内容创建内存中的资源容器
    /// </summary>
    /// <param name="moduleABConfig"></param>
    /// <returns></returns>
    public Hashtable ConfigAssembly(ModuleABConfig moduleABConfig)
    {
        //建一个临时的字典来存储 键是包名，值是AB包信息BundleRef
        Dictionary<string, BundleRef> name2BundleRef = new Dictionary<string, BundleRef>();

        foreach (KeyValuePair<string,BundleInfo> keyValue in moduleABConfig.BundleArray)
        {
            //取出包名AB包信息装入临时字典
            string bundleName = keyValue.Key;

            BundleInfo bundleInfo = keyValue.Value;

            name2BundleRef[bundleName] = new BundleRef(bundleInfo);

        }

        //建立一个哈希表 键是资源的路径名，值是AssetRef
        Hashtable Path2AssetRef = new Hashtable();

        for (int i = 0; i < moduleABConfig.AssetArray.Length; i++)
        {
            //获取资源类数组中的资源
            AssetInfo assetInfo = moduleABConfig.AssetArray[i];

            //装配一个AssetRef对象
            AssetRef assetRef = new AssetRef(assetInfo);

            //设置资源所属的BundleRef对象
            assetRef.bundleRef = name2BundleRef[assetInfo.bundle_name];

            //设置资源依赖的AB包
            int count = assetInfo.dependencies.Count;

            assetRef.dependencies = new BundleRef[count];

            for (int index = 0; index < count; index++)
            {
                string bundleName = assetInfo.dependencies[index];

                assetRef.dependencies[index] = name2BundleRef[bundleName];
            }

            //装配好了就放到Path2AssetRef容器中

            Path2AssetRef.Add(assetInfo.asset_path, assetRef);
        }

        return Path2AssetRef;
    }

    /// <summary>
    /// 克隆一个GameObject对象
    /// </summary>
    /// <param name="moduleName">模块的名字</param>
    /// <param name="path"></param>
    /// <returns></returns>
    public GameObject Clone(string moduleName,string path)
    {
        AssetRef assetRef = LoadAssetRef<GameObject>(moduleName, path);

        if (assetRef==null||assetRef.asset==null)
        {
            return null;
        }

        GameObject gameObject = UnityEngine.Object.Instantiate(assetRef.asset) as GameObject;

        if (assetRef.children==null)
        {
            assetRef.children = new List<GameObject>();
        }

        assetRef.children.Add(gameObject);

        return gameObject;
    }

    /// <summary>
    /// 加载AssetRef对象
    /// </summary>
    /// <typeparam name="T">要加载的资源类型</typeparam>
    /// <param name="moduleName">模块的名字</param>
    /// <param name="assetPath">资源的相对路径</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef<T>(string moduleName,string assetPath) where T:UnityEngine.Object
    {
#if UNITY_EDITOR
        //如果不加载AB包
        if (GlobalConfig.BundleMode==false)
        {
            return LoadAssetRef_Editor<T>(moduleName, assetPath);
        }

        else
        {
            return LoadAssetRef_Runtime<T>(moduleName, assetPath);
        }
#else
            return LoadAssetRef_Runtime<T>(moduleName, assetPath);
#endif
    }

    /// <summary>
    /// 在编辑器模式下加载 AssetRef 对象
    /// </summary>
    /// <typeparam name="T">要加载的资源类型</typeparam>
    /// <param name="moduleName">模块的名字</param>
    /// <param name="assetPath">资源的相对路径</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Editor<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        AssetRef assetRef = new AssetRef(null);

        assetRef.asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        return assetRef;
#else
        return null;
#endif
    }

    /// <summary>
    /// 在AB包模式下加载 AssetRef 对象
    /// </summary>
    /// <typeparam name="T">要加载的资源类型</typeparam>
    /// <param name="moduleName">模块的名字</param>
    /// <param name="assetPath">资源的相对路径</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Runtime<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        // 先查找update路径下的容器，再查找base路径下容器
        BaseOrUpdate witch = BaseOrUpdate.Update;

        //用一个哈希表获取模块下的所有资源
        Hashtable module2AssetRef;

        if (update2Assets.TryGetValue(moduleName,out module2AssetRef)==false)
        {
            witch = BaseOrUpdate.Base;

            if (base2Assets.TryGetValue(moduleName, out module2AssetRef) == false)
            {
                Debug.LogError("未找到资源对应的模块：moduleName " + moduleName + " assetPath " + assetPath);

                return null;
            }
        }

        AssetRef assetRef = (AssetRef)module2AssetRef[assetPath];

        if (assetRef ==null)
        {
            Debug.LogError("未找到资源：moduleName" + moduleName + " path" + assetPath);

            return null;
        }

        if (assetRef.asset!=null)
        {
            return assetRef;
        }

        //处理assetRef依赖的BundleRef列表

        foreach (BundleRef onebundleRef in assetRef.dependencies)
        {
            if (onebundleRef.bundle==null)
            {
                //从对应的路径加载AB包
                string bundlePath = BundlePath(witch,moduleName, onebundleRef.bundleInfo.bundle_name);
                
                onebundleRef.bundle = AssetBundle.LoadFromFile(bundlePath);
            }

            //加入需要这个AB包的资源
            if (onebundleRef.children==null)
            {
                onebundleRef.children = new List<AssetRef>();
            }

            onebundleRef.children.Add(assetRef);
        }

        //处理assetRef属于的那个Bundle对象

        BundleRef bundleRef = assetRef.bundleRef;

        if (bundleRef.bundle==null)
        {
            bundleRef.bundle = AssetBundle.LoadFromFile(BundlePath(witch,moduleName, bundleRef.bundleInfo.bundle_name));

        }

        //加入AB包的children，就是资源，资源的children是GameObject1
        if (bundleRef.children==null)
        {
            bundleRef.children = new List<AssetRef>();
        }

        bundleRef.children.Add(assetRef);

        //从bundle中提取asset

        assetRef.asset = assetRef.bundleRef.bundle.LoadAsset<T>(assetRef.assetInfo.asset_path);

        if (typeof(T) == typeof(GameObject) && assetRef.assetInfo.asset_path.EndsWith(".prefab"))
        {
            assetRef.isGameObject = true;
        }
        else
        {
            assetRef.isGameObject = false;
        }

        return assetRef;
    }

    /// <summary>
    /// 工具函数，根据模块名字和bundle名字，返回其实际的资源路径
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="bundleName"></param>
    /// <returns></returns>
    private string BundlePath(BaseOrUpdate baseOrUpdate,string moduleName,string bundleName)
    {
        if (baseOrUpdate==BaseOrUpdate.Update)
        {
            return Application.persistentDataPath + "/Bundles/" + moduleName + "/" + bundleName;
        }
        else
        {
            return Application.streamingAssetsPath + "/" + moduleName + "/" + bundleName;
        }
       
    }

    /// <summary>
    /// 创建资源对象，并且将其赋予游戏对象GameObject
    /// </summary>
    /// <typeparam name="T">资源的类型</typeparam>
    /// <param name="moduleName">模块的名字</param>
    /// <param name="assetPath">资源的路径</param>
    /// <param name="gameObject">资源加载后要挂载到的游戏对象</param>
    /// <returns></returns>
    public T CreateAsset<T>(string moduleName,string assetPath,GameObject gameObject) where T:UnityEngine.Object
    {
        if (typeof(T)==typeof(GameObject)||(!string.IsNullOrEmpty(assetPath)&&assetPath.EndsWith(".prefab")))
        {
            Debug.LogError("不可以加载GameObject类型，请用AssetLoader.Instance.clone");

            return null;
        }

        if (gameObject==null)
        {
            Debug.LogError("CreateAsset必须传一个gameObject其将要被挂载的GameObject对象！");

            return null;
        }

        AssetRef assetRef = LoadAssetRef<T>(moduleName, assetPath);

        if (assetRef==null||assetRef.asset==null)
        {
            return null;
        }

        if (assetRef.children==null)
        {
            assetRef.children = new List<GameObject>();
        }

        assetRef.children.Add(gameObject);

        return assetRef.asset as T;

    }

    /// <summary>
    /// 全局卸载函数
    /// </summary>
    /// <param name="module2Assets"></param>
    public void Unload(Dictionary<string,Hashtable> module2Assets)
    {
        foreach (string moduleName in module2Assets.Keys)
        {
            Hashtable Path2AssetRef = module2Assets[moduleName];

            if (Path2AssetRef==null)
            {
                continue;
            }

            foreach (AssetRef assetRef in Path2AssetRef.Values)
            {

                if (assetRef.children==null||assetRef.children.Count==0)
                {
                    continue;
                }

                for (int i = assetRef.children.Count-1; i >=0; i--)
                {
                    GameObject go = assetRef.children[i];

                    if (go==null)
                    {
                        assetRef.children.RemoveAt(i);
                    }
                }
                // 如果这个资源assetRef已经没有被任何GameObject所依赖了，那么此assetRef就可以卸载了

                if (assetRef.children.Count == 0)
                {
                    assetRef.asset = null;

                    Resources.UnloadUnusedAssets();

                    // 对于assetRef所属的这个bundle，解除关系

                    assetRef.bundleRef.children.Remove(assetRef);

                    if (assetRef.bundleRef.children.Count==0)
                    {
                        assetRef.bundleRef.bundle.Unload(true);
                    }

                    // 对于assetRef所依赖的那些bundle列表，解除关系

                    foreach (BundleRef bundleRef in assetRef.dependencies)
                    {
                        bundleRef.children.Remove(assetRef);

                        if (bundleRef.children.Count==0)
                        {
                            bundleRef.bundle.Unload(true);
                        }
                    }

                }
            }
        }
    }
}
