using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ڴ��еĵ�����Դ����
/// </summary>
public class AssetRef 
{
    /// <summary>
    /// �����Դ��������Ϣ
    /// </summary>
    public AssetInfo assetInfo;

    /// <summary>
    /// �����Դ������bundleRef����
    /// </summary>
    public BundleRef bundleRef;

    /// <summary>
    /// �����Դ��������bundleRef����
    /// </summary>
    public BundleRef[] dependencies;

    /// <summary>
    /// ��bundle�ļ�����ȡ��������Դ����
    /// </summary>
    public Object asset;

    /// <summary>
    /// �Ƿ���GameObject
    /// </summary>
    public bool isGameObject;

    /// <summary>
    /// ���Asset������ЩGameObject����
    /// </summary>
    public List<GameObject> children;

    /// <summary>
    /// AssetRef�Ĺ��캯��
    /// </summary>
    /// <param name="assetInfo"></param>
    public AssetRef(AssetInfo assetInfo)
    {
        this.assetInfo = assetInfo;
    }
}
