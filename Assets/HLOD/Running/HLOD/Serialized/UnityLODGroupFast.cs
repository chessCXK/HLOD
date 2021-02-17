using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    [System.Serializable]
    public class UnityLODGroupFast : UnityLODGroup
    {
        [SerializeField]
        public Vector3 _localReferencePoint;
        [SerializeField]
        public float _size;
        [SerializeField]
        public int _lodCount;
        [SerializeField]
        public LOD[] _lods;
        [SerializeField]
        public float _worldSpaceSize;
        [SerializeField]
        public Vector3 _worldSpaceCenter;
        [SerializeField]
        public Bounds _bounds;
        [SerializeField]
        public float[] screenRelativeTransitionHeights;
        [SerializeField]
        public HLODPose _pose;
        [SerializeField]
        public float _delayUnLoad;
#if UNITY_EDITOR
        public override void Init(LODGroup lodGroup, GameObject hlodRoot)
        {
            base.Init(lodGroup, hlodRoot);
            _localReferencePoint = _lodGroup.localReferencePoint;
            _size = _lodGroup.size;
            _lodCount = _lodGroup.lodCount;
            _lods = _lodGroup.GetLODs();
            _worldSpaceCenter = base.worldSpaceCenter;
            _worldSpaceSize = base.worldSpaceSize;
            _bounds = base.bounds;
            screenRelativeTransitionHeights = new float[_lods.Length];
            for (int i = 0; i < _lods.Length; i++)
            {
                screenRelativeTransitionHeights[i] = _lods[i].screenRelativeTransitionHeight;
            }
            _pose.position = _hlodRoot.transform.position;
            _pose.rotation = _hlodRoot.transform.rotation;
            _pose.scale = _hlodRoot.transform.localScale;
        }
#endif
        public override Vector3 localReferencePoint { get { return _localReferencePoint; } }

        public override float size { get { return _size; } }
        public override int lodCount { get { return _lodCount; } }
        public override LOD[] GetLODs()
        {
            if (_lods == null && _lodGroup != null)
                _lods = _lodGroup.GetLODs();
            return _lods;
        }

        public override float worldSpaceSize
        {
            get
            {
                return _worldSpaceSize;
            }
        }

        public override Vector3 worldSpaceCenter
        {
            get
            {
                return _worldSpaceCenter;
            }
        }

        public override Bounds bounds { get { return _bounds; } }

        public override int GetCurrentLOD(float relativeHeight)
        {
            var lodIndex = screenRelativeTransitionHeights.Length - 1;

            for (var i = 0; i < screenRelativeTransitionHeights.Length; i++)
            {
                if (relativeHeight >= screenRelativeTransitionHeights[i])
                {
                    lodIndex = i;
                    break;
                }
            }

            return lodIndex;
        }

        public override int GetCurrentLOD(Camera camera, Vector3 cameraPosition)
        {
            return base.GetCurrentLOD(camera, cameraPosition);
        }
        public override HLODPose Pose
        {
            get
            {
                return _pose;
            }
        }
    }
}
