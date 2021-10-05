﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class ShaderVariantProfiler : MonoBehaviour 
{
    public Color FontColor = Color.white;
    public int FontSize = 25;
    public float LabelHeight = 40;
    public bool ClearWhenSceneUnloaded;

    private static readonly string[] mNames = new string[]
    {
        "Shader.Parse",
        "CreateGpuProgram"
    };

    private struct RecorderData
    {
        public Recorder Recorder;
        public int Denominator;
        public int Count;
        public long Time;
        public string Text;
    }

    private RecorderData[] mRecorders;
    private GUIStyle labelStyle;

    private void OnEnable()
    {
        mRecorders = new RecorderData[mNames.Length];

        for (int i = 0; i < mRecorders.Length; ++i)
        {
            var recorder = Recorder.Get(mNames[i]);
            recorder.enabled = true;
            mRecorders[i] = new RecorderData { Recorder = recorder };
        }

        mRecorders[0].Denominator = 1;
        mRecorders[1].Denominator = 2;

        for (int i = 0; i < mRecorders.Length; ++i)
            UpdateText(i);

        if (ClearWhenSceneUnloaded)
            SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        foreach (RecorderData data in mRecorders)
        {
            data.Recorder.enabled = false;
        }

        mRecorders = null;
        labelStyle = null;

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Update()
    {
        for (int i = 0; i < mRecorders.Length; ++i)
        {
            int count = mRecorders[i].Recorder.sampleBlockCount;
            if (count <= 0)
                continue;

            mRecorders[i].Count += count;
            mRecorders[i].Time += mRecorders[i].Recorder.elapsedNanoseconds;

            UpdateText(i);
        }
    }

    private void UpdateText(int index)
    {
        int count = mRecorders[index].Count / mRecorders[index].Denominator;
        double milliseconds = mRecorders[index].Time * 0.000001;
        string text = string.Format("{0}: {1}, {2:0.0}ms", mNames[index], count, milliseconds);
        mRecorders[index].Text = text;
    }

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = FontColor;
            labelStyle.fontSize = FontSize;
        }

        var rect = new Rect(0, 0, 500, LabelHeight);
        foreach (RecorderData data in mRecorders)
        {
            GUI.Label(rect, data.Text, labelStyle);
            rect.y += LabelHeight;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        for (int i = 0; i < mRecorders.Length; ++i)
        {
            mRecorders[i].Count = 0;
            mRecorders[i].Time = 0;

            UpdateText(i);
        }
    }
}
