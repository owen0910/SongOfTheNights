using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

public class ABEditor : MonoBehaviour
{
    /// <summary>
    /// �ȸ�����Դ�ĸ�Ŀ¼
    /// </summary>
    public static string rootPath = Application.dataPath + "/GAssets";

    /// <summary>
    /// ������Ҫ�����AB����Ϣ��һ��AB����Ӧһ��AssetBundleBuild����
    /// </summary>
    public static List<AssetBundleBuild> assetBundleBuildList = new List<AssetBundleBuild>();

    /// <summary>
    /// AB���ļ������·��
    /// </summary>
    public static string abOutputPath = Application.streamingAssetsPath;

    /// <summary>
    /// ��¼�Ǹ�asset��Դ�����ĸ�AB���ļ�
    /// </summary>
    public static Dictionary<string, string> asset2bundle = new Dictionary<string, string>();

    /// <summary>
    /// ��¼ÿ��asset��Դ��������AB���ļ��б�
    /// </summary>
    public static Dictionary<string, List<string>> asset2Dependencies = new Dictionary<string, List<string>>();


    /// <summary>
    /// ����AB��
    /// </summary>
    [MenuItem("AB���������/����AB��")]
    public static void BuildAssetBundle()
    {
        Debug.Log("��ʼ��������ģ��AB��");

        if (Directory.Exists(abOutputPath)==true)
        {
            Directory.Delete(abOutputPath, true);
        }

        //��������ģ�顣������ģ��ֱ���

        DirectoryInfo rootDir = new DirectoryInfo(rootPath);

        //���Ҹ�Ŀ¼�µ�һ���ļ���
        DirectoryInfo[] Dirs = rootDir.GetDirectories();

        foreach (DirectoryInfo moduleDir in Dirs)
        {
            //ģ������Ϊ�ļ�����
            string moduleName = moduleDir.Name;

            assetBundleBuildList.Clear();

            asset2bundle.Clear();

            asset2Dependencies.Clear();

            //��ʼ��ģ������AB���ļ�
            //������ģ���µ��ļ������ļ��У�����AB���Ĵ��
            ScanChildRireations(moduleDir);

            AssetDatabase.Refresh();

            //����AB�����·���ļ���
            string moduleOutPath = abOutputPath + "/" + moduleName;

            if (Directory.Exists(moduleOutPath)==true)
            {
                Directory.Delete(moduleOutPath, true);
            }

            Directory.CreateDirectory(moduleOutPath);

            // ѹ��ѡ�����
            // BuildAssetBundleOptions.None��ʹ��LZMA�㷨ѹ����ѹ���İ���С�����Ǽ���ʱ�������ʹ��֮ǰ��Ҫ�����ѹ��һ������ѹ���������ʹ��LZ4����ѹ����ʹ����Դ��ʱ����Ҫ�����ѹ�������ص�ʱ�����ʹ��LZMA�㷨��һ������������֮������ʹ��LZ4�㷨���浽�����ϡ�
            // BuildAssetBundleOptions.UncompressedAssetBundle����ѹ�������󣬼��ؿ�
            // BuildAssetBundleOptions.ChunkBasedCompression��ʹ��LZ4ѹ����ѹ����û��LZMA�ߣ��������ǿ��Լ���ָ����Դ�����ý�ѹȫ��

            // ����һ: bundle�ļ��б�����·��
            // ������������bundle�ļ��б�����Ҫ��AssetBundleBuild�������飨����ָ��Unity������Щbundle�ļ���ÿ���ļ��������Լ��ļ��������Щ��Դ��
            // ��������ѹ��ѡ��BuildAssetBundleOptions.NoneĬ����LZMA�㷨ѹ��
            // �����ģ������ĸ�ƽ̨��bundle�ļ�����Ŀ��ƽ̨
            BuildPipeline.BuildAssetBundles(moduleOutPath, assetBundleBuildList.ToArray(),
                BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            //����AB��������ϵ
            CalculateDependencies();

            //����ģ���ļ�������json�����ļ�
            SaveModuleABConfig(moduleName);
            
            AssetDatabase.Refresh();
                

        }

        Debug.Log("��������ģ��AB������");
    }

    /// <summary>
    /// ����ָ���ļ���
    /// ������ļ����µ�����һ�����ļ����һ��AB��
    /// ���ݹ��������ļ����µ��������ļ�
    /// </summary>
    /// <param name="directoryInfo"></param>
    public static void ScanChildRireations(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Name.EndsWith("CSProject~"))
        {
            return;
        }
        
        //�Ѽ���ǰ·���µ��ļ��������Ǵ��һ��AB��
        ScanCurrDirectory(directoryInfo);

        //������ǰ·���µ����ļ���
        DirectoryInfo[] dirs = directoryInfo.GetDirectories();

        foreach (DirectoryInfo info in dirs)
        {
            ScanChildRireations(info);
        }
    }

    /// <summary>
    /// ������ǰ·���µ��ļ� �����Ǵ��AB��
    /// </summary>
    /// <param name="directoryInfo"></param>
    private static void ScanCurrDirectory(DirectoryInfo directoryInfo)
    {
        List<string> assetNames = new List<string>();

        FileInfo[] fileInfoList = directoryInfo.GetFiles();

        foreach (FileInfo fileInfo in fileInfoList)
        {
            if (fileInfo.FullName.EndsWith(".meta"))
            {
                continue;
            }
            //assetName�ĸ�ʽ����"Assets/GAssets/Launch/Sphere.prefab" ȥ��֮ǰ��Assets
            //Application.dataPath�Ӹ�Ŀ¼���ص�Assets��ȥ��Assets�����Ǵ�Assets���濪ʼ����
            string assetName = fileInfo.FullName.Substring(Application.dataPath.Length - "Assets".Length).Replace('\\', '/');
            //����AB����·������Ӧָ����һ���ļ�
            assetNames.Add(assetName);

        }


        if (assetNames.Count>0)
        {
            //��Assets/���濪ʼȡ����ȡ��Ӧ�ļ��е�����
            string assetbundleName = directoryInfo.FullName.Substring(Application.dataPath.Length + 1).Replace('\\', '_').ToLower();
            
            //��ȡ��ӦAB���ļ�
            AssetBundleBuild build = new AssetBundleBuild();
            
            //�����ļ��е�����Ϊ����
            build.assetBundleName = assetbundleName;
            
            //����·����,��������ݰ�����Щ�ļ�
            build.assetNames = new string[assetNames.Count];
            
            //��¼ÿ��AB���°�������Щ��Ҫ������ļ�
            for (int i = 0; i < assetNames.Count; i++)
            {
                build.assetNames[i] = assetNames[i];
                
                //���ֵ��м�¼�ļ�·����ab����·��
                asset2bundle.Add(assetNames[i], assetbundleName);
            }
            
            //��¼AB��
            assetBundleBuildList.Add(build);
        }
    }

    /// <summary>
    /// ����ÿ����Դ��������AB���ļ��б�
    /// </summary>
    public static void CalculateDependencies()
    {
        foreach (string asset in asset2bundle.Keys)
        {
            //�����Դ�Լ����ڵ�ab������
            string assetBundle = asset2bundle[asset];

            //����Դ�ļ�·���л�ȡ��Ҫ��������Դ�ļ�������
            string[] dependencies = AssetDatabase.GetDependencies(asset);

            //��һ���б�洢��Ҫ�������ļ�
            List<string> assetList = new List<string>();

            if (dependencies!=null&&dependencies.Length>0)
            {
                foreach (string oneAsset in dependencies)
                {
                    //�����������ļ����ļ����������c#�ű��ͺ���
                    if (oneAsset==asset||oneAsset.EndsWith(".cs"))
                    {
                        continue;
                    }

                    assetList.Add(oneAsset);
                }
            }

            if (assetList.Count>0)
            {
                List<string> abList = new List<string>();

                foreach (string oneAsset in assetList)
                {
                    //ȷ��������ļ�·���Ƿ��ڼ�¼�ļ�·����AB�����ֵ���
                    bool result = asset2bundle.TryGetValue(oneAsset, out string bundle);

                    if (result==true)
                    {
                        //ȡ������������ļ����ĸ�AB����,�ж��ǲ����Լ������AB����������Ǽ���AB�������б���
                        if (bundle!=assetBundle)
                        {
                            abList.Add(bundle);
                        }
                    }
                }
                
                //��¼��Դ�ļ�·�����������Դ�ļ�������AB���б�
                asset2Dependencies.Add(asset, abList);
            }
        }
    }

    /// <summary>
    /// ����ģ���Ӧ��Json�����ļ�
    /// </summary>
    /// <param name="moduleName"></param>
    private static void SaveModuleABConfig(string moduleName)
    {
        //����һ��ģ�����
        ModuleABConfig moduleABConfig = new ModuleABConfig(asset2bundle.Count);

        //��¼AB������Ϣ
        foreach (AssetBundleBuild build in assetBundleBuildList)
        {
            //����һ��AB������
            BundleInfo bundleInfo = new BundleInfo();

            //����AB������
            bundleInfo.bundle_name = build.assetBundleName;

            //����AB������������Դ
            bundleInfo.assets = new List<string>();

            //��¼AB������������Դ·�����б�
            foreach (string asset in build.assetNames)
            {
                bundleInfo.assets.Add(asset);
            }

            //����һ��AB�����ļ�CRCɢ����
            //����AB��CRCɢ�����ļ�·��
            string abFilePath = abOutputPath + "/" + moduleName + "/" + bundleInfo.bundle_name;
            
            using(FileStream stream =File.OpenRead(abFilePath))
            {

                bundleInfo.crc = AssetUtility.GetCRC32Hash(stream);
            }

            //��ģ�����������AB����Ϣ
            moduleABConfig.AddBundle(bundleInfo.bundle_name, bundleInfo);
        }

        //��¼ÿ����Դ��������ϵ

        int assetIndex = 0;

        foreach (var item in asset2bundle)
        {
            //����һ����Դ����
            AssetInfo assetInfo = new AssetInfo();

            //������Դ·��
            assetInfo.asset_path = item.Key;

            //������Դ���ڵ�AB����
            assetInfo.bundle_name = item.Value;

            //������Դ�����İ���
            assetInfo.dependencies = new List<string>();

            bool result = asset2Dependencies.TryGetValue(item.Key, out List<string> dependencies);

            if (result==true)
            {
                for (int i = 0; i < dependencies.Count; i++)
                {
                    string bundleName = dependencies[i];

                    assetInfo.dependencies.Add(bundleName);
                }
            }

            moduleABConfig.AddAsset(assetIndex, assetInfo);

            assetIndex++;

        }

        //��ʼд��Json�ļ�
        //��ģ������+json��Ϊjson�ļ���
        string moduleConfigName = moduleName.ToLower() + ".json";

        //����ģ��json�ļ����·��
        string jsonPath = abOutputPath + "/" + moduleName + "/" + moduleConfigName;

        if (File.Exists(jsonPath)==true)
        {
            File.Delete(jsonPath);
        }

        //����json�ļ��������ļ���
        File.Create(jsonPath).Dispose();

        //��json���л��������л�json�ļ�
        string jsonData = LitJson.JsonMapper.ToJson(moduleABConfig);

        //д��json
        File.WriteAllText(jsonPath, ConvertJsonString(jsonData));

    }

    /// <summary>
    /// ��ʽ��json
    /// </summary>
    /// <param name="str">����json�ַ���</param>
    /// <returns>���ظ�ʽ������ַ���</returns>
    private static string ConvertJsonString(string str)
    {
        JsonSerializer serializer = new JsonSerializer();

        TextReader tr = new StringReader(str);

        JsonTextReader jtr = new JsonTextReader(tr);

        object obj = serializer.Deserialize(jtr);
        if (obj != null)
        {
            StringWriter textWriter = new StringWriter();

            JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
            {
                Formatting = Formatting.Indented,

                Indentation = 4,

                IndentChar = ' '
            };

            serializer.Serialize(jsonWriter, obj);

            return textWriter.ToString();
        }
        else
        {
            return str;
        }
    }

}
