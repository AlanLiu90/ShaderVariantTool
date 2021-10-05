using System;
using UnityEngine;
using UnityEngine.Rendering;

public class ShaderVariantConfig : ScriptableObject
{
    [Serializable]
    public struct Item
    {
        public string Key;
        public int[] ShaderVariantIds;
    }

    [Serializable]
    public struct ShaderVariants
    {
        public string ShaderName;
        public ShaderVariant[] Variants;

        public ShaderVariants(string shaderName, ShaderVariant[] variants)
        {
            ShaderName = shaderName;
            Variants = variants;
        }
    }

    [Serializable]
    public struct ShaderVariant
    {
        public PassType PassType;
        public string[] Keywords;

        public ShaderVariant(PassType passType, params string[] keywords)
        {
            PassType = passType;
            Keywords = keywords;
        }
    }

    public Item[] Items;
    public ShaderVariants[] ShaderVaraints;
}
