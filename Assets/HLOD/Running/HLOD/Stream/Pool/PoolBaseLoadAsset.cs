using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.HLOD
{
    /*流式加载专用池*/
    public class PoolBaseLoadAsset : PoolBase
    {
        static string s_VectorName = "LoadAssetBaseVector";
        private List<LoadAssetBase> _freeObjs = new List<LoadAssetBase>(10);
        private List<LoadAssetBase> _busyObjs = new List<LoadAssetBase>(10);

        //所有加载脚本的载体
        private LoadAssetVector m_Vector;
        //提供池子里的给过去
        public override LoadAssetBase Spawn()
        {
            if (m_Vector == null)
            {
                GameObject obj = new GameObject(s_VectorName);
                m_Vector = obj.AddComponent<LoadAssetVector>();
            }
            LoadAssetBase t = null;
            if (_freeObjs.Count <= 0)
            {
                //本套资源流式装卸用LoadAsset，如果自己写的请继承LoadAssetBase类，然后new你的这个类
                t = new LoadAsset(m_Vector);
                //t = new LoadAssetBase(m_Vector);
                _busyObjs.Add(t);
                t.OnEnable();
                return t;
            }

            t = _freeObjs[0];
            _freeObjs.RemoveAt(0);
            _busyObjs.Add(t);
            t.OnEnable();
            return t;
        }

        //回收到池子里
        public override void UnSpawn(LoadAssetBase obj)
        {
            if (obj == null)
                return;

            if (!_freeObjs.Contains(obj))
            {
                _freeObjs.Add(obj);
            }
            if (_busyObjs.Contains(obj))
            {
                _busyObjs.Remove(obj);
            }

        }

        public static void ActionPoolBaseLoadAsset()
        {
            ChildTreeRoot.s_LoadAssetPool = new PoolBaseLoadAsset();
        }
    }
}