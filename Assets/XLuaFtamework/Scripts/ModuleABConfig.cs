using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// һ��AB������ �������л�Ϊjson�ļ�
/// </summary>
public class BundleInfo 
{
    //AB������
    public string bundle_name;

    //AB����crcɢ��
    public string crc;

    //���AB������������Դ�ļ���·���б�
    public List<string> assets;

}

/// <summary>
/// ��Դ������
/// </summary>
public class AssetInfo
{
    //��Դ�ļ������·��
    public string asset_path;

    //�����Դ������AB��������
    public string bundle_name;

    //�����Դ��������AB�����б�
    public List<string> dependencies;
}

/// <summary>
/// ģ�������
/// </summary>
public class ModuleABConfig
{
    /// <summary>
    /// ��Դ������
    /// </summary>
    public AssetInfo[] AssetArray;

    /// <summary>
    /// AB����Ϣ�ֵ�,����AB������ ֵΪAB����Ϣ
    /// </summary>
    public Dictionary<string, BundleInfo> BundleArray;


    public ModuleABConfig()
    {

    }
    /// <summary>
    /// ����ģ����
    /// </summary>
    /// <param name="assetCount">��Դ����</param>
    public ModuleABConfig(int assetCount)
    {
        BundleArray = new Dictionary<string, BundleInfo>();
        AssetArray = new AssetInfo[assetCount];
    }

    /// <summary>
    /// ����һ��AB����¼
    /// </summary>
    /// <param name="bundleName"></param>
    /// <param name="bundleInfo"></param>
    public void AddBundle(string bundleName,BundleInfo bundleInfo)
    {
        BundleArray[bundleName] = bundleInfo;
    }

    /// <summary>
    /// ����һ����Դ�ļ�
    /// </summary>
    /// <param name="index">��Դ���</param>
    /// <param name="assetInfo">��Դ��Ϣ</param>
    public void AddAsset(int index,AssetInfo assetInfo)
    {
        AssetArray[index] = assetInfo;
    }
}
