using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeObjectDontDestroy : MonoBehaviour 
{
	public GameObject Object;
	
	private void Awake()
    {
		DontDestroyOnLoad(Object);
    }
}
