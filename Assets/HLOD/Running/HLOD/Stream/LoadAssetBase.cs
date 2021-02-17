using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    public enum Loadm_State
    {
        None,
        Loading,
        Instantiated,
        UnLoading,
        Unloaded,
    }
    public class LoadAssetBase
    {
        //节点数据
        protected UnityLODGroupFast m_Data;
        //维护子树
        protected ChildTreeRoot m_Root;
        //当前状态
        protected Loadm_State m_State;
        //预备状态
        protected Loadm_State m_ReadyState;
        //协程载体
        protected LoadAssetVector m_Vector;
        //当前协程
        protected Coroutine m_Coroutine;
        //开启卸载
        protected bool startUnLoadCoroutine = false;
        public  LoadAssetBase(LoadAssetVector vector)
        {
            m_Vector = vector;
        }
        public virtual void OnEnable()
        {
            m_State = Loadm_State.Unloaded;
            m_ReadyState = Loadm_State.None;
            m_Root = null;
            m_Data = null;
            m_Coroutine = null;
        }
        public virtual void Set(UnityLODGroupFast data, ChildTreeRoot root)
        {
            m_Data = data;
            m_Root = root;
        }

        /*开始加载*/
        public virtual void Load(UnityLODGroupFast data, ChildTreeRoot root)
        {
            //子类实现
        }

        /*卸载资源*/
        public virtual void UnLoad()
        {
            //子类实现
        }

        /*停止卸载*/
        public virtual void StopCoroutines()
        {
            if (m_Coroutine != null)
            {
                m_Vector.StopCoroutine(m_Coroutine);
            }
            startUnLoadCoroutine = false;
        }
        #region 对象属性
        public Loadm_State State
        {
            get
            {
                return m_State;
            }
            set
            {
                m_State = value;
            }
        }
        public Loadm_State ReadyState
        {
            get
            {
                return m_ReadyState;
            }
            set
            {
                m_ReadyState = value;
            }
        }

        #endregion
    }
}
