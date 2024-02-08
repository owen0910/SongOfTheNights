using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个AB包数据 用于序列化为json文件
/// </summary>
public class BundleInfo 
{
    //AB包名字
    public string bundle_name;

    //AB包的crc散码
    public string crc;

    //这个AB包所包含的资源文件的路径列表
    public List<string> assets;

}

/// <summary>
/// 资源数据类
/// </summary>
public class AssetInfo
{
    //资源文件的相对路径
    public string asset_path;

    //这个资源所属的AB包的名字
    public string bundle_name;

    //这个资源所依赖的AB包名列表
    public List<string> dependencies;
}

/// <summary>
/// 模块对象类
/// </summary>
public class ModuleABConfig
{
    /// <summary>
    /// 资源类数组
    /// </summary>
    public AssetInfo[] AssetArray;

    /// <summary>
    /// AB包信息字典,键是AB包名字 值为AB包信息
    /// </summary>
    public Dictionary<string, BundleInfo> BundleArray;


    public ModuleABConfig()
    {

    }
    /// <summary>
    /// 创建模块类
    /// </summary>
    /// <param name="assetCount">资源数量</param>
    public ModuleABConfig(int assetCount)
    {
        BundleArray = new Dictionary<string, BundleInfo>();
        AssetArray = new AssetInfo[assetCount];
    }

    /// <summary>
    /// 新增一个AB包记录
    /// </summary>
    /// <param name="bundleName"></param>
    /// <param name="bundleInfo"></param>
    public void AddBundle(string bundleName,BundleInfo bundleInfo)
    {
        BundleArray[bundleName] = bundleInfo;
    }

    /// <summary>
    /// 新增一个资源文件
    /// </summary>
    /// <param name="index">资源编号</param>
    /// <param name="assetInfo">资源信息</param>
    public void AddAsset(int index,AssetInfo assetInfo)
    {
        AssetArray[index] = assetInfo;
    }
}
