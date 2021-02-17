using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLOD
{
    public class HLODEditor : EditorWindow
    {
        const string k_GI_CacheDistance = "GI_CacheDistance";
        const int k_DefaultCacheDistance = 0;
        public static float GI_CacheDistance
        {
            set
            {
                EditorPrefs.SetFloat(k_GI_CacheDistance, value);
            }
            get
            {
                return EditorPrefs.GetFloat(k_GI_CacheDistance, k_DefaultCacheDistance);
            }
        }
        Vector2 scrollPos;

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GUILayout.Space(6);
            var label = new GUIContent("GI CacheDistance", "全局HLOD切换缓冲距离");
            float value = EditorGUILayout.FloatField(label, GI_CacheDistance);
            if(value < 0)
            {
                GI_CacheDistance = 0;
            }
            else
            {
                GI_CacheDistance = value;
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
