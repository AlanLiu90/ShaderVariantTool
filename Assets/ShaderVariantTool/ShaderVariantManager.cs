using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShaderVariantManager 
{
    private ShaderVariantConfig mConfig;
    private readonly Dictionary<string, ShaderVariantConfig.Item> mItemTable = new Dictionary<string, ShaderVariantConfig.Item>();

    private readonly HashSet<string> mKeys = new HashSet<string>();

	public void SetConfig(ShaderVariantConfig cfg)
    {
        mConfig = cfg;

        mItemTable.Clear();
        foreach (ShaderVariantConfig.Item item in cfg.Items)
        {
            mItemTable.Add(item.Key, item);
        }
    }

    public void AppendToWarmUp(string key)
    {
        mKeys.Add(key);
    }

    public void WarmUp()
    {
        float time = Time.realtimeSinceStartup;

        var svc = CreateShaderVariantCollection(mKeys);
        svc.WarmUp();

        mKeys.Clear();

        Debug.LogFormat("WarmUp cost {0:0.0} ms", (Time.realtimeSinceStartup - time) * 1000);
    }

    protected virtual ShaderVariantCollection CreateShaderVariantCollection(HashSet<string> keySet)
    {
        var shaderVariantIdSet = new HashSet<int>();

        foreach (string key in keySet)
        {
            ShaderVariantConfig.Item item;
            if (!mItemTable.TryGetValue(key, out item))
                continue;

            foreach (int id in item.ShaderVariantIds)
                shaderVariantIdSet.Add(id);
        }

        var shaderVariantIds = shaderVariantIdSet.ToArray();
        Array.Sort(shaderVariantIds);

        var svc = new ShaderVariantCollection();

        int index = 0;
        int prevCount = 0;
        Shader shader = null;
        foreach (int id in shaderVariantIds)
        {
            int count = id - prevCount;
            while (count >= mConfig.ShaderVaraints[index].Variants.Length)
            {
                int variantCount = mConfig.ShaderVaraints[index].Variants.Length;
                count -= variantCount;
                prevCount += variantCount;
                ++index;
                shader = null;
            }

            if (shader == null)
                shader = Shader.Find(mConfig.ShaderVaraints[index].ShaderName);

            var variant = mConfig.ShaderVaraints[index].Variants[count];
            var unityVariant = new ShaderVariantCollection.ShaderVariant(shader, variant.PassType, variant.Keywords);
            if (!Process(ref unityVariant))
                continue;
            
            svc.Add(unityVariant);
        }

        return svc;
    }

    protected virtual bool Process(ref ShaderVariantCollection.ShaderVariant variant)
    {
        return true;
    }
}
