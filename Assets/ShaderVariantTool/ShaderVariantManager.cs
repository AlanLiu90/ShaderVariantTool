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

    public void ClearKeys()
    {
        mKeys.Clear();
    }

    public void WarmUp()
    {
        float time = Time.realtimeSinceStartup;

        var shaderVariantIdSet = new HashSet<int>();

        foreach (string key in mKeys)
        {
            ShaderVariantConfig.Item item;
            if (!mItemTable.TryGetValue(key, out item))
                continue;

            foreach (int id in item.ShaderVariantIds)
                shaderVariantIdSet.Add(id);
        }

        var shaderVariantIds = shaderVariantIdSet.ToArray();
        Array.Sort(shaderVariantIds);

        var svc = CreateShaderVariantCollection(mConfig, shaderVariantIds);
        svc.WarmUp();

        mKeys.Clear();

        Debug.LogFormat("ShaderVariantCollection.WarmUp cost {0:0.0} ms", (Time.realtimeSinceStartup - time) * 1000);
    }

    public void WarmUp(ShaderVariantConfig cfg)
    {
        float time = Time.realtimeSinceStartup;

        var ids = new int[cfg.ShaderVariants.Select(x => x.Variants.Length).Sum()];
        for (int i = 0; i < ids.Length; ++i)
            ids[i] = i;

        var svc = CreateShaderVariantCollection(cfg, ids);
        svc.WarmUp();

        Debug.LogFormat("ShaderVariantCollection.WarmUp({0}) cost {1:0.0} ms", cfg.name, (Time.realtimeSinceStartup - time) * 1000);
    }

    protected virtual ShaderVariantCollection CreateShaderVariantCollection(ShaderVariantConfig cfg, int[] shaderVariantIds)
    {
        var svc = new ShaderVariantCollection();

        int index = 0;
        int prevCount = 0;
        Shader shader = null;
        foreach (int id in shaderVariantIds)
        {
            int count = id - prevCount;
            while (count >= cfg.ShaderVariants[index].Variants.Length)
            {
                int variantCount = cfg.ShaderVariants[index].Variants.Length;
                count -= variantCount;
                prevCount += variantCount;
                ++index;
                shader = null;
            }

            if (shader == null)
                shader = Shader.Find(cfg.ShaderVariants[index].ShaderName);

            var variant = cfg.ShaderVariants[index].Variants[count];
            var unityVariant = new ShaderVariantCollection.ShaderVariant(shader, variant.PassType, variant.Keywords);

            ProcessVariant(svc, unityVariant);
        }

        return svc;
    }

    protected virtual void ProcessVariant(ShaderVariantCollection svc, ShaderVariantCollection.ShaderVariant variant)
    {
        svc.Add(variant);
    }
}
