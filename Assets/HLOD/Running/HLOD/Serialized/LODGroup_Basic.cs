using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    [System.Serializable]
    public abstract class LODGroup_Basic : ScriptableObject
    {
        [SerializeField]
        protected bool _enabled;
        // 统一接口方便优化
        public virtual void SetEnable(bool bEnable)
        {
            if (_enabled == bEnable)
                return;
            _enabled = bEnable;
            SetRenderersEnabled(_enabled);
        }

        private bool _enabledVirtual;
        public virtual void SetEnableVirtual(bool bEnable)
        {
            _enabledVirtual = bEnable;
        }
        public virtual void ApplyEnable()
        {
            SetEnable(_enabledVirtual);
        }
        //上一帧与当前帧的状态是否一致
        public virtual bool CheckState()
        {
            return _enabled == _enabledVirtual;
        }
        /*设置状态*/
        public virtual void SetEnableApply()
        {
            _enabled = _enabledVirtual;
        }
        // 通过摄像机计算显示精度
        public virtual float GetRelativeHeight(Camera camera, Vector3 cameraPosition)
        {
            var distance = (worldSpaceCenter - cameraPosition).magnitude;
            return HLODExtensions.DistanceToRelativeHeight(camera, distance, worldSpaceSize);
        }
        // 根据摄像机计算精度后 获取显示的层级
        public virtual int GetCurrentLOD(Camera camera, Vector3 cameraPosition)
        {
            var relativeHeight = this.GetRelativeHeight(camera, cameraPosition);
            return GetCurrentLOD(relativeHeight);
        }

        // 中心点相对坐标
        public abstract Vector3 localReferencePoint { get; }
        // 显示大小
        public abstract float size { get; }
        // LOD层数
        public abstract int lodCount { get; }
        // 世界坐标系中的显示大小
        public abstract float worldSpaceSize { get; }
        // 中心点在世界坐标系的位置
        public abstract Vector3 worldSpaceCenter { get; }
        // 包围盒大小
        public abstract Bounds bounds { get; }
        // 开关Renderer显示
        public abstract void SetRenderersEnabled(bool enabled);
        // 通过精度获取当前LOD层级
        public abstract int GetCurrentLOD(float relativeHeight);
        // 原始位置
        public abstract HLODPose Pose { get; }


        #region 对象属性
        public bool EnabledVirtual { get => _enabledVirtual; set => _enabledVirtual = value; }
        #endregion
#if UNITY_EDITOR

        public bool Enabled { get => _enabled; set => _enabled = value; }

#endif

        //public abstract LOD[] GetLODs();
    }
}