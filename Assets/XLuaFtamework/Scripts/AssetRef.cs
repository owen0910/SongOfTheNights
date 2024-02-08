using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内存中的单个资源对象
/// </summary>
public class AssetRef 
{
    /// <summary>
    /// 这个资源的配置信息
    /// </summary>
    public AssetInfo assetInfo;

    /// <summary>
    /// 这个资源所属的bundleRef对象
    /// </summary>
    public BundleRef bundleRef;

    /// <summary>
    /// 这个资源所依赖的bundleRef对象
    /// </summary>
    public BundleRef[] dependencies;

    /// <summary>
    /// 从bundle文件中提取出来的资源对象
    /// </summary>
    public Object asset;

    /// <summary>
    /// 是否是GameObject
    /// </summary>
    public bool isGameObject;

    /// <summary>
    /// 这个Asset对象被哪些GameObject依赖
    /// </summary>
    public List<GameObject> children;

    /// <summary>
    /// AssetRef的构造函数
    /// </summary>
    /// <param name="assetInfo"></param>
    public AssetRef(AssetInfo assetInfo)
    {
        this.assetInfo = assetInfo;
    }
}
