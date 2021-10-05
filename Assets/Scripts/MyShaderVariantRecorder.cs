#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class MyShaderVariantRecorder : MonoBehaviour 
{
	public Camera MainCamera;
	public string[] Prefabs;

	ShaderVariantRecorder mRecorder;

	private IEnumerator Start()
    {
	 	Vector3 pos = MainCamera.transform.position + MainCamera.transform.forward * 10;

		mRecorder = new ShaderVariantRecorder();
		yield return StartCoroutine(mRecorder.Initialize());

		foreach (string prefab in Prefabs)
        {
			yield return StartCoroutine(RecordOne(pos, prefab));

			yield return Resources.UnloadUnusedAssets();
        }

		mRecorder.Export("Assets/ShaderVariants.asset");
	}

	private IEnumerator RecordOne(Vector3 pos, string path)
    {
		mRecorder.BeginRecord();

		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
	    var obj = Instantiate(prefab, pos, Quaternion.identity);

		yield return null;

		mRecorder.EndRecord(Path.GetFileNameWithoutExtension(path));

		Destroy(obj);
    }
}

#endif
