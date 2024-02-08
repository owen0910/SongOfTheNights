using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
/// <summary>
/// 模块管理器
/// </summary>
public class ModuleManager : Singleton<ModuleManager>
{
    /// <summary>
    /// 加载模块
    /// </summary>
    /// <param name="moduleConfig">模块对象</param>
    public async Task<bool> Load(ModuleConfig moduleConfig)
    {
        //不用热更新
        if (GlobalConfig.HotUpdate == false)
        {
            //不热更也不采用AB包加载，直接返回出去
            if (GlobalConfig.BundleMode == false)
            {
                return true;
            }
            //不热更但是要加载AB包
            else
            {
                ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(moduleConfig.moduleName);

                if (moduleABConfig == null)
                {                   
                    return false;
                }

                Debug.Log("模块包含的AB包总数量：" + moduleABConfig.BundleArray.Count);

                //一个哈希表 键是所有资源的路径名，值是AssetRef
                Hashtable Path2AssetRef = AssetLoader.Instance.ConfigAssembly(moduleABConfig);

                //资源加载器中的字典，键是模块名，值是模块下所有资源的哈希表
                AssetLoader.Instance.base2Assets.Add(moduleConfig.moduleName, Path2AssetRef);

                return true;

            }
        }
        //需要热更新
        else
        {
            return await Downloader.Instance.Download(moduleConfig);
        }
    }

    
}
