using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ������ ������
/// </summary>
public class Downloader : Singleton<Downloader>
{
    public async Task<bool> Download(ModuleConfig moduleConfig)
    {
        // ��������ȸ���������Դ�� ����·��

        string updatePath = GetUpdatePath(moduleConfig.moduleName);

        // Զ�̷����������ģ���AB��Դ�����ļ���URL

        string configURL = GetServerURL(moduleConfig, moduleConfig.moduleName.ToLower() + ".json");

        UnityWebRequest request = UnityWebRequest.Get(configURL);

        request.downloadHandler = new DownloadHandlerFile(string.Format("{0}/{1}_temp.json", updatePath, moduleConfig.moduleName.ToLower()));

        Debug.Log("���ص�����·��: " + updatePath);

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
    /// ִ��������Ϊ
    /// </summary>
    /// <param name="bundleInfoList"></param>
    /// <returns>���ص�List�������ǻ�δ���ص�Bundle</returns>
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
                Debug.Log("������Դ��" + bundleInfo.bundle_name + " �ɹ�");

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
    /// ���ڸ���ģ�飬������������Ҫ���ص�BundleInfo��ɵ�List
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

        // ע�⣺���ﲻ���ж�localConfig�Ƿ���� ���ص�localConfigȷʵ���ܲ����ڣ������ڴ�ģ���һ���ȸ���֮ǰ������update·����ɶ��û��

        List<BundleInfo> diffList = CalculateDiff(moduleName, localConfig, serverConfig);

        return diffList;
    }

    /// <summary>
    /// ͨ������AB��Դ�����ļ����Աȳ��в����Bundle
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

        // �ҵ���Щ�����bundle�ļ����ŵ�bundleList������

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

        // ������Щ�����ڱ��ص����õ�bundle�ļ���Ҫ�������Ȼ�����ļ�Խ����Խ��

        BundleInfo[] removeList = localBundleDic.Values.ToArray();

        for (int i = removeList.Length - 1; i >= 0; i--)
        {
            BundleInfo bundleInfo = removeList[i];

            string filePath = string.Format("{0}/" + bundleInfo.bundle_name, updatePath);

            File.Delete(filePath);
        }

        // ɾ���ɵ������ļ�

        string oldFile = string.Format("{0}/{1}.json", updatePath, moduleName.ToLower());

        if (File.Exists(oldFile))
        {
            File.Delete(oldFile);
        }

        // ���µ������ļ����֮

        string newFile = string.Format("{0}/{1}_temp.json", updatePath, moduleName.ToLower());

        File.Move(newFile, oldFile);

        return bundleList;
    }

    /// <summary>
    /// �ͻ��˸���ģ����ȸ���Դ��ŵ�ַ
    /// </summary>
    private string GetUpdatePath(string moduleName)
    {
        return Application.persistentDataPath + "/Bundles/" + moduleName;
    }

    /// <summary>
    /// ���� ����ģ��ĸ����ļ��ڷ������˵�����URL
    /// </summary>
    /// <param name="moduleConfig">ģ�����ö���</param>
    /// <param name="fileName">�ļ�����</param>
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
