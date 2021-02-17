using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    [System.Serializable]
    public class UnityLODGroup : LODGroup_Basic
    {
        //当前维护的lodgroup
        [SerializeField]
        public LODGroup _lodGroup;

        //当前维护的合批模型节点
        [SerializeField]
        public GameObject _hlodRoot;

        //流式加载路径
        [SerializeField]
        public string _assetPath;

        //哪个叶子LODVolume引用了我
        [SerializeField]
        public LODVolume _lodVolume;
#if UNITY_EDITOR
        public virtual void Init(LODGroup lodGroup, GameObject hlodRoot)
        {
            _lodGroup = lodGroup;
            _enabled = _lodGroup.enabled;
            _hlodRoot = hlodRoot;
        }
#endif
        public override Vector3 localReferencePoint { get { return _lodGroup.localReferencePoint; } }

        public override float size { get { return _lodGroup.size; } }

        public override int lodCount { get { return _lodGroup.lodCount; } }

        public virtual LOD[] GetLODs() { return _lodGroup.GetLODs(); }
        public override float worldSpaceSize
        {
            get
            {
                return _lodGroup.GetWorldSpaceSize();
            }
        }
        public override Vector3 worldSpaceCenter { get { return _lodGroup.GetWorldSpaceCenter(); } }
        public override Bounds bounds { get { return _lodGroup.GetBounds(); } }

        public override int GetCurrentLOD(float relativeHeight)
        {
            LOD[] lods = GetLODs();
            var lodIndex = lods.Length;

            for (var i = 0; i < lods.Length; i++)
            {
                var lod = lods[i];

                if (relativeHeight >= lod.screenRelativeTransitionHeight)
                {
                    lodIndex = i;
                    break;
                }
            }

            return lodIndex;
        }
        public override void SetRenderersEnabled(bool enabled)
        {
            var lods = GetLODs();
            for (var i = 0; i < lods.Length; i++)
            {
                var lod = lods[i];

                var renderers = lod.renderers;
                foreach (var r in renderers)
                {
                    if (r)
                        r.enabled = enabled;
                }
            }
        }
        public override HLODPose Pose
        {
            get
            {

                HLODPose p = new HLODPose();
                if (_hlodRoot)
                {
                    p.position = _hlodRoot.transform.position;
                    p.rotation = _hlodRoot.transform.rotation;
                    p.scale = _hlodRoot.transform.localScale;
                }
                return p;
            }

        }

        public bool Equals(UnityLODGroup _other)
        {
            if (ReferenceEquals(null, _other)) return false;
            if (ReferenceEquals(this, _other)) return true;
            return this._lodGroup == _other._lodGroup;
        }
        public override bool Equals(object obj)
        {
            //this非空，obj如果为空，则返回false
            if (ReferenceEquals(null, obj)) return false;

            //如果为同一对象，必然相等
            if (ReferenceEquals(this, obj)) return true;

            //如果类型不同，则必然不相等
            if (obj.GetType() != this.GetType()) return false;

            //调用强类型对比
            return Equals((UnityLODGroup)obj);
        }
        public override int GetHashCode()
        {
            return _lodGroup ? _lodGroup.GetHashCode() : 0;
        }

        //重写==操作符
        public static bool operator ==(UnityLODGroup left, UnityLODGroup right)
        {
            return Equals(left, right);
        }

        //重写!=操作符
        public static bool operator !=(UnityLODGroup left, UnityLODGroup right)
        {
            return !Equals(left, right);
        }
    }
}