
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
#if UNITY_EDITOR
    [System.Serializable]
    public class  BVHDivide
    {
        //分割点
        public ushort m_VolumeSplitRendererCount = 32;

        //合并层次(从底部开始计算)
        [SerializeField]
        public int m_MaximunLayer = 2;

        //开启剔除
        [SerializeField]
        public bool m_Cull = false;

        //包围盒剔除直径
        [SerializeField]
        public float m_BoundCondtionDia = 10;

        //包围盒剔除计算子物体
        [SerializeField]
        public bool m_CalboundOfChild = false;
    }
#endif
    public class HLODGenerate : MonoBehaviour
    {
        //流式,开始不等待加载完直接卸载
        public bool m_ImmediateUnLoad = false;

        //流式卸载时延时卸载
        public float m_DelayUnLoadTime;

        //距离缓冲
        public float m_CacheDistance;

        private void Start()
        {
            ChildTreeRoot [] childTrees = GetComponentsInChildren<ChildTreeRoot>();
            foreach(var tree in childTrees)
            {
                tree.m_ImmediateUnLoad = m_ImmediateUnLoad;
            }
        }
#if UNITY_EDITOR
        //BVH节点计数
        [HideInInspector]
        public int m_LODVolumeCount = 0;

        //需要生成的物体父节点们
        public GameObject[] m_Targets;

        //BVH跟节点
        public GameObject m_RootLODVolume;

        //HLODS
        public GameObject m_HLODS;

        

        [HideInInspector]
        public string m_TextureAssetPath;

        [HideInInspector]
        public string m_MaterialAssetPath;

        [HideInInspector]
        public string m_MeshAssetPath;

        [HideInInspector]
        public string m_StreamingAssetPath;

        //是否生成过
        [HideInInspector]
        public bool m_IsGenerate = false;

        //导出过资源不能重复导出
        [HideInInspector]
        public bool m_IsExportMesh = false;

        //导出过资源不能重复导出
        [HideInInspector]
        public bool m_IsExportMatTex = false;

        //是否生成了资源
        [HideInInspector]
        public bool m_GenerateAsset = false;

        //是否正在生成
        [HideInInspector]
        public bool m_IsGenerating = false;

        //是否生成流式
        [HideInInspector]
        public bool m_IsStreaming = false;

        //是否需要更新流式
        private List<LODVolume> m_StreamUpdate;

        //BVH剔除相关
        public BVHDivide m_BVHDivide;

        public List<LODVolume> StreamUpdate
        { 
            get     
            {
                if (m_StreamUpdate == null)
                {
                    m_StreamUpdate = new List<LODVolume>();
                }
                return m_StreamUpdate;
            }
        }

        public void Init()
        {
            m_RootLODVolume = null;
            m_HLODS = null;
            m_IsGenerate = false;
            m_IsExportMesh = false;
            m_IsExportMatTex = false;
            m_GenerateAsset = false;
            m_IsGenerating = false;
            m_IsStreaming = false;
            m_LODVolumeCount = 0;
            if (m_StreamUpdate != null)
            {
                m_StreamUpdate.Clear();
            }  
        }
#endif
    }
}