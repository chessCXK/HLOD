using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    [Serializable]
    public struct HLODPose
    {
        //
        // 摘要:
        //     The position component of the pose.
        [SerializeField]
        public Vector3 position;
        //
        // 摘要:
        //     The rotation component of the pose.
        [SerializeField]
        public Quaternion rotation;

        [SerializeField]
        public Vector3 scale;

        public HLODPose(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }
}