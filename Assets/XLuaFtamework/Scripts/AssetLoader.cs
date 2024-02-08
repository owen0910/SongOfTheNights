using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ģ����Դ������
/// </summary>
public class AssetLoader : Singleton<AssetLoader>
{

    /// <summary>
    /// key��Ӧģ������
    /// value��Ӧģ�����е���Դ
    /// </summary>
    public Dictionary<string, Hashtable> base2Assets;

    /// <summary>
    /// ƽ̨��Ӧ�Ŀɶ���д·��
    /// key��Ӧģ������
    /// value��Ӧģ�����е���Դ
    /// </summary>
    public Dictionary<string, Hashtable> update2Assets;

    public AssetLoader()
    {
        base2Assets = new Dictionary<string, Hashtable>();

        update2Assets = new Dictionary<string, Hashtable>();
    }

    /// <summary>
    /// ����ģ���AB��Դ�����ļ�
    /// </summary>
    /// <param name="baseOrUpdate">ֻ��·�����ǿɶ���д·��</param>
    /// <param name="moduleName">ģ������</param>
    /// <param name="bundleConfigName">AB��Դ�����ļ�������</param>
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
    /// ����ModuleABConfig�����ݴ����ڴ��е���Դ����
    /// </summary>
    /// <param name="moduleABConfig"></param>
    /// <returns></returns>
    public Hashtable ConfigAssembly(ModuleABConfig moduleABConfig)
    {
        //��һ����ʱ���ֵ����洢 ���ǰ�����ֵ��AB����ϢBundleRef
        Dictionary<string, BundleRef> name2BundleRef = new Dictionary<string, BundleRef>();

        foreach (KeyValuePair<string,BundleInfo> keyValue in moduleABConfig.BundleArray)
        {
            //ȡ������AB����Ϣװ����ʱ�ֵ�
            string bundleName = keyValue.Key;

            BundleInfo bundleInfo = keyValue.Value;

            name2BundleRef[bundleName] = new BundleRef(bundleInfo);

        }

        //����һ����ϣ�� ������Դ��·������ֵ��AssetRef
        Hashtable Path2AssetRef = new Hashtable();

        for (int i = 0; i < moduleABConfig.AssetArray.Length; i++)
        {
            //��ȡ��Դ�������е���Դ
            AssetInfo assetInfo = moduleABConfig.AssetArray[i];

            //װ��һ��AssetRef����
            AssetRef assetRef = new AssetRef(assetInfo);

            //������Դ������BundleRef����
            assetRef.bundleRef = name2BundleRef[assetInfo.bundle_name];

            //������Դ������AB��
            int count = assetInfo.dependencies.Count;

            assetRef.dependencies = new BundleRef[count];

            for (int index = 0; index < count; index++)
            {
                string bundleName = assetInfo.dependencies[index];

                assetRef.dependencies[index] = name2BundleRef[bundleName];
            }

            //װ����˾ͷŵ�Path2AssetRef������

            Path2AssetRef.Add(assetInfo.asset_path, assetRef);
        }

        return Path2AssetRef;
    }

    /// <summary>
    /// ��¡һ��GameObject����
    /// </summary>
    /// <param name="moduleName">ģ�������</param>
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
    /// ����AssetRef����
    /// </summary>
    /// <typeparam name="T">Ҫ���ص���Դ����</typeparam>
    /// <param name="moduleName">ģ�������</param>
    /// <param name="assetPath">��Դ�����·��</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef<T>(string moduleName,string assetPath) where T:UnityEngine.Object
    {
#if UNITY_EDITOR
        //���������AB��
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
    /// �ڱ༭��ģʽ�¼��� AssetRef ����
    /// </summary>
    /// <typeparam name="T">Ҫ���ص���Դ����</typeparam>
    /// <param name="moduleName">ģ�������</param>
    /// <param name="assetPath">��Դ�����·��</param>
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
    /// ��AB��ģʽ�¼��� AssetRef ����
    /// </summary>
    /// <typeparam name="T">Ҫ���ص���Դ����</typeparam>
    /// <param name="moduleName">ģ�������</param>
    /// <param name="assetPath">��Դ�����·��</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Runtime<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        // �Ȳ���update·���µ��������ٲ���base·��������
        BaseOrUpdate witch = BaseOrUpdate.Update;

        //��һ����ϣ���ȡģ���µ�������Դ
        Hashtable module2AssetRef;

        if (update2Assets.TryGetValue(moduleName,out module2AssetRef)==false)
        {
            witch = BaseOrUpdate.Base;

            if (base2Assets.TryGetValue(moduleName, out module2AssetRef) == false)
            {
                Debug.LogError("δ�ҵ���Դ��Ӧ��ģ�飺moduleName " + moduleName + " assetPath " + assetPath);

                return null;
            }
        }

        AssetRef assetRef = (AssetRef)module2AssetRef[assetPath];

        if (assetRef ==null)
        {
            Debug.LogError("δ�ҵ���Դ��moduleName" + moduleName + " path" + assetPath);

            return null;
        }

        if (assetRef.asset!=null)
        {
            return assetRef;
        }

        //����assetRef������BundleRef�б�

        foreach (BundleRef onebundleRef in assetRef.dependencies)
        {
            if (onebundleRef.bundle==null)
            {
                //�Ӷ�Ӧ��·������AB��
                string bundlePath = BundlePath(witch,moduleName, onebundleRef.bundleInfo.bundle_name);
                
                onebundleRef.bundle = AssetBundle.LoadFromFile(bundlePath);
            }

            //������Ҫ���AB������Դ
            if (onebundleRef.children==null)
            {
                onebundleRef.children = new List<AssetRef>();
            }

            onebundleRef.children.Add(assetRef);
        }

        //����assetRef���ڵ��Ǹ�Bundle����

        BundleRef bundleRef = assetRef.bundleRef;

        if (bundleRef.bundle==null)
        {
            bundleRef.bundle = AssetBundle.LoadFromFile(BundlePath(witch,moduleName, bundleRef.bundleInfo.bundle_name));

        }

        //����AB����children��������Դ����Դ��children��GameObject1
        if (bundleRef.children==null)
        {
            bundleRef.children = new List<AssetRef>();
        }

        bundleRef.children.Add(assetRef);

        //��bundle����ȡasset

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
    /// ���ߺ���������ģ�����ֺ�bundle���֣�������ʵ�ʵ���Դ·��
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
    /// ������Դ���󣬲��ҽ��丳����Ϸ����GameObject
    /// </summary>
    /// <typeparam name="T">��Դ������</typeparam>
    /// <param name="moduleName">ģ�������</param>
    /// <param name="assetPath">��Դ��·��</param>
    /// <param name="gameObject">��Դ���غ�Ҫ���ص�����Ϸ����</param>
    /// <returns></returns>
    public T CreateAsset<T>(string moduleName,string assetPath,GameObject gameObject) where T:UnityEngine.Object
    {
        if (typeof(T)==typeof(GameObject)||(!string.IsNullOrEmpty(assetPath)&&assetPath.EndsWith(".prefab")))
        {
            Debug.LogError("�����Լ���GameObject���ͣ�����AssetLoader.Instance.clone");

            return null;
        }

        if (gameObject==null)
        {
            Debug.LogError("CreateAsset���봫һ��gameObject�佫Ҫ�����ص�GameObject����");

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
    /// ȫ��ж�غ���
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
                // ��������ԴassetRef�Ѿ�û�б��κ�GameObject�������ˣ���ô��assetRef�Ϳ���ж����

                if (assetRef.children.Count == 0)
                {
                    assetRef.asset = null;

                    Resources.UnloadUnusedAssets();

                    // ����assetRef���������bundle�������ϵ

                    assetRef.bundleRef.children.Remove(assetRef);

                    if (assetRef.bundleRef.children.Count==0)
                    {
                        assetRef.bundleRef.bundle.Unload(true);
                    }

                    // ����assetRef����������Щbundle�б������ϵ

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
