using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    /// <summary>
    /// ������
    /// </summary>
    public static Main Instance;

    private async void Awake()
    {
        InitGlobal();

        //����ģ��
        ModuleConfig launchModule = new ModuleConfig()
        {
            moduleName = "Launch",
            moduleVersion = "1.0.0",
            moduleUrl = "http://172.0.0.1"

        };

        bool result = await ModuleManager.Instance.Load(launchModule);
        if (result==true)
        {
            //������Ѵ������Ȩ����Lua

            AssetLoader.Instance.Clone("Launch", "Assets/GAssets/Launch/Sphere.prefab");
            GameObject pizzaCat = AssetLoader.Instance.Clone("Launch", "Assets/GAssets/Launch/PizzaCat.prefab");

            pizzaCat.GetComponent<SpriteRenderer>().sprite =
                AssetLoader.Instance.CreateAsset<Sprite>("Launch", "Assets/GAssets/Launch/123.jpg", pizzaCat);

        }
    }

    /// <summary>
    /// ��ʼ��ȫ�ֱ���
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
        // ִ��ж�ز���
        AssetLoader.Instance.Unload(AssetLoader.Instance.base2Assets);
    }
}
