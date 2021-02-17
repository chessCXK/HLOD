using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.HLOD
{
    public class SceneLOD : ScriptableSingleton<SceneLOD>
    {
        string m_CreateRootVolumeForScene = "Default"; // Set to some value, so new scenes don't auto-create
        Queue<IEnumerator> m_CoroutineQueue = new Queue<IEnumerator>();

        LODVolume m_RootVolume;
        HashSet<LODGroup> m_ExistingLODGroups = new HashSet<LODGroup>();
        HashSet<LODGroup> m_AddedLODGroups = new HashSet<LODGroup>();
        HashSet<LODGroup> m_RemovedLODGroups = new HashSet<LODGroup>();
        HashSet<LODGroup> m_ExcludedLODGroups = new HashSet<LODGroup>();
        List<LODGroup> m_FoundLODGroups = new List<LODGroup>();

        /*当前正在维护的HLODGenerate*/
        public HLODGenerate m_CurrHLODGenerate;
        IEnumerator UpdateOctreeByLodGroup(GameObject[] gameObjects = null)
        {
            if (gameObjects == null || gameObjects.Count() < 1)
                yield break;

            if (!m_RootVolume)
            {
                if (m_CreateRootVolumeForScene == SceneManager.GetActiveScene().name)
                {
#if UNITY_EDITOR
                    m_RootVolume = LODVolume.Create(m_CurrHLODGenerate);
#endif
                }

                else
                    yield break;
            }

            var lodGroups = m_FoundLODGroups;
            lodGroups.Clear();

            foreach(var objRoot in gameObjects)
            {
                LODGroup [] childGroups = objRoot.GetComponentsInChildren<LODGroup>();
                lodGroups.AddRange(childGroups);
            }

            // Remove any renderers that should not be there (e.g. HLODs)
            lodGroups.RemoveAll(r =>
            m_ExcludedLODGroups.Contains(r)
            );
            lodGroups.RemoveAll(l =>
            {
                if (l)
                {
                    // Check against previous collection
                    if (m_ExcludedLODGroups.Contains(l))
                        return false;
                    
                    var rds = l.GetLODs()[0].renderers;
                    foreach(var r in rds)
                    {
                        if (r == null)
                            return false;

                        var mf = r.GetComponent<MeshFilter>();
                        if (!mf || (mf.sharedMesh && mf.sharedMesh.GetTopology(0) != MeshTopology.Triangles))
                        {
                            m_ExcludedLODGroups.Add(l);
                            return true;
                        }
#if UNITY_EDITOR
                        //根据包围盒直径进行剔除不需要计算的模型
                        if (m_CurrHLODGenerate.m_BVHDivide.m_Cull)
                        {
                            bool result = m_CurrHLODGenerate.m_BVHDivide.CullByMesh(r.transform);
                            if (result)
                            {
                                m_ExcludedLODGroups.Add(l);
                                return true;
                            }
                        }
#endif
                    }
                }

                return false;
            });

            var existingLODGroups = m_ExistingLODGroups;
            existingLODGroups.Clear();
            existingLODGroups.UnionWith(m_RootVolume.lodGroups.Select(l => l._lodGroup));

            var removed = m_RemovedLODGroups;
            removed.Clear();
            removed.UnionWith(m_ExistingLODGroups);
            removed.ExceptWith(lodGroups);

            var added = m_AddedLODGroups;
            added.Clear();
            added.UnionWith(lodGroups);
            added.ExceptWith(existingLODGroups);

            int count = 1;
            foreach (var l in removed)
            {
                if (existingLODGroups.Contains(l))
                {
#if UNITY_EDITOR
                    yield return m_RootVolume.RemoveLodGroup(l);
#endif
                    // Check if the BVH shrunk
                    yield return SetRootLODVolume();
                }
            }
            count = 1;
            foreach (var l in added)
            {
                EditorUtility.DisplayProgressBar("网格划分", l.name, (float)count++ / added.Count);
                if (!existingLODGroups.Contains(l))
                {
                    UnityLODGroupFast lodGroupFast = CreateInstance<UnityLODGroupFast>();
#if UNITY_EDITOR
                    lodGroupFast.Init(l, l.gameObject);
                    //UnityLODGroupFast lodGroupFast = new UnityLODGroupFast(l, l.gameObject);
                    yield return m_RootVolume.AddLodGroup(lodGroupFast);
#endif
                    l.transform.hasChanged = false;

                    // Check if the BVH grew
                    yield return SetRootLODVolume();
                }
            }
        }
        IEnumerator SetRootLODVolume()
        {
            if (m_RootVolume)
            {
                var rootVolumeTransform = m_RootVolume.transform;
                var transformRoot = rootVolumeTransform.root;

                if(transformRoot.GetComponent<HLODGenerate>())//已经是根节点
                {
                    yield break;
                }

                // Handle the case where the BVH has grown
                if (rootVolumeTransform != transformRoot)
                    m_RootVolume = transformRoot.GetComponent<LODVolume>();

                yield break;
            }

            // Handle initialization or the case where the BVH has shrunk
            LODVolume lodVolume = null;
            var scene = SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var go in rootGameObjects)
            {
                if (!go)
                    continue;

                lodVolume = go.GetComponent<LODVolume>();
                if (lodVolume)
                    break;

                yield return null;
            }
            
            if (lodVolume)
                m_RootVolume = lodVolume;
        }

        static Action<GameObject, GameObject> s_endCall = null;
        IEnumerator GenerateHLOD(GameObject[] gameObjects = null, Action<GameObject, GameObject> endCall = null)
        {
            m_CoroutineQueue.Enqueue(UpdateOctreeByLodGroup(gameObjects));

            s_endCall = endCall;

            yield return MonoBehaviourHelper.StartCoroutine(m_CoroutineQueue.Dequeue());
            while (m_RootVolume == null)
            {
                yield return null;
            }

            #if UNITY_EDITOR
            if (m_RootVolume && m_RootVolume.dirty)
            {
                GameObject hlods = null;
                if (m_CurrHLODGenerate != null)//更新
                {
                    hlods = m_CurrHLODGenerate.m_HLODS;
                }
                if (!hlods)//创建
                {
                    hlods = new GameObject("RootHLOD");
                }

                m_CoroutineQueue.Enqueue(m_RootVolume.CheckAfterBVHUpdata(m_CurrHLODGenerate.m_BVHDivide.m_MaximunLayer));
                m_CoroutineQueue.Enqueue(m_RootVolume.UpdateHLODs(m_CurrHLODGenerate.m_BVHDivide.m_MaximunLayer, hlods));


                m_CoroutineQueue.Enqueue(m_RootVolume.UpdateRootVolume((List<LODVolume> backList) => {
                    HLODCull hlodCull = m_RootVolume.GetComponent<HLODCull>();
                    if (!m_RootVolume.GetComponent<HLODCull>())
                    {
                        hlodCull = m_RootVolume.gameObject.AddComponent<HLODCull>();
                        Selection.activeGameObject = hlodCull.gameObject;
                    }
                    hlodCull.m_BackSort = backList;
                    EditorUtility.ClearProgressBar();
                    EditorUtility.SetDirty(m_RootVolume);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    //AutoLOD.sceneLODEnabled = false;

                    if (s_endCall != null)
                    {
                        s_endCall.Invoke(m_RootVolume.gameObject, hlods.gameObject);
                    }

                }));
            }
            else
            {
                EditorUtility.ClearProgressBar();
                if (s_endCall != null)
                {
                    s_endCall.Invoke(null, null);
                }
            }
#endif
            while (m_CoroutineQueue.Count > 0)
            {
                yield return MonoBehaviourHelper.StartCoroutine(m_CoroutineQueue.Dequeue());
            }
        }

        /*生成SceneLOD接口,指定hlodGenerate*/
        public void GenerateSceneLODByHLODGenerate(HLODGenerate hlodGenerate, Action<GameObject, GameObject> endCall = null)
        {
            m_CurrHLODGenerate = hlodGenerate;
#if UNITY_EDITOR
            if (hlodGenerate.m_RootLODVolume)
            {
                m_RootVolume = hlodGenerate.m_RootLODVolume.GetComponent<LODVolume>();
            }
            else
            {
                m_RootVolume = null;
            }
            m_CreateRootVolumeForScene = SceneManager.GetActiveScene().name;
            MonoBehaviourHelper.StartCoroutine(GenerateHLOD(hlodGenerate.m_Targets, endCall));
#endif
        }

        /*更新BVH*/
        public void OnUpdataBVHByHLODGenerate(HLODGenerate hlodGenerate, Action<GameObject, GameObject> endCall = null)
        {
            GenerateSceneLODByHLODGenerate(hlodGenerate, endCall);
        }
    }
}
