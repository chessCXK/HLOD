using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    public class HLODCull : MonoBehaviour
    {
        public static List<HLODCull> Alls;

        //是否开启HLOD
        public static bool s_HLODEnabled = true;

        private static bool m_SceneDirty = false;

        //延时
        public static bool s_delayEnable = false;

        public static float s_CullInterval = 0.1f;

        public static float s_HLOD_Bias = 0.3f;

        //LODVolume根节点
        public LODVolume m_RootVolume;

        //后续遍历列表
        [HideInInspector]
        public List<LODVolume> m_BackSort;

        //流式加载
        [HideInInspector]
        public  bool m_Stream = false;

        //距离缓冲
        private  float s_CacheDistance = 0;

        //上一次触发位置
        private Vector3 m_LastCameraPosition = Vector3.zero;
        private class CameraCullData
        {
            public float m_lastCullTime = 0;
            public Vector3 m_LastCameraPosition;
            public Quaternion m_LastCameraRotation;
            public float m_LastFOV;
            public float m_LastLODBias;
            public float m_LastHLODBias;
            
        }

        private static CameraCullData CullData = new CameraCullData();

        static Queue<LODVolume> s_LODVolume = new Queue<LODVolume>();
        static Stack<LODVolume> s_AuxStack = new Stack<LODVolume>();
        void Start()
        {
            if (Alls == null)
            {
                Alls = new List<HLODCull>();
            }

            Alls.Add(this);
            if (Alls.Count == 1)
            {
                Camera.onPreCull += OnCameraPreCull;
            }

            s_CacheDistance = GetComponentInParent<HLODGenerate>().m_CacheDistance;
        }
        public void OnDestroy()
        {
            Alls.Remove(this);
            if (Alls.Count == 0)
            {
                Camera.onPreCull -= OnCameraPreCull;
            }
        }

        static void OnCameraPreCull(Camera camera)
        {
            if (!Application.isPlaying)
                return;
            if (camera.cameraType != CameraType.Game || camera.tag != "MainCamera")
                return;


            // 刷新间隔没到，不做任何处理
            if (CullData.m_lastCullTime + s_CullInterval > Time.realtimeSinceStartup && s_delayEnable)
            {
                return;
            }
            CullData.m_lastCullTime = Time.realtimeSinceStartup;
            //判断摄像机参数是否有变化
            var cameraTransform = camera.transform;
            var cameraPosition = cameraTransform.position;
            var cameraRotation = cameraTransform.rotation;

            

            if (CullData.m_LastCameraPosition != cameraPosition)
            {
                CullData.m_LastCameraPosition = cameraPosition;
                m_SceneDirty = true;
            }
            /*
            if (CullData.m_LastCameraRotation != cameraRotation)
            {
                CullData.m_LastCameraRotation = cameraRotation;
                m_SceneDirty = true;
            }
            */
            if (CullData.m_LastFOV != camera.fieldOfView)
            {
                CullData.m_LastFOV = camera.fieldOfView;
                m_SceneDirty = true;
            }

            //判断LOD精度设置是否有变化
            if (CullData.m_LastLODBias != UnityEngine.QualitySettings.lodBias)
            {
                CullData.m_LastLODBias = UnityEngine.QualitySettings.lodBias;
                m_SceneDirty = true;
            }
            if (CullData.m_LastHLODBias != s_HLOD_Bias)
            {
                CullData.m_LastHLODBias = s_HLOD_Bias;
                m_SceneDirty = true;
            }

            if (m_SceneDirty)
            {
                foreach (var tree in Alls)
                {
                    //判断是否超过距离缓冲，超过了再改变状态
                    if (Vector3.Distance(tree.m_LastCameraPosition, cameraPosition) < tree.s_CacheDistance)
                    {
                        continue;
                    }
                    tree.m_LastCameraPosition = cameraPosition;

                    LODVolume rootLODVolume = tree.m_RootVolume;
                    if (rootLODVolume == null)
                    {
                        continue;
                    }

                    tree.HLODPreCull(camera, camera.transform.position);
                    HLODApply(tree.m_Stream, rootLODVolume);

                    foreach (var lodGroup in rootLODVolume.lodGroups)
                    {
                        if (tree.m_Stream)
                        {
                            if (!lodGroup.CheckState())
                            {
                                ChoiceQueue(lodGroup._lodVolume, lodGroup);
                            }
                            continue;
                        }
                        lodGroup.ApplyEnable();
                    }
                }
                CullData.m_LastCameraPosition = cameraPosition;
                CullData.m_LastCameraRotation = cameraRotation;
                m_SceneDirty = false;
            }
        }
        static void HLODApply(bool stream, LODVolume node)
        {
            s_LODVolume.Clear();
            s_LODVolume.Enqueue(node);
            while (s_LODVolume.Count > 0)
            {
                LODVolume lodVolume = s_LODVolume.Dequeue();

                foreach (var child in lodVolume.childVolumes)
                {
                    if (child.combined == null && child.childVolumes.Count < 1)
                    {
                        continue;
                    }
                    s_LODVolume.Enqueue(child);
                }
                if (lodVolume.combined != null)
                {
                    if (stream)
                    {
                        if (!lodVolume.combined.CheckState())
                        {
                            ChoiceQueue(lodVolume, lodVolume.combined);
                        }
                        continue;
                    }
                    lodVolume.combined.ApplyEnable();
                }
            }
        }
        //选择卸载或加载队列
        static void ChoiceQueue(LODVolume node, UnityLODGroupFast fast)
        {
            if (node.childTreeRoot == null)
            {
                node.childTreeRoot = node.gameObject.AddComponent<ChildTreeRoot>();
            }
            if (fast.EnabledVirtual)
            {
                node.childTreeRoot.AddLoadAsset(fast);
            }
            else
            {
                node.childTreeRoot.AddUnLoadAsset(fast);
            }
        }
        static void SetLOGGroupEnable(LODGroup_Basic lodGroup, bool bEnable)
        {
            lodGroup.SetEnableVirtual(bEnable);
        }

        static void SetHLODEnable(LODVolume node, bool bEnable)
        {
            if (node.combined != null)
            {
                SetLOGGroupEnable(node.combined, bEnable);
            }
        }
        static void SetLODEnable(LODVolume node, bool bEnable)
        {
            if (node.combined == null)
            {
                // 由于有一种情况是单个模型时，不会合并
                // 所以这里如果没有合并的模型，就看看有没有单独的模型需要设置
                foreach (var lodGroup in node.lodGroups)
                {
                     SetLOGGroupEnable(lodGroup, bEnable);
                }
            }
        }

        //TODO 准备将这一步移植到Job System
        void HLODPreCull(Camera camera, Vector3 cameraPos)
        {
            s_AuxStack.Clear();
            List<LODVolume> backSort = m_BackSort;
            if(!s_HLODEnabled)//不开HLOD
            {
                foreach (var lv in backSort)
                {
                    if(lv.combined)
                    {
                        SetLOGGroupEnable(lv.combined, false);
                    }
                }
                foreach (var lodGroup in m_RootVolume.lodGroups)
                {
                    SetLOGGroupEnable(lodGroup, true);
                }
                return;
            }
            for (int index = 0; index < backSort.Count - 1; index++)
            {
                LODVolume curLv = backSort[index];
                LODVolume nextLv = backSort[index + 1];

                s_AuxStack.Push(curLv);
                if (curLv.deep <= nextLv.deep)
                {
                    //表示后续遍历还没遍历到当前节点的父节点，继续入栈
                    continue;
                }
                bool parentVisible = true;
                //下降，表示遍历到了父节点，把栈顶前面几个相同的兄弟节点处理，然后将当前节点入栈
                while (s_AuxStack.Count > 0)
                {
                    LODVolume node = s_AuxStack.Pop();
                    if(node.combined == null && node.childVolumes.Count > 0)
                    {
                        //遇到没有合并的父节点
                        continue;
                    }
                    bool allChildInvisible = false;
                    if (node.childVolumes.Count == 0)
                    {
                        foreach (var lodGroup in node.lodGroups)
                        {
                            if (lodGroup.GetCurrentLOD(camera, cameraPos) != lodGroup.lodCount - 1)
                            {
                                // 只要有一个显示最精细模型
                                // 就认为不能显示HLOD对象
                                allChildInvisible = true;
                                parentVisible = false;
                                break;
                            }
                        }
                        // 修改所有原始模型的显示状态
                        foreach (var lodGroup in node.lodGroups)
                        {
                            SetLOGGroupEnable(lodGroup, allChildInvisible);
                        }
                        //设置合批模型状态
                        if(node.combined)
                        {
                            SetLOGGroupEnable(node.combined, !allChildInvisible);
                        }
                    }
                    else if(parentVisible != false)//不是叶子节点，且兄弟节点没有显示
                    {
                        if(node.combined.EnabledVirtual)//如果当前节点显示就不管,否则自己显示了，那么父节点需要隐藏
                        {
                            continue;
                        }
                        parentVisible = false;
                    }

                    LODVolume who = null;
                    if(s_AuxStack.Count > 0)
                    {
                        who = s_AuxStack.Peek();
                    }
                    else
                    {
                        break;
                    }

                    //下一个是上层节点
                    if(who.deep != node.deep)
                    {
                        break;
                    }
                }
                if(nextLv.combined != null)
                {
                    s_AuxStack.Push(nextLv);
                    
                    if (parentVisible)
                    {
                        //孩子合批都显示，所以隐藏孩子显示自己
                        foreach (var child in nextLv.childVolumes)
                        {
                            SetHLODEnable(child, false);
                        }
                    }
                    else
                    {
                        //父节点不能显示，激活叶子没有合并的LODGroup
                        foreach (var child in nextLv.childVolumes)
                        {
                            SetLODEnable(child, true);
                        }
                    }
                    SetLOGGroupEnable(nextLv.combined, parentVisible);
                }
            }
        }
    }
}