
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    public class ChildTreeRoot : MonoBehaviour
    {
        //load脚本对象池
        public static PoolBase s_LoadAssetPool = null;

        //加载了的队列
        private Dictionary<int, LoadAssetBase> m_LoadAsset = new Dictionary<int, LoadAssetBase>();

        //加载着几个
        int m_LoadCount = 0;

        //准备卸载几个
        int m_UnLoadCount = 0;

        //延时卸载时间
        public float m_DelayUnLoadTime;

        //卸载了多少个，跟gc搭钩的变量
        //private int UnLoadCount = 0;

        //卸载超过多少个清楚内存
        private int m_Refesh = 30;

        //等待加载
        public bool m_ImmediateUnLoad = false;
        private void Update()
        {
            Refesh();
        }
        private void OnDestroy()
        {
            foreach(var loadAsset in m_LoadAsset)
            {
                loadAsset.Value.StopCoroutines();
                s_LoadAssetPool.UnSpawn(loadAsset.Value);
            }
        }
        /// <summary>
        ///刷新状态
        /// </summary>
        public void Refesh()
        {
            /*
            if(m_LoadCount < 1 && m_UnLoadCount < 1)//gc
            {
                if (UnLoadCount > m_Refesh)
                {
                    UnLoadCount = 0;
                    Resources.UnloadUnusedAssets();
                }
                return;
            }*/
            if (!m_ImmediateUnLoad  || m_LoadCount > 0 || m_UnLoadCount < 1)//是否等待加载完成
            {
                return;
            }

            foreach (var value in m_LoadAsset)
            {
                LoadAssetBase loadAsset = value.Value;
                if (loadAsset.ReadyState == Loadm_State.Unloaded)//卸载标记要卸载的
                {
                    loadAsset.ReadyState = Loadm_State.None;
                    loadAsset.UnLoad();
                    m_UnLoadCount--;  
                }
            }
        }
        /// <summary>
        ///节点实例化完毕
        /// </summary>
        public void Instantiated(LoadAssetBase loadAsset, UnityLODGroupFast fast)
        {
            m_LoadCount--;
        }

        /// <summary>
        ///节点卸载完毕
        /// </summary>
        public void Unloaded(LoadAssetBase loadAsset, UnityLODGroupFast fast)
        {
            s_LoadAssetPool.UnSpawn(loadAsset);
            m_LoadAsset.Remove(fast.GetInstanceID());
            //UnLoadCount++;
        }

        /// <summary>
        ///加载
        /// </summary>
        public void AddLoadAsset(UnityLODGroupFast fast)
        {
            int id = fast.GetInstanceID();
            LoadAssetBase loadAsset = null;
            fast.SetEnableApply();
                
            if(!m_LoadAsset.TryGetValue(id, out loadAsset))
            {
                loadAsset = s_LoadAssetPool.Spawn();
                m_LoadAsset.Add(id, loadAsset);
            }

            //要准备卸载，把状态改变回来
            if(loadAsset.ReadyState == Loadm_State.Unloaded)
            {
                m_UnLoadCount--;

            }
            loadAsset.ReadyState = Loadm_State.None;
            switch (loadAsset.State)
            {
                case Loadm_State.Instantiated:
                    loadAsset.StopCoroutines();
                    break;
                case Loadm_State.Loading:
                    break;
                case Loadm_State.Unloaded:
                    m_LoadCount++;
                    loadAsset.Load(fast, this);
                    break;
                case Loadm_State.UnLoading:
                    m_LoadCount++;
                    loadAsset.StopCoroutines();
                    loadAsset.Load(null, null);
                    break;
            }
        }

        /// <summary>
        ///卸载
        /// </summary>
        public void AddUnLoadAsset(UnityLODGroupFast fast)
        {
            int id = fast.GetInstanceID();
            LoadAssetBase loadAsset = null;
            fast.SetEnableApply();
            if (!m_LoadAsset.TryGetValue(id, out loadAsset))
            {
                return;
            }
            switch (loadAsset.State)
            {
                case Loadm_State.Loading:
                    m_LoadCount--;
                    loadAsset.UnLoad();
                    break;
                case Loadm_State.Instantiated:
                    if(!m_ImmediateUnLoad)//不等待
                    {
                        loadAsset.UnLoad();
                        break;
                    }
                    m_UnLoadCount++;
                    loadAsset.ReadyState = Loadm_State.Unloaded;
                    break;
                case Loadm_State.Unloaded:
                case Loadm_State.UnLoading:
                    break;
            }
        }
    }
}