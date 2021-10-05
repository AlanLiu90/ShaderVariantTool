#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using ShaderVariant = UnityEngine.ShaderVariantCollection.ShaderVariant;

public class ShaderVariantRecorder
{
    public enum RecordMode
    {
        Add,
        Override,
        Merge
    }

    private class ShaderVariantComparer : IComparer<ShaderVariant>
    {
        public int Compare(ShaderVariant x, ShaderVariant y)
        {
            if (x.shader != y.shader)
                return x.shader.name.CompareTo(y.shader.name);

            if (x.passType != y.passType)
                return x.passType.CompareTo(y.passType);

            int length = Mathf.Min(x.keywords.Length, y.keywords.Length);
            for (int i = 0; i < length; ++i)
            {
                if (x.keywords[i] != y.keywords[i])
                    return x.keywords[i].CompareTo(y.keywords[i]);
            }

            return x.keywords.Length.CompareTo(y.keywords.Length);
        }
    }

    private class ShaderVariantEqualityComparer : IEqualityComparer<ShaderVariant>
    {
        public bool Equals(ShaderVariant x, ShaderVariant y)
        {
            if (x.shader != y.shader)
                return false;

            if (x.passType != y.passType)
                return false;

            if (x.keywords.Length != y.keywords.Length)
                return false;

            for (int i = 0; i < x.keywords.Length; ++i)
            {
                if (x.keywords[i] != y.keywords[i])
                    return false;
            }

            return true;
        }

        public int GetHashCode(ShaderVariant obj)
        {
            int hash = obj.shader != null ? obj.shader.GetHashCode() : 0;
            hash ^= obj.passType.GetHashCode();

            foreach (var k in obj.keywords)
                hash ^= k.GetHashCode();

            return hash;
        }
    }

    private const string TempPath = "Assets/__cache.shaderVariants";

    private static readonly Action<string> mSaveCurrentShaderVariantCollection;
    private static readonly Action mClearCurrentShaderVariantCollection;
    private static readonly ShaderVariantEqualityComparer mEqualityComparer;

    private readonly Dictionary<string, ShaderVariant[]> mRecordedShaderVaraints =
        new Dictionary<string, ShaderVariant[]>();

    private ShaderVariant[] mDefaultShaderVariants;

    static ShaderVariantRecorder()
    {
        mSaveCurrentShaderVariantCollection = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), typeof(ShaderUtil).GetMethod("SaveCurrentShaderVariantCollection", BindingFlags.NonPublic | BindingFlags.Static));
        mClearCurrentShaderVariantCollection = (Action)Delegate.CreateDelegate(typeof(Action), typeof(ShaderUtil).GetMethod("ClearCurrentShaderVariantCollection", BindingFlags.NonPublic | BindingFlags.Static));
        mEqualityComparer = new ShaderVariantEqualityComparer();
    }

    public IEnumerator Initialize(bool ignoreDefaults = true)
    {
        mRecordedShaderVaraints.Clear();
        mDefaultShaderVariants = null;

        if (ignoreDefaults)
        {
            BeginRecord();

            yield return null;
            yield return null;

            EndRecord(string.Empty);

            mDefaultShaderVariants = mRecordedShaderVaraints[string.Empty];
            mRecordedShaderVaraints.Clear();
        }
    }

    public void BeginRecord()
    {
        mClearCurrentShaderVariantCollection();
    }

    public void EndRecord(string key, RecordMode mode = RecordMode.Add)
    {
        try
        {
            mSaveCurrentShaderVariantCollection(TempPath);
            AssetDatabase.Refresh();

            var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(TempPath);
            var obj = new SerializedObject(svc);
            var shaders = obj.FindProperty("m_Shaders");

            var shaderVariants = new List<ShaderVariant>();

            for (int i = 0; i < shaders.arraySize; ++i)
            {
                SerializedProperty arrayElementAtIndex = shaders.GetArrayElementAtIndex(i);
                Shader shader = (Shader)arrayElementAtIndex.FindPropertyRelative("first").objectReferenceValue;
                SerializedProperty serializedProperty = arrayElementAtIndex.FindPropertyRelative("second.variants");

                for (int j = 0; j < serializedProperty.arraySize; j++)
                {
                    SerializedProperty arrayElementAtIndex2 = serializedProperty.GetArrayElementAtIndex(j);
                    PassType passType = (PassType)arrayElementAtIndex2.FindPropertyRelative("passType").intValue;
                    string text = arrayElementAtIndex2.FindPropertyRelative("keywords").stringValue;

                    var sv = new ShaderVariant
                    {
                        shader = shader,
                        passType = passType,
                        keywords = string.IsNullOrEmpty(text) ? new string[0] : text.Split(' ')
                    };

                    if (IsDefaultShaderVariant(sv))
                        continue;

                    shaderVariants.Add(sv);
                }
            }

            switch (mode)
            {
                case RecordMode.Add:
                    mRecordedShaderVaraints.Add(key, shaderVariants.ToArray());
                    break;

                case RecordMode.Override:
                    mRecordedShaderVaraints[key] = shaderVariants.ToArray();
                    break;

                case RecordMode.Merge:
                    {
                        ShaderVariant[] shaderVariantArray;
                        if (mRecordedShaderVaraints.TryGetValue(key, out shaderVariantArray))
                        {
                            var shaderVariantSet = new HashSet<ShaderVariant>(shaderVariantArray, mEqualityComparer);
                            foreach (var variant in shaderVariants)
                                shaderVariantSet.Add(variant);

                            mRecordedShaderVaraints[key] = shaderVariantSet.ToArray();
                        }
                        else
                        {
                            mRecordedShaderVaraints.Add(key, shaderVariants.ToArray());
                        }
                    }

                    break;
            }

            
        }
        finally
        {
            AssetDatabase.DeleteAsset(TempPath);
        }
    }

    public void AddExtra(string key, params ShaderVariant[] shaderVaraints)
    {
        if (shaderVaraints == null || shaderVaraints.Length == 0)
            return;

        for (int i = 0; i < shaderVaraints.Length; ++i)
            Array.Sort(shaderVaraints[i].keywords);

        mRecordedShaderVaraints.Add(key, shaderVaraints);
    }

    public void Export(string path)
    {
        var shaderVariantTable = new Dictionary<ShaderVariant, int>(new ShaderVariantEqualityComparer());

        foreach (var shaderVaraintList in mRecordedShaderVaraints.Values)
        {
            foreach (var sv in shaderVaraintList)
                shaderVariantTable[sv] = 0;
        }

        var keys = shaderVariantTable.Keys.ToArray();
        Array.Sort(keys, new ShaderVariantComparer());

        int index = 0;
        foreach (var key in keys)
            shaderVariantTable[key] = index++;

        var cfg = AssetDatabase.LoadAssetAtPath<ShaderVariantConfig>(path);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<ShaderVariantConfig>();
            AssetDatabase.CreateAsset(cfg, path);
        }

        var items = new List<ShaderVariantConfig.Item>();
        var list = new List<int>();

        foreach (var pair in mRecordedShaderVaraints)
        {
            list.Clear();
            foreach (var shaderVariant in pair.Value)
                list.Add(shaderVariantTable[shaderVariant]);

            list.Sort();
            items.Add(new ShaderVariantConfig.Item { Key = pair.Key, ShaderVariantIds = list.ToArray() });
        }

        items.Sort((x, y) => x.Key.CompareTo(y.Key));

        cfg.Items = items.ToArray();

        var shaderVariantsList = new List<ShaderVariantConfig.ShaderVariants>();
        var shaderVariants = new List<ShaderVariantConfig.ShaderVariant>();
        string shaderName = string.Empty;
        foreach (ShaderVariant key in keys)
        {
            if (shaderName != key.shader.name)
            {
                if (!string.IsNullOrEmpty(shaderName))
                    shaderVariantsList.Add(new ShaderVariantConfig.ShaderVariants(shaderName, shaderVariants.ToArray()));

                shaderName = key.shader.name;
                shaderVariants.Clear();
            }

            shaderVariants.Add(new ShaderVariantConfig.ShaderVariant(key.passType, key.keywords));
        }

        if (!string.IsNullOrEmpty(shaderName))
            shaderVariantsList.Add(new ShaderVariantConfig.ShaderVariants(shaderName, shaderVariants.ToArray()));

        cfg.ShaderVaraints = shaderVariantsList.ToArray();

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();

        Debug.LogFormat("Export ShaderVariantConfig to {0}", path);
    }

    private bool IsDefaultShaderVariant(ShaderVariant shaderVariant)
    {
        if (mDefaultShaderVariants == null)
            return false;

        return Array.Exists(mDefaultShaderVariants, x => mEqualityComparer.Equals(x, shaderVariant));
    }
}

#endif
