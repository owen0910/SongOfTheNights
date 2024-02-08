using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
/// <summary>
/// ģ�������
/// </summary>
public class ModuleManager : Singleton<ModuleManager>
{
    /// <summary>
    /// ����ģ��
    /// </summary>
    /// <param name="moduleConfig">ģ�����</param>
    public async Task<bool> Load(ModuleConfig moduleConfig)
    {
        //�����ȸ���
        if (GlobalConfig.HotUpdate == false)
        {
            //���ȸ�Ҳ������AB�����أ�ֱ�ӷ��س�ȥ
            if (GlobalConfig.BundleMode == false)
            {
                return true;
            }
            //���ȸ�����Ҫ����AB��
            else
            {
                ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(moduleConfig.moduleName);

                if (moduleABConfig == null)
                {                   
                    return false;
                }

                Debug.Log("ģ�������AB����������" + moduleABConfig.BundleArray.Count);

                //һ����ϣ�� ����������Դ��·������ֵ��AssetRef
                Hashtable Path2AssetRef = AssetLoader.Instance.ConfigAssembly(moduleABConfig);

                //��Դ�������е��ֵ䣬����ģ������ֵ��ģ����������Դ�Ĺ�ϣ��
                AssetLoader.Instance.base2Assets.Add(moduleConfig.moduleName, Path2AssetRef);

                return true;

            }
        }
        //��Ҫ�ȸ���
        else
        {
            return await Downloader.Instance.Download(moduleConfig);
        }
    }

    
}
