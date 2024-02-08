using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    /// <summary>
    /// 主对象
    /// </summary>
    public static Main Instance;

    private async void Awake()
    {
        InitGlobal();

        //启动模块
        ModuleConfig launchModule = new ModuleConfig()
        {
            moduleName = "Launch",
            moduleVersion = "1.0.0",
            moduleUrl = "http://172.0.0.1"

        };

        bool result = await ModuleManager.Instance.Load(launchModule);
        if (result==true)
        {
            //在这里把代码控制权交给Lua

            AssetLoader.Instance.Clone("Launch", "Assets/GAssets/Launch/Sphere.prefab");
            GameObject pizzaCat = AssetLoader.Instance.Clone("Launch", "Assets/GAssets/Launch/PizzaCat.prefab");

            pizzaCat.GetComponent<SpriteRenderer>().sprite =
                AssetLoader.Instance.CreateAsset<Sprite>("Launch", "Assets/GAssets/Launch/123.jpg", pizzaCat);

        }
    }

    /// <summary>
    /// 初始化全局变量
    /// </summary>
    private void InitGlobal()
    {
        Instance = this;

        GlobalConfig.HotUpdate = false;

        GlobalConfig.BundleMode = true;

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 执行卸载策略
        AssetLoader.Instance.Unload(AssetLoader.Instance.base2Assets);
    }
}
