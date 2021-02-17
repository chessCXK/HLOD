
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

//资源暂时直接卸载
namespace Unity.HLOD
{
    
    public class LoadAsset : LoadAssetBase
    {
        
        //资源数据
        AsyncOperationHandle<GameObject> m_Handle;

        public LoadAsset(LoadAssetVector vector) :base(vector){}

        //开始加载
        public override void Load(UnityLODGroupFast data, ChildTreeRoot root)
        {
            if (m_State == Loadm_State.Unloaded)
            {
                
                m_Data = data;
                m_Root = root;
                Addressables.InstantiateAsync(m_Data._assetPath).Completed += OnAssetInstantiated;
                m_State = Loadm_State.Loading;
            }
            else if (m_State == Loadm_State.UnLoading)
            {
                m_State = Loadm_State.Loading;
            }
        }
        //实例化完毕
        void OnAssetInstantiated(AsyncOperationHandle<GameObject> obj)
        {
            m_Handle = obj;
            if (m_State != Loadm_State.Loading)
            {
                Addressables.ReleaseInstance(m_Handle.Result);
                DeleteAsset();
                return;
            }

            m_Data._hlodRoot = obj.Result;
            m_Data._hlodRoot.transform.SetPositionAndRotation(m_Data.Pose.position, m_Data.Pose.rotation);
            m_Data._hlodRoot.transform.localScale = m_Data.Pose.scale;
            m_Root.Instantiated(this, m_Data);
            m_State = Loadm_State.Instantiated;
        }
        
        //卸载资源
        public override void UnLoad()
        {
            if (m_State != Loadm_State.Instantiated)
            {
                m_State = Loadm_State.UnLoading;
            }
            if (startUnLoadCoroutine == true)
            {
                return;
            }
            startUnLoadCoroutine = true;

            m_Coroutine = m_Vector.StartCoroutine(DelayUnLoad());
        }

        //延时卸载
        IEnumerator DelayUnLoad()
        {
            if(m_Data._delayUnLoad != 0)
            {
                yield return new WaitForSeconds(m_Data._delayUnLoad);//自己
            }
            else
            {
                yield return new WaitForSeconds(m_Root.m_DelayUnLoadTime);//全局延时
            }
            DeleteAsset();
        }
        
        void DeleteAsset()
        {
            if (m_State == Loadm_State.Instantiated || m_State == Loadm_State.UnLoading)
            {
                if (m_Data._hlodRoot != null)
                {
                    Addressables.ReleaseInstance(m_Handle.Result);
                    m_Data._hlodRoot = null;
                }
                m_Root.Unloaded(this, m_Data);
                m_State = Loadm_State.Unloaded;
            }
            startUnLoadCoroutine = false; 
        }
       
    } 
}

