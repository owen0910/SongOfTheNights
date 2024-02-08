using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

public class ABEditor : MonoBehaviour
{
    /// <summary>
    /// 热更新资源的根目录
    /// </summary>
    public static string rootPath = Application.dataPath + "/GAssets";

    /// <summary>
    /// 所有需要打包的AB包信息：一个AB包对应一个AssetBundleBuild对象
    /// </summary>
    public static List<AssetBundleBuild> assetBundleBuildList = new List<AssetBundleBuild>();

    /// <summary>
    /// AB包文件的输出路径
    /// </summary>
    public static string abOutputPath = Application.streamingAssetsPath;

    /// <summary>
    /// 记录那个asset资源属于哪个AB包文件
    /// </summary>
    public static Dictionary<string, string> asset2bundle = new Dictionary<string, string>();

    /// <summary>
    /// 记录每个asset资源所依赖的AB包文件列表
    /// </summary>
    public static Dictionary<string, List<string>> asset2Dependencies = new Dictionary<string, List<string>>();


    /// <summary>
    /// 生成AB包
    /// </summary>
    [MenuItem("AB包打包工具/生成AB包")]
    public static void BuildAssetBundle()
    {
        Debug.Log("开始生成所有模块AB包");

        if (Directory.Exists(abOutputPath)==true)
        {
            Directory.Delete(abOutputPath, true);
        }

        //遍历所有模块。对所有模块分别打包

        DirectoryInfo rootDir = new DirectoryInfo(rootPath);

        //查找根目录下的一级文件夹
        DirectoryInfo[] Dirs = rootDir.GetDirectories();

        foreach (DirectoryInfo moduleDir in Dirs)
        {
            //模块名字为文件夹名
            string moduleName = moduleDir.Name;

            assetBundleBuildList.Clear();

            asset2bundle.Clear();

            asset2Dependencies.Clear();

            //开始给模块生成AB包文件
            //遍历各模块下的文件和子文件夹，进行AB包的打包
            ScanChildRireations(moduleDir);

            AssetDatabase.Refresh();

            //创建AB包输出路径文件夹
            string moduleOutPath = abOutputPath + "/" + moduleName;

            if (Directory.Exists(moduleOutPath)==true)
            {
                Directory.Delete(moduleOutPath, true);
            }

            Directory.CreateDirectory(moduleOutPath);

            // 压缩选项详解
            // BuildAssetBundleOptions.None：使用LZMA算法压缩，压缩的包更小，但是加载时间更长。使用之前需要整体解压。一旦被解压，这个包会使用LZ4重新压缩。使用资源的时候不需要整体解压。在下载的时候可以使用LZMA算法，一旦它被下载了之后，它会使用LZ4算法保存到本地上。
            // BuildAssetBundleOptions.UncompressedAssetBundle：不压缩，包大，加载快
            // BuildAssetBundleOptions.ChunkBasedCompression：使用LZ4压缩，压缩率没有LZMA高，但是我们可以加载指定资源而不用解压全部

            // 参数一: bundle文件列表的输出路径
            // 参数二：生成bundle文件列表所需要的AssetBundleBuild对象数组（用来指导Unity生成哪些bundle文件，每个文件的名字以及文件里包含哪些资源）
            // 参数三：压缩选项BuildAssetBundleOptions.None默认是LZMA算法压缩
            // 参数四：生成哪个平台的bundle文件，即目标平台
            BuildPipeline.BuildAssetBundles(moduleOutPath, assetBundleBuildList.ToArray(),
                BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            //计算AB包依赖关系
            CalculateDependencies();

            //保存模块文件并生成json配置文件
            SaveModuleABConfig(moduleName);
            
            AssetDatabase.Refresh();
                

        }

        Debug.Log("生成所有模块AB包结束");
    }

    /// <summary>
    /// 根据指定文件夹
    /// 将这个文件夹下的所有一级子文件打成一个AB包
    /// 并递归遍历这个文件夹下的所有子文件
    /// </summary>
    /// <param name="directoryInfo"></param>
    public static void ScanChildRireations(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Name.EndsWith("CSProject~"))
        {
            return;
        }
        
        //搜集当前路径下的文件，把他们打成一个AB包
        ScanCurrDirectory(directoryInfo);

        //遍历当前路径下的子文件夹
        DirectoryInfo[] dirs = directoryInfo.GetDirectories();

        foreach (DirectoryInfo info in dirs)
        {
            ScanChildRireations(info);
        }
    }

    /// <summary>
    /// 遍历当前路径下的文件 把他们打成AB包
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
            //assetName的格式类似"Assets/GAssets/Launch/Sphere.prefab" 去掉之前的Assets
            //Application.dataPath从根目录返回到Assets，去掉Assets，就是从Assets后面开始返回
            string assetName = fileInfo.FullName.Substring(Application.dataPath.Length - "Assets".Length).Replace('\\', '/');
            //保存AB包的路径，对应指向哪一个文件
            assetNames.Add(assetName);

        }


        if (assetNames.Count>0)
        {
            //从Assets/后面开始取，获取对应文件夹的名称
            string assetbundleName = directoryInfo.FullName.Substring(Application.dataPath.Length + 1).Replace('\\', '_').ToLower();
            
            //获取对应AB包文件
            AssetBundleBuild build = new AssetBundleBuild();
            
            //设置文件夹的名称为包名
            build.assetBundleName = assetbundleName;
            
            //设置路径名,包里的内容包含哪些文件
            build.assetNames = new string[assetNames.Count];
            
            //记录每个AB包下包含了哪些需要打包的文件
            for (int i = 0; i < assetNames.Count; i++)
            {
                build.assetNames[i] = assetNames[i];
                
                //向字典中记录文件路径，ab包名路径
                asset2bundle.Add(assetNames[i], assetbundleName);
            }
            
            //记录AB包
            assetBundleBuildList.Add(build);
        }
    }

    /// <summary>
    /// 计算每个资源所以来的AB包文件列表
    /// </summary>
    public static void CalculateDependencies()
    {
        foreach (string asset in asset2bundle.Keys)
        {
            //这个资源自己所在的ab包名字
            string assetBundle = asset2bundle[asset];

            //从资源文件路径中获取需要依赖的资源文件夹名称
            string[] dependencies = AssetDatabase.GetDependencies(asset);

            //用一个列表存储需要依赖的文件
            List<string> assetList = new List<string>();

            if (dependencies!=null&&dependencies.Length>0)
            {
                foreach (string oneAsset in dependencies)
                {
                    //如果这个依赖文件是文件本身或者是c#脚本就忽略
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
                    //确认下这个文件路径是否在记录文件路径和AB包的字典中
                    bool result = asset2bundle.TryGetValue(oneAsset, out string bundle);

                    if (result==true)
                    {
                        //取出这个依赖的文件在哪个AB包中,判断是不是自己本身的AB包，如果不是加入AB包依赖列表中
                        if (bundle!=assetBundle)
                        {
                            abList.Add(bundle);
                        }
                    }
                }
                
                //记录资源文件路径，和这个资源文件依赖的AB包列表
                asset2Dependencies.Add(asset, abList);
            }
        }
    }

    /// <summary>
    /// 生成模块对应的Json配置文件
    /// </summary>
    /// <param name="moduleName"></param>
    private static void SaveModuleABConfig(string moduleName)
    {
        //生成一个模块对象
        ModuleABConfig moduleABConfig = new ModuleABConfig(asset2bundle.Count);

        //记录AB包的信息
        foreach (AssetBundleBuild build in assetBundleBuildList)
        {
            //生成一个AB包对象
            BundleInfo bundleInfo = new BundleInfo();

            //设置AB包名字
            bundleInfo.bundle_name = build.assetBundleName;

            //设置AB包所包含的资源
            bundleInfo.assets = new List<string>();

            //记录AB包所包含的资源路径名列表
            foreach (string asset in build.assetNames)
            {
                bundleInfo.assets.Add(asset);
            }

            //计算一个AB包的文件CRC散列码
            //设置AB包CRC散列码文件路径
            string abFilePath = abOutputPath + "/" + moduleName + "/" + bundleInfo.bundle_name;
            
            using(FileStream stream =File.OpenRead(abFilePath))
            {

                bundleInfo.crc = AssetUtility.GetCRC32Hash(stream);
            }

            //在模块对象中新增AB包信息
            moduleABConfig.AddBundle(bundleInfo.bundle_name, bundleInfo);
        }

        //记录每个资源的依赖关系

        int assetIndex = 0;

        foreach (var item in asset2bundle)
        {
            //生成一个资源对象
            AssetInfo assetInfo = new AssetInfo();

            //设置资源路径
            assetInfo.asset_path = item.Key;

            //设置资源所在的AB包名
            assetInfo.bundle_name = item.Value;

            //设置资源依赖的包名
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

        //开始写入Json文件
        //以模块名字+json作为json文件名
        string moduleConfigName = moduleName.ToLower() + ".json";

        //设置模块json文件输出路径
        string jsonPath = abOutputPath + "/" + moduleName + "/" + moduleConfigName;

        if (File.Exists(jsonPath)==true)
        {
            File.Delete(jsonPath);
        }

        //创建json文件并销毁文件流
        File.Create(jsonPath).Dispose();

        //用json序列化工具序列化json文件
        string jsonData = LitJson.JsonMapper.ToJson(moduleABConfig);

        //写入json
        File.WriteAllText(jsonPath, ConvertJsonString(jsonData));

    }

    /// <summary>
    /// 格式化json
    /// </summary>
    /// <param name="str">输入json字符串</param>
    /// <returns>返回格式化后的字符串</returns>
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
