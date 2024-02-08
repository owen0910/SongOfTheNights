using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ȫ������
/// </summary>
public static class GlobalConfig
{
    /// <summary>
    /// �Ƿ����ȸ�
    /// </summary>
    public static bool HotUpdate;

    /// <summary>
    /// �Ƿ����bundle�ķ�ʽ����
    /// </summary>
    public static bool BundleMode;


    // ���캯�� Ĭ�϶��ر�
    static GlobalConfig()
    {
        HotUpdate = false;

        BundleMode = false;
    }
}

/// <summary>
/// ����ģ���������
/// </summary>
public class ModuleConfig
{
    /// <summary>
    /// ģ����Դ��Զ�̷������ϵĻ�����ַ
    /// </summary>
    public string DownloadURL
    {
        get
        {
            return moduleUrl + "/" + moduleName + "/" + moduleVersion;
        }
    }

    /// <summary>
    /// ģ�������
    /// </summary>
    public string moduleName;

    /// <summary>
    /// ģ��汾��
    /// </summary>
    public string moduleVersion;

    /// <summary>
    /// ģ���ȸ��µķ�������ַ
    /// </summary>
    public string moduleUrl;
}

/// <summary>
/// ѡ�� ԭʼֻ��·�� ���� �ɶ���д·��
/// </summary>
public enum BaseOrUpdate
{
    /// <summary>
    /// APP��װʱ��������ԭʼ��ֻ��·��
    /// </summary>
    Base,

    /// <summary>
    /// APP�ṩ�� �ɶ���д��·��
    /// </summary>
    Update
}