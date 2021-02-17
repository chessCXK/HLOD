
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.HLOD
{
    [CustomEditor(typeof(HLODGenerate))]
    [CanEditMultipleObjects]
    public class HLODGenerateEditor : HLODEditorWindow
    {
        public static string s_StreamingDirectory = "Streaming";//流式资源文件夹

        List<TextureAtlasData> m_AddedTextureAtlasData = new List<TextureAtlasData>();
        List<TextureAtlasData> m_RemovedTextureAtlasData = new List<TextureAtlasData>();

        public bool m_DeleteStreamingAsset = false;
#if UNITY_EDITOR
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            HLODGenerate hlodGenerate = (HLODGenerate)target;

            OtherOperator(hlodGenerate);
            string nullSearch = null;
            GUILayout.BeginVertical();

            DrawHeader("Mesh", ref nullSearch, 0, true);
            MeshOperator(hlodGenerate);
            GUILayout.Space(10);
            DrawHeader("Texture", ref nullSearch, 0, true);
            MatsAndTexsOperator(hlodGenerate);

            GUILayout.Space(10);
            DrawHeader("Generate", ref nullSearch, 0, true);
            GUILayout.BeginHorizontal();
            GenerateHLODS(hlodGenerate);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            DrawHeader("Streaming", ref nullSearch, 0, true);
            DrawHeader("Streaming导出的预制体以及合并前的预制体需要添加到AddressablesAssets里才能正常使用", ref nullSearch, 0, true);
            GUILayout.BeginHorizontal();
            GenerateStreamingLoad(hlodGenerate);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        /*一些其他操作*/
        void OtherOperator(HLODGenerate hlodGenerate)
        {
            //设置延时卸载时间
            /*
            float value = EditorGUILayout.FloatField("DelayUnLoadTime", hlodGenerate.m_DelayUnLoadTime);
            if(value != hlodGenerate.m_DelayUnLoadTime)
            {
                hlodGenerate.m_DelayUnLoadTime = value;
                ChildTreeRoot [] roots = hlodGenerate.GetComponentsInChildren<ChildTreeRoot>();
                foreach(var root in roots)
                {
                    root.m_DelayUnLoadTime = value;
                }
            }*/

            //距离缓冲设置，全局
            /*
            if (hlodGenerate.m_CacheDistance != HLODEditor.GI_CacheDistance)
            {
                hlodGenerate.m_CacheDistance = HLODEditor.GI_CacheDistance;
                EditorUtility.SetDirty(hlodGenerate);
            }*/
        }
        /*流式操作*/
        void GenerateStreamingLoad(HLODGenerate hlodGenerate)
        {

            if(hlodGenerate.m_RootLODVolume)
            {
                HLODCull hlodCull = hlodGenerate.m_RootLODVolume.GetComponent<HLODCull>();
                if (hlodCull)
                {
                    hlodCull.m_Stream = hlodGenerate.m_IsStreaming;
                }
            }
            

            if (!hlodGenerate.m_IsExportMatTex || !hlodGenerate.m_IsExportMesh)
            {
                return;
            }
            
            GUILayout.BeginVertical();
            do
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Streming路径:");
                GUILayout.Label(hlodGenerate.m_StreamingAssetPath + "\\" + s_StreamingDirectory);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (hlodGenerate.m_IsStreaming && hlodGenerate.StreamUpdate.Count > 0 && GUILayout.Button("更新"))
                {
                    string savePath = Path.Combine(hlodGenerate.m_StreamingAssetPath, s_StreamingDirectory);
                    foreach (var lv in hlodGenerate.StreamUpdate)
                    {
                        GeneratePrefab(lv, savePath);
                    }
                    hlodGenerate.StreamUpdate.Clear();
                    GenrateFineModelRef(hlodGenerate.m_RootLODVolume.GetComponent<LODVolume>());
                }
                if (hlodGenerate.m_IsStreaming)
                {
                    GUILayout.BeginHorizontal();
                    
                    m_DeleteStreamingAsset = GUILayout.Toggle(m_DeleteStreamingAsset, "同时删除流式资源");
                    
                    if (GUILayout.Button("退回"))
                    {
                        hlodGenerate.ComeBackInMemory(this);
                        hlodGenerate.m_IsStreaming = false;
                        EditorUtility.SetDirty(hlodGenerate.gameObject);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    GUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("Streaming已生成", MessageType.Info);
                    break;
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("路径选择"))
                {
                    string path = EditorUtility.OpenFolderPanel("Save HLODsStreaming to....", Application.dataPath, "");
                    path = path.Replace(Application.dataPath, "Assets");
                    path = path.Replace("/", "\\");
                    hlodGenerate.m_StreamingAssetPath = path;
                }
                if (hlodGenerate.m_StreamingAssetPath != "" && GUILayout.Button("导出"))
                {
                    hlodGenerate.m_IsStreaming = true;
                    UpdataStreamingAsset(hlodGenerate);
                    hlodGenerate.StreamUpdate.Clear();
                    EditorUtility.SetDirty(hlodGenerate.gameObject);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                GUILayout.EndHorizontal();
            } while (false);


            GUILayout.EndVertical();
        }

        /*生成、更新操作*/
        void GenerateHLODS(HLODGenerate hlodGenerate)
        {
            if (hlodGenerate == null || hlodGenerate.m_Targets == null || hlodGenerate.m_Targets.Length < 1)
            {
                return;
            }
            if (hlodGenerate.m_IsGenerating)
            {
                EditorGUILayout.HelpBox("正在工作中。。。", MessageType.Warning);
                return;
            }
            if(hlodGenerate.m_IsStreaming)
            {
                EditorGUILayout.HelpBox("请先回退后再操作", MessageType.Warning);
                return;
            }
            if (!hlodGenerate.m_IsGenerate)
            {
                
                if (GUILayout.Button("生成HLODS"))
                {
                    if(!hlodGenerate.m_GenerateAsset)
                    {
                        if (!EditorUtility.DisplayDialog("提示", "建议生成后就导出贴图，贴图的索引缓存在内存中，只要代码重新编译缓存就会被清除！！", "好了，我知道了"))
                            return;
                    }
                    MonoBehaviourHelper.instance.DestroySurrogate();
                    hlodGenerate.m_IsGenerating = true;
                    SceneLOD.instance.GenerateSceneLODByHLODGenerate(hlodGenerate, (GameObject bvhRoot, GameObject hlodsRoot) =>
                    {
                        if (bvhRoot != null && hlodsRoot != null)
                        {
                            //将生成的hlods与bvh管理起来
                            bvhRoot.transform.parent = hlodGenerate.transform;
                            hlodsRoot.transform.parent = hlodGenerate.transform;
                            hlodGenerate.m_RootLODVolume = bvhRoot;
                            hlodGenerate.m_HLODS = hlodsRoot;
                            hlodGenerate.m_IsGenerate = true;
                            if (hlodGenerate.m_GenerateAsset)
                            {
                                hlodGenerate.m_IsExportMesh = true;
                                hlodGenerate.m_IsExportMatTex = true;
                                UpdataCombineMesh(hlodGenerate);
                                UpdataCombineMatsAndTexs(hlodGenerate);
                            }
                            EditorUtility.SetDirty(hlodGenerate);
                            EditorUtility.SetDirty(hlodGenerate.gameObject);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                        hlodGenerate.m_IsGenerating = false;
                    });
                    
                }
                hlodGenerate.m_GenerateAsset = GUILayout.Toggle(hlodGenerate.m_GenerateAsset, "同时导出资源");
                /*查看是否已经选择了mesh和texture导出路径*/
                if(hlodGenerate.m_GenerateAsset)
                {
                    if (hlodGenerate.m_MeshAssetPath == "")
                    {
                        EditorUtility.DisplayDialog("警告", "没有选择mesh路径，请先选择路径", "是");
                        hlodGenerate.m_GenerateAsset = false;
                        return;
                    }
                    if (hlodGenerate.m_TextureAssetPath == "")
                    {
                        EditorUtility.DisplayDialog("警告", "没有选择textures路径，请先选择路径", "是");
                        hlodGenerate.m_GenerateAsset = false;
                        return;
                    }
                }

            }
            else
            {
                if (GUILayout.Button("更新"))
                {
                    MonoBehaviourHelper.instance.DestroySurrogate();
                    hlodGenerate.m_IsGenerating = true;
                    SceneLOD.instance.OnUpdataBVHByHLODGenerate(hlodGenerate, (GameObject bvhRoot, GameObject hlodsRoot) =>
                    {
                        if(bvhRoot != null && hlodsRoot != null)
                        {
                            UpdataCombineMesh(hlodGenerate);
                            UpdataCombineMatsAndTexs(hlodGenerate);
                        }
                        hlodGenerate.m_IsGenerating = false;
                    });
                }
                if (GUILayout.Button("删除HLOD（同时删除资源）"))
                {
                    if(EditorUtility.DisplayDialog("警告", "本次操作会伴随删除对应生成的贴图和mesh目录", "删除", "我再想想"))
                    {
                        DeleteHLODs(hlodGenerate);
                    }                
                }
            }
            
        }

        /*网格操作*/
        void MeshOperator(HLODGenerate hlodGenerate)
        {
            GUILayout.BeginVertical();

            do
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Mesh路径:");
                GUILayout.Label(hlodGenerate.m_MeshAssetPath + "\\" +ExportHLODsByMesh.s_DirectoryName);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (hlodGenerate.m_IsExportMesh)
                {
                    EditorGUILayout.HelpBox("网格已导出", MessageType.Info);
                    break;
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("路径选择"))
                {
                    string path = EditorUtility.OpenFolderPanel("Save HLODsMesh to....", Application.dataPath, "");
                    path = path.Replace(Application.dataPath, "Assets");
                    path = path.Replace("/", "\\");
                    hlodGenerate.m_MeshAssetPath = path;
                }
                if (hlodGenerate.m_HLODS != null && GUILayout.Button("导出"))
                {
                    hlodGenerate.m_IsExportMesh = true;
                    UpdataCombineMesh(hlodGenerate);
                    //ExportMesh(hlodGenerate);
                }
                GUILayout.EndHorizontal();
            } while (false);
            

            GUILayout.EndVertical();
        }

        /*贴图、材质操作*/
        void MatsAndTexsOperator(HLODGenerate hlodGenerate)
        {
            GUILayout.BeginVertical();
            do
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Textures路径:");
                GUILayout.Label(hlodGenerate.m_TextureAssetPath + "\\" + TextureAtlasModule.s_TexDirectoryName);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Materials路径:");
                GUILayout.Label(hlodGenerate.m_MaterialAssetPath + "\\" + TextureAtlasModule.s_MatDirectoryName);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (hlodGenerate.m_IsExportMatTex)
                {
                    EditorGUILayout.HelpBox("贴图、材质已导出", MessageType.Info);
                    break;
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("贴图路径选择"))
                {
                    string path = EditorUtility.OpenFolderPanel("Save HLODsTextures to....", Application.dataPath, "");
                    path = path.Replace(Application.dataPath, "Assets");
                    path = path.Replace("/", "\\");
                    hlodGenerate.m_TextureAssetPath = path;
                }
                if (GUILayout.Button("材质球路径选择"))
                {
                    string path = EditorUtility.OpenFolderPanel("Save HLODsMaterial to....", Application.dataPath, "");
                    path = path.Replace(Application.dataPath, "Assets");
                    path = path.Replace("/", "\\");
                    hlodGenerate.m_MaterialAssetPath = path;
                }
                if (hlodGenerate.m_HLODS != null && GUILayout.Button("导出"))
                {
                    hlodGenerate.m_IsExportMatTex = true;
                    UpdataCombineMatsAndTexs(hlodGenerate);
                    //ExportTextures(hlodGenerate);
                }
                GUILayout.EndHorizontal();
            } while (false);
            
           
            GUILayout.EndVertical();
        }

        /*更新合批网格*/
        void UpdataCombineMesh(HLODGenerate hlodGenerate)
        {
            if (!hlodGenerate.m_IsExportMesh)
            {
                return;
            }

            if (hlodGenerate.m_MeshAssetPath == "")
            {
                EditorUtility.DisplayDialog("警告", "没有选择mesh路径，请先选择路径", "是");
                hlodGenerate.m_IsExportMesh = false;
                return;
            }
            Queue<LODVolume> queue = new Queue<LODVolume>();

            queue.Enqueue(hlodGenerate.m_RootLODVolume.GetComponent<LODVolume>());
            //AssetDatabase.StartAssetEditing();
            while (queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();

                if (lodVolume.combined != null && lodVolume.combined._hlodRoot != null)
                {
                    string path = AssetDatabase.GetAssetPath(lodVolume.combined._hlodRoot.GetComponent<MeshFilter>().sharedMesh);
                    
                    if (lodVolume.lodGroups.Count < 1)//删除多出来的资源
                    {
                        if (path == "")
                        {
                            path = Path.Combine(hlodGenerate.m_MeshAssetPath, ExportHLODsByMesh.s_DirectoryName, lodVolume.combined._hlodRoot.name);
                            path = Path.ChangeExtension(path, "asset");
                        }
                        DestroyImmediate(lodVolume.GetComponent<LODGroup>());
                        DestroyImmediate(lodVolume.combined._hlodRoot);
                        lodVolume.combined._hlodRoot = null;

                        if (path != "")
                        {
                            AssetDatabase.DeleteAsset(path);
                        }
                    }
                    else if (path == "")//更新资源
                    {
                        //ExportHLODsByMesh.PersistHLODs(lodVolume.hlodRoot.transform, hlodGenerate.m_MeshAssetPath);
                        ExportHLODsByMesh.PersistHLODOfFbx(lodVolume.combined._hlodRoot.transform, hlodGenerate.m_MeshAssetPath);
                        if(!hlodGenerate.StreamUpdate.Contains(lodVolume))
                        {
                            hlodGenerate.StreamUpdate.Add(lodVolume);
                        }

                    }
                    EditorUtility.SetDirty(lodVolume);
                }

                foreach (LODVolume lv in lodVolume.childVolumes)
                {
                    queue.Enqueue(lv);
                }
            }
            //AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /*更新合批贴图、材质球*/
        void UpdataCombineMatsAndTexs(HLODGenerate hlodGenerate)
        {
            Dictionary<HLODGenerate, List<TextureAtlasData>> m_AllAtlases = TextureAtlasModule.instance.AllAtlases;
            List<TextureAtlasData> data = null;
            if (m_AllAtlases.TryGetValue(hlodGenerate, out data))
            {
                List<TextureAtlasData> remove = m_RemovedTextureAtlasData;
                List<TextureAtlasData> add = m_AddedTextureAtlasData;
                remove.Clear();
                add.Clear();

                //标记delete的就删除并删除资源，标记add就添加或更新
                for (int i = 0; i < data.Count; i++)
                {
                    TextureAtlasData taData = data[i];
                    switch (taData.m_State)
                    {
                        case TextureAtlasData.State.Delete:
                            remove.Add(taData);
                            break;
                        case TextureAtlasData.State.Add:
                            taData.m_MeshRenderers.RemoveAll(r => r == null);
                            if(taData.m_MeshRenderers.Count > 0)
                            {
                                add.Add(taData);
                            }
                            else
                            {
                                taData.m_State = TextureAtlasData.State.Delete;
                                remove.Add(taData);
                            }
                            //删除空renderer
                            
                            break;
                    }
                }
                data.RemoveAll(r => r.m_State == TextureAtlasData.State.Delete);

                if (hlodGenerate.m_IsExportMatTex)
                {
                    if (hlodGenerate.m_TextureAssetPath == "")
                    {
                        EditorUtility.DisplayDialog("警告", "没有选择textures路径，请先选择路径", "是");
                        hlodGenerate.m_IsExportMatTex = false;
                        return;
                    }
                    if (hlodGenerate.m_MaterialAssetPath == "")
                    {
                        EditorUtility.DisplayDialog("警告", "没有选择materials路径，请先选择路径", "是");
                        hlodGenerate.m_IsExportMatTex = false;
                        return;
                    }
                    int progressCount = 1;
                    foreach (TextureAtlasData r in remove)
                    {
                        EditorUtility.DisplayProgressBar("删除图集", r.m_Root.name, (float)progressCount++ / remove.Count);
                        TextureAtlasModule.DeleteOnlyOneTextureAtlasData(r);
                    }
                    progressCount = 1;
                    foreach (TextureAtlasData a in add)
                    {
                        EditorUtility.DisplayProgressBar("保存图集", a.m_Root.name, (float)progressCount++ / add.Count);
                        TextureAtlasModule.ExportOnlyOneTextureAtlasData(a, hlodGenerate.m_TextureAssetPath, hlodGenerate.m_MaterialAssetPath, a.m_MeshRenderers[0].name);
                        foreach (MeshRenderer mr in a.m_MeshRenderers)
                        {
                            mr.sharedMaterial = a.m_Material;
                        }
                        a.m_State = TextureAtlasData.State.None;
                    }
                }
                remove.Clear();
                add.Clear();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /*更新流式资源*/
        void UpdataStreamingAsset(HLODGenerate hlodGenerate)
        {
            string savePath = Path.Combine(hlodGenerate.m_StreamingAssetPath, s_StreamingDirectory);
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            LODVolume rootLODVolume = hlodGenerate.m_RootLODVolume.GetComponent<LODVolume>();
            Queue<LODVolume> queue = new Queue<LODVolume>();
            queue.Enqueue(rootLODVolume);

            AssetDatabase.StartAssetEditing();
            while (queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();
                foreach (var child in lodVolume.childVolumes)
                {
                    queue.Enqueue(child);
                }
                if (lodVolume.combined == null)
                {
                    continue;
                }

                //导出资源
                GeneratePrefab(lodVolume, savePath);
            }
            AssetDatabase.StopAssetEditing();
            GenrateFineModelRef(hlodGenerate.m_RootLODVolume.GetComponent<LODVolume>());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /*生成prefab*/
        void GeneratePrefab(LODVolume lodVolume, string savePath)
        {
            if (lodVolume.combined._hlodRoot == null)
            {
                return;
            }
            string namePath = null;
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(lodVolume.combined._hlodRoot) == "")
            {
                lodVolume.combined._hlodRoot.GetComponent<MeshRenderer>().enabled = true;
                namePath = Path.Combine(savePath, lodVolume.combined._hlodRoot.name + ".prefab");
                PrefabUtility.SaveAsPrefabAsset(lodVolume.combined._hlodRoot, namePath);
                lodVolume.combined._assetPath = namePath.Replace("\\", "/");
            }

            DestroyImmediate(lodVolume.combined._hlodRoot);
            lodVolume.combined._hlodRoot = null;
            lodVolume.combined._lodGroup = null;
        }

        /*将精细用资源地址引用，并删除,把自己的位置存放父节点，以便还原使用*/
        void GenrateFineModelRef(LODVolume rootLODVolume)
        {
            foreach (var fast in rootLODVolume.lodGroups)
            {
                if (fast._hlodRoot == null)
                {
                    continue;
                }
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(fast._hlodRoot);
                if (path == "")
                {
                    Debug.LogError("找不到预制体！！");
                    continue;
                }
                Transform parent = fast._hlodRoot.transform.parent;
                DestroyImmediate(fast._hlodRoot);
                fast._assetPath = path.Replace("\\", "/");
                fast._hlodRoot = parent ? parent.gameObject : null;
                fast._lodGroup = null;
                fast.Enabled = false;
            }
        }

        /*删除*/
        void DeleteHLODs(HLODGenerate hlodGenerate)
        {
            DestroyImmediate(hlodGenerate.m_RootLODVolume);
            DestroyImmediate(hlodGenerate.m_HLODS);
            hlodGenerate.DeleteExportMesh();
            hlodGenerate.DeleteExportMatTex();
            hlodGenerate.DeleteExportStreamingAsset();

            TextureAtlasModule.instance.ClearTextureAtlasList(hlodGenerate);
            hlodGenerate.m_IsGenerating = false;
            hlodGenerate.Init();
        }  
#endif
    }
}