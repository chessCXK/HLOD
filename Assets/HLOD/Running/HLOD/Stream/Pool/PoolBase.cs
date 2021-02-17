using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Unity.HLOD
{
   
    /*流式加载专用池*/
    public class PoolBase
    {

        //提供池子里的给过去
        public virtual LoadAssetBase Spawn()
        {
            return null;
        }

        //回收到池子里
        public virtual void UnSpawn(LoadAssetBase obj)
        {

        }
    }
}