
using UnityEditor;
using UnityEngine;

namespace Unity.HLOD
{
    
    public class HLODHelper
    {
        [MenuItem("HLOD/AddHLODGenerate", priority = 1)]
        static void AddHLODGenerate()
        {
            GameObject obj = Selection.activeGameObject;
            if(obj != null)
            {
                if(obj.GetComponent<HLODGenerate>())
                {
                    return;
                }
                obj.AddComponent<HLODGenerate>();
                return;
            }

            Selection.activeGameObject = new GameObject("HLODManager", typeof(HLODGenerate));
        }
        /*
        [MenuItem(LOD_PREFIX + HLOD + "HLODSetting")]
        [MenuItem(HLOD + "HLODSetting", priority = 10)]
        static void HLODSetting(MenuCommand menuCommand)
        {
            HLODEditor window = EditorWindow.GetWindow<HLODEditor>("HLOD Setting");
            window.Show();
        }
        */
    }

}
