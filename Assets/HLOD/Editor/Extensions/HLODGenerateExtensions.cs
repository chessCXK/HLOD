using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLOD
{
    public static class HLODGenerateExtensions
    {
        //还原HLOD
        public static void ComeBackHLODAsset(this HLODGenerate hlodGenerate, HLODGenerateEditor editor)
        {
#if UNITY_EDITOR
            if(hlodGenerate.m_RootLODVolume == null)
            {
                return;
            }
#endif
            Queue<LODVolume> queue = new Queue<LODVolume>();
#if UNITY_EDITOR
            queue.Enqueue(hlodGenerate.m_RootLODVolume.GetComponent<LODVolume>());
#endif
            while(queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();
                foreach(var child in lodVolume.childVolumes)
                {
                    queue.Enqueue(child);
                }
                if(lodVolume.combined == null)
                {
                    continue;
                }
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(lodVolume.combined._assetPath);
                if(obj)
                {
#if UNITY_EDITOR
                    obj = PrefabUtility.InstantiatePrefab(obj, hlodGenerate.m_HLODS.transform) as GameObject;
#endif
                    obj.transform.SetPositionAndRotation(lodVolume.combined.Pose.position, lodVolume.combined.Pose.rotation);
                    obj.transform.localScale = lodVolume.combined.Pose.scale;
                    obj.GetComponent<MeshRenderer>().enabled = false;
                    lodVolume.combined._hlodRoot = obj;
                    lodVolume.combined._lodGroup = obj.GetComponent<LODGroup>();

                    if(editor.m_DeleteStreamingAsset)
                    {
                        PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                    }
                }
                
            }
        }

        //还远原始资源
        public static void ComeBackOriginalAsset(this HLODGenerate hlodGenerate)
        {
#if UNITY_EDITOR
            LODVolume root = hlodGenerate.m_RootLODVolume.GetComponent<LODVolume>();
            foreach (var lodGroup in root.lodGroups)
            { 
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(lodGroup._assetPath);
                if (obj)
                {
                    obj = PrefabUtility.InstantiatePrefab(obj, hlodGenerate.m_HLODS.transform) as GameObject;
                    obj.transform.parent = lodGroup._hlodRoot.transform;//生成流式资源后_hlodRoot引用的是父节点
                    obj.transform.SetPositionAndRotation(lodGroup.Pose.position, lodGroup.Pose.rotation);
                    obj.transform.localScale = lodGroup.Pose.scale;
                    lodGroup._hlodRoot = obj;
                    lodGroup._lodGroup = obj.GetComponent<LODGroup>();
                    lodGroup.Enabled = true;
                }
            }
#endif
        }

        //从流式资源中回退
        public static void ComeBackInMemory(this HLODGenerate hlodGenerate, HLODGenerateEditor editor)
        {
            //还原HLOD
            hlodGenerate.ComeBackHLODAsset(editor);

            //还远原始资源
            hlodGenerate.ComeBackOriginalAsset();

            //删除流式资源
            if (editor.m_DeleteStreamingAsset)
            {
                hlodGenerate.DeleteExportStreamingAsset();
            }
        }

        //删除导出的FBX或Mesh资源
        public static void DeleteExportMesh(this HLODGenerate hlodGenerate)
        {
#if UNITY_EDITOR
            if (hlodGenerate.m_IsExportMesh)
            {
                AssetDatabase.DeleteAsset(hlodGenerate.m_MeshAssetPath + "\\" + ExportHLODsByMesh.s_DirectoryName);
            }
#endif
        }

        //删除导出的贴图材质
        public static void DeleteExportMatTex(this HLODGenerate hlodGenerate)
        {
#if UNITY_EDITOR
            if (hlodGenerate.m_IsExportMatTex)
            {
                AssetDatabase.DeleteAsset(hlodGenerate.m_TextureAssetPath + "\\" + TextureAtlasModule.s_TexDirectoryName);
                AssetDatabase.DeleteAsset(hlodGenerate.m_MaterialAssetPath + "\\" + TextureAtlasModule.s_MatDirectoryName);
            }
#endif
        }

        //删除导出的流式资源
        public static void DeleteExportStreamingAsset(this HLODGenerate hlodGenerate)
        {
#if UNITY_EDITOR
            if(hlodGenerate.m_StreamingAssetPath != "")
            {
                AssetDatabase.DeleteAsset(hlodGenerate.m_StreamingAssetPath + "\\" + HLODGenerateEditor.s_StreamingDirectory);
            }
#endif
        }
    }

}