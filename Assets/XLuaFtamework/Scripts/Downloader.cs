using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 下载器 工具类
/// </summary>
public class Downloader : Singleton<Downloader>
{
    public async Task<bool> Download(ModuleConfig moduleConfig)
    {
        // 用来存放热更下来的资源的 本地路径

        string updatePath = GetUpdatePath(moduleConfig.moduleName);

        // 远程服务器上这个模块的AB资源配置文件的URL

        string configURL = GetServerURL(moduleConfig, moduleConfig.moduleName.ToLower() + ".json");

        UnityWebRequest request = UnityWebRequest.Get(configURL);

        request.downloadHandler = new DownloadHandlerFile(string.Format("{0}/{1}_temp.json", updatePath, moduleConfig.moduleName.ToLower()));

        Debug.Log("下载到本地路径: " + updatePath);

        await request.SendWebRequest();

        if (string.IsNullOrEmpty(request.error) == false)
        {
            return false;
        }

        List<BundleInfo> downloadList = await GetDownloadList(moduleConfig.moduleName);

        List<BundleInfo> remainList = await ExecuteDownload(moduleConfig, downloadList);

        if (remainList.Count > 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 执行下载行为
    /// </summary>
    /// <param name="bundleInfoList"></param>
    /// <returns>返回的List包含的是还未下载的Bundle</returns>
    private async Task<List<BundleInfo>> ExecuteDownload(ModuleConfig moduleConfig, List<BundleInfo> bundleList)
    {
        while (bundleList.Count > 0)
        {
            BundleInfo bundleInfo = bundleList[0];

            UnityWebRequest request = UnityWebRequest.Get(GetServerURL(moduleConfig, bundleInfo.bundle_name));

            string updatePath = GetUpdatePath(moduleConfig.moduleName);

            request.downloadHandler = new DownloadHandlerFile(string.Format("{0}/" + bundleInfo.bundle_name, updatePath));

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("下载资源：" + bundleInfo.bundle_name + " 成功");

                bundleList.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        return bundleList;
    }

    /// <summary>
    /// 对于给定模块，返回其所有需要下载的BundleInfo组成的List
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private async Task<List<BundleInfo>> GetDownloadList(string moduleName)
    {
        ModuleABConfig serverConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + "_temp.json");

        if (serverConfig == null)
        {
            return null;
        }

        ModuleABConfig localConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + ".json");

        // 注意：这里不用判断localConfig是否存在 本地的localConfig确实可能不存在，比如在此模块第一次热更新之前，本地update路径下啥都没有

        List<BundleInfo> diffList = CalculateDiff(moduleName, localConfig, serverConfig);

        return diffList;
    }

    /// <summary>
    /// 通过两个AB资源配置文件，对比出有差异的Bundle
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="localConfig"></param>
    /// <param name="serverConfig"></param>
    /// <returns></returns>
    private List<BundleInfo> CalculateDiff(string moduleName, ModuleABConfig localConfig, ModuleABConfig serverConfig)
    {
        List<BundleInfo> bundleList = new List<BundleInfo>();

        Dictionary<string, BundleInfo> localBundleDic = new Dictionary<string, BundleInfo>();

        if (localConfig != null)
        {
            foreach (BundleInfo bundleInfo in localConfig.BundleArray.Values)
            {
                string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

                localBundleDic.Add(uniqueId, bundleInfo);
            }
        }

        // 找到那些差异的bundle文件，放到bundleList容器中

        foreach (BundleInfo bundleInfo in serverConfig.BundleArray.Values)
        {
            string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

            if (localBundleDic.ContainsKey(uniqueId) == false)
            {
                bundleList.Add(bundleInfo);
            }
            else
            {
                localBundleDic.Remove(uniqueId);
            }
        }

        string updatePath = GetUpdatePath(moduleName);

        // 对于那些遗留在本地的无用的bundle文件，要清除，不然本地文件越积累越多

        BundleInfo[] removeList = localBundleDic.Values.ToArray();

        for (int i = removeList.Length - 1; i >= 0; i--)
        {
            BundleInfo bundleInfo = removeList[i];

            string filePath = string.Format("{0}/" + bundleInfo.bundle_name, updatePath);

            File.Delete(filePath);
        }

        // 删除旧的配置文件

        string oldFile = string.Format("{0}/{1}.json", updatePath, moduleName.ToLower());

        if (File.Exists(oldFile))
        {
            File.Delete(oldFile);
        }

        // 用新的配置文件替代之

        string newFile = string.Format("{0}/{1}_temp.json", updatePath, moduleName.ToLower());

        File.Move(newFile, oldFile);

        return bundleList;
    }

    /// <summary>
    /// 客户端给定模块的热更资源存放地址
    /// </summary>
    private string GetUpdatePath(string moduleName)
    {
        return Application.persistentDataPath + "/Bundles/" + moduleName;
    }

    /// <summary>
    /// 返回 给定模块的给定文件在服务器端的完整URL
    /// </summary>
    /// <param name="moduleConfig">模块配置对象</param>
    /// <param name="fileName">文件名字</param>
    /// <returns></returns>
    public string GetServerURL(ModuleConfig moduleConfig, string fileName)
    {
#if UNITY_ANDROID
        return string.Format("{0}/{1}/{2}", moduleConfig.DownloadURL, "Android", fileName);

#elif UNITY_IOS
        return string.Format("{0}/{1}/{2}", moduleConfig.DownloadURL, "iOS", fileName);

#elif UNITY_STANDALONE_WIN
        return string.Format("{0}/{1}/{2}", moduleConfig.DownloadURL, "StandaloneWindows64", fileName);

#endif
    }
}
