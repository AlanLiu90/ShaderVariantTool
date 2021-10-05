using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour 
{
    public GameObject[] Prefabs;

    public ShaderVariantConfig VariantConfig;

    public void Create()
    {
        foreach (var prefab in Prefabs)
            Instantiate(prefab);
    }

    public void WarmUp()
    {
        if (VariantConfig != null)
        {
            var manager = new ShaderVariantManager();
            manager.SetConfig(VariantConfig);

            manager.AppendToWarmUp("Cube");
            manager.AppendToWarmUp("Sphere");
            manager.WarmUp();
        }
    }
}
